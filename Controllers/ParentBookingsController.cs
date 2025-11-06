using System.Linq.Expressions;
using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.Swagger.Annotations;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/bookings")]
[Authorize(Roles = "Parent")]
public class ParentBookingsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public ParentBookingsController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    // Единая проекция, которую EF может перевести в SQL
    private static readonly Expression<Func<Booking, BookingResponse>> BookingToResponse =
        b => new BookingResponse(
            b.Id,
            b.SpecialistUserId,
            b.ParentUserId,
            b.StartsAtUtc,
            b.EndsAtUtc,
            b.Status,
            b.MessageFromParent,
            b.AvailabilitySlotId,
            b.ChildId,                 // <-- порядок согласован
            b.CreatedAtUtc,
            b.UpdatedAtUtc
        );

    /// <summary>
    /// Создать бронирование на конкретный слот (статус Pending).
    /// Атомарно помечает слот как занятый. Если не получилось — 409 (Slot already booked).
    /// Требует, чтобы выбранный ребёнок принадлежал текущему родителю.
    /// </summary>
    /// <remarks>
    /// Бизнес-правила:
    /// - Слот должен быть в будущем
    /// - Профиль специалиста должен быть одобрен (Approved)
    /// - Ребёнок должен принадлежать текущему пользователю (родителю)
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create([FromBody] CreateBookingRequest req)
    {
        var parent = await _userManager.GetUserAsync(User);
        if (parent is null) return Unauthorized();

        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1) слот
        var slot = await _db.AvailabilitySlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.AvailabilitySlotId);

        if (slot is null)
            return NotFound(new { error = "Slot not found" });

        // 2) профиль специалиста одобрен
        var isApproved = await _db.SpecialistProfiles
            .AsNoTracking()
            .AnyAsync(p => p.UserId == slot.SpecialistUserId &&
                           p.Status == ModerationStatus.Approved);

        if (!isApproved)
            return BadRequest(new { error = "Specialist is not available for booking (profile not approved)" });

        // 3) нельзя в прошлое
        if (slot.StartsAtUtc < now)
            return BadRequest(new { error = "Cannot book a past slot" });

        // 4) ребёнок принадлежит текущему родителю
        var childIsMine = await _db.Children
            .Where(c => c.Id == req.ChildId)
            .Select(c => c.ParentProfile!.UserId)
            .FirstOrDefaultAsync();

        if (childIsMine is null)
            return NotFound(new { error = "Child not found" });

        if (childIsMine != parent.Id)
            return Forbid();

        // 5) атомарно занимаем слот
        var affected = await _db.AvailabilitySlots
            .Where(s => s.Id == slot.Id && s.IsBooked == false)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsBooked, true)
                .SetProperty(s => s.UpdatedAtUtc, now));

        if (affected == 0)
            return Conflict(new { error = "Slot already booked" });

        // 6) создаём бронь
        var booking = new Booking
        {
            SpecialistUserId = slot.SpecialistUserId,
            ParentUserId = parent.Id,
            StartsAtUtc = slot.StartsAtUtc,
            EndsAtUtc = slot.EndsAtUtc,
            Status = BookingStatus.Pending,
            MessageFromParent = req.MessageFromParent,
            AvailabilitySlotId = slot.Id,
            ChildId = req.ChildId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // Можно вернуть 200 OK (как сейчас) или 201 Created с Location
        return Ok(new BookingResponse(
            booking.Id, booking.SpecialistUserId, booking.ParentUserId,
            booking.StartsAtUtc, booking.EndsAtUtc, booking.Status,
            booking.MessageFromParent, booking.AvailabilitySlotId, booking.ChildId,
            booking.CreatedAtUtc, booking.UpdatedAtUtc));
    }

    /// <summary>
    /// Список моих бронирований (родителя) с опциональными фильтрами.
    /// </summary>
    [HttpGet]
    [SwaggerOperation("Список бронирований родителя")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> MyBookings(
        [FromQuery] BookingStatus? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc)
    {
        var parent = await _userManager.GetUserAsync(User);
        if (parent is null) return Unauthorized();

        var q = _db.Bookings.Where(b => b.ParentUserId == parent.Id);

        if (status is not null) q = q.Where(b => b.Status == status);
        if (fromUtc is not null && toUtc is not null && toUtc > fromUtc)
            q = q.Where(b => b.StartsAtUtc < toUtc && b.EndsAtUtc > fromUtc);

        var list = await q
            .OrderByDescending(b => b.StartsAtUtc)
            .Select(BookingToResponse)
            .AsNoTracking()
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Отмена бронирования родителем.
    /// Освобождает слот (если встреча в будущем и слот всё ещё помечен как занятый).
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [SwaggerOperation( "Отменить бронирование родителем")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var parent = await _userManager.GetUserAsync(User);
        if (parent is null) return Unauthorized();

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.ParentUserId == parent.Id);
        if (booking is null) return NotFound();

        if (booking.Status is BookingStatus.CancelledByParent or BookingStatus.CancelledBySpecialist or BookingStatus.Declined)
            return NoContent(); // уже отменено/отклонено

        booking.Status = BookingStatus.CancelledByParent;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        // Освободить слот, если встреча в будущем
        if (booking.AvailabilitySlotId is Guid slotId && booking.StartsAtUtc > DateTime.UtcNow)
        {
            await _db.AvailabilitySlots
                .Where(s => s.Id == slotId && s.IsBooked == true)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.IsBooked, false)
                    .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow));
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
