using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/specialist/bookings")]
[Authorize(Roles = "Specialist")]
public class SpecialistBookingsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public SpecialistBookingsController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>
    /// Входящие заявки специалиста.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> Incoming(
        [FromQuery] BookingStatus? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var q = _db.Bookings.Where(b => b.SpecialistUserId == spec.Id);

        if (status is not null) q = q.Where(b => b.Status == status);
        if (fromUtc is not null && toUtc is not null && toUtc > fromUtc)
            q = q.Where(b => b.StartsAtUtc < toUtc && b.EndsAtUtc > fromUtc);

        var list = await q
            .OrderByDescending(b => b.StartsAtUtc)
            .Select(b => new BookingResponse(
                b.Id, b.SpecialistUserId, b.ParentUserId, b.StartsAtUtc, b.EndsAtUtc,
                b.Status, b.MessageFromParent, b.AvailabilitySlotId, b.ChildId, b.CreatedAtUtc, b.UpdatedAtUtc))

            .AsNoTracking()
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Подтвердить заявку (Pending -> Confirmed).
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.SpecialistUserId == spec.Id);
        if (booking is null) return NotFound();

        if (booking.Status != BookingStatus.Pending)
            return BadRequest(new { error = "Only pending bookings can be confirmed" });

        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Отклонить заявку (Pending -> Declined) и освободить слот.
    /// </summary>
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.SpecialistUserId == spec.Id);
        if (booking is null) return NotFound();

        if (booking.Status != BookingStatus.Pending)
            return BadRequest(new { error = "Only pending bookings can be declined" });

        booking.Status = BookingStatus.Declined;
        booking.UpdatedAtUtc = DateTime.UtcNow;

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
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseBookingRequest req)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var booking = await _db.Bookings
            .Include(b => b.Outcome)
            .FirstOrDefaultAsync(b => b.Id == id && b.SpecialistUserId == spec.Id);

        if (booking is null) return NotFound(new { error = "Booking not found" });

        if (booking.Status != BookingStatus.Confirmed)
            return BadRequest(new { error = "Only confirmed bookings can be closed" });

        // (опционально) запрет закрытия до конца слота
        // if (booking.EndsAtUtc > DateTime.UtcNow)
        //     return BadRequest(new { error = "Cannot close before session ends" });

        var now = DateTime.UtcNow;
        if (booking.Outcome is null)
        {
            booking.Outcome = new BookingOutcome
            {
                BookingId = booking.Id,
                SpecialistUserId = booking.SpecialistUserId,
                ParentUserId = booking.ParentUserId,
                Summary = req.Summary,
                Recommendations = req.Recommendations,
                NextSteps = req.NextSteps,
                SpecialistPrivateNotes = req.SpecialistPrivateNotes,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.BookingOutcomes.Add(booking.Outcome);
        }
        else
        {
            booking.Outcome.Summary = req.Summary;
            booking.Outcome.Recommendations = req.Recommendations;
            booking.Outcome.NextSteps = req.NextSteps;
            booking.Outcome.SpecialistPrivateNotes = req.SpecialistPrivateNotes;
            booking.Outcome.UpdatedAtUtc = now;
        }

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAtUtc = now;

        await _db.SaveChangesAsync();
        return NoContent();
    }
    
    /// <summary>Детали брони с Outcome (для специалиста)</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailsResponse>> GetDetails(Guid id)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var b = await _db.Bookings
            .AsNoTracking()
            .Include(x => x.Outcome)
            .FirstOrDefaultAsync(x => x.Id == id && x.SpecialistUserId == spec.Id);

        if (b is null) return NotFound();

        BookingOutcomeResponse? outcome = b.Outcome is null ? null :
            new BookingOutcomeResponse(b.Id, b.Outcome.Summary ?? "",
                b.Outcome.Recommendations, b.Outcome.NextSteps,
                b.Outcome.CreatedAtUtc, b.Outcome.ParentAcknowledgedAtUtc);

        return Ok(new BookingDetailsResponse(
            b.Id, b.SpecialistUserId, b.ParentUserId, b.StartsAtUtc, b.EndsAtUtc,
            b.Status, b.MessageFromParent, b.AvailabilitySlotId, b.ChildId,
            b.CreatedAtUtc, b.UpdatedAtUtc, outcome));
    }

}
