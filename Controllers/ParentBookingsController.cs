using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

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

    /// <summary>
    /// Создать бронирование на конкретный слот (статус Pending).
    /// Атомарно помечает слот как занятый. Если не получилось — 409.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest req)
    {
        var parent = await _userManager.GetUserAsync(User);
        if (parent is null) return Unauthorized();

        // Найдём слот
        var slot = await _db.AvailabilitySlots
            .FirstOrDefaultAsync(s => s.Id == req.AvailabilitySlotId);

        if (slot is null)
            return NotFound(new { error = "Slot not found" });

        // Проверим, что у специалиста есть утверждённый профиль
        var specProfile = await _db.SpecialistProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == slot.SpecialistUserId);

        if (specProfile is null || specProfile.Status != ModerationStatus.Approved)
            return BadRequest(new { error = "Specialist is not available for booking (profile not approved)" });

        // Нельзя бронировать прошлое
        if (slot.StartsAtUtc < DateTime.UtcNow)
            return BadRequest(new { error = "Cannot book a past slot" });

        // Атомарно пытаемся занять слот
        var affected = await _db.AvailabilitySlots
            .Where(s => s.Id == slot.Id && s.IsBooked == false)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsBooked, true)
                .SetProperty(s => s.UpdatedAtUtc, DateTime.UtcNow)
            );

        if (affected == 0)
            return Conflict(new { error = "Slot already booked" });

        // Создаём бронирование (Pending)
        var booking = new Booking
        {
            SpecialistUserId = slot.SpecialistUserId,
            ParentUserId = parent.Id,
            StartsAtUtc = slot.StartsAtUtc,
            EndsAtUtc = slot.EndsAtUtc,
            Status = BookingStatus.Pending,
            MessageFromParent = req.MessageFromParent,
            AvailabilitySlotId = slot.Id,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(booking));
    }

    /// <summary>
    /// Список моих броней родителя с фильтрами (опц.).
    /// </summary>
    [HttpGet]
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
            .Select(b => ToResponse(b))
            .AsNoTracking()
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Отмена родителем. Освобождает слот, если он ещё помечен как занятый и время в будущем.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
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

        // Освобождаем слот, если он ещё актуален
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

    private static BookingResponse ToResponse(Booking b) =>
        new(b.Id, b.SpecialistUserId, b.ParentUserId, b.StartsAtUtc, b.EndsAtUtc, b.Status, b.MessageFromParent, b.AvailabilitySlotId, b.CreatedAtUtc, b.UpdatedAtUtc);
}
