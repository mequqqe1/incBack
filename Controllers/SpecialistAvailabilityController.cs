using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/specialist/availability")]
[Authorize(Roles = "Specialist")]
public class SpecialistAvailabilityController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public SpecialistAvailabilityController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>
    /// Создать слоты доступности пачкой (кратность 30 минут, без пересечений).
    /// Все даты — UTC.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IEnumerable<AvailabilitySlotResponse>>> Create([FromBody] CreateAvailabilityRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        if (req.Slots is null || req.Slots.Length == 0)
            return BadRequest(new { error = "No slots provided" });

        // Валидация входных данных (локальная)
        foreach (var s in req.Slots)
        {
            if (s.StartsAtUtc >= s.EndsAtUtc)
                return BadRequest(new { error = "EndsAtUtc must be greater than StartsAtUtc" });

            if (!IsThirtyMinuteAligned(s.StartsAtUtc) || !IsThirtyMinuteAligned(s.EndsAtUtc))
                return BadRequest(new { error = "Times must be aligned to 30-minute boundaries (e.g., 09:00, 09:30)" });

            if (s.StartsAtUtc < DateTime.UtcNow.AddMinutes(-1))
                return BadRequest(new { error = "Slot cannot start in the past" });
        }

        // Проверка пересечений внутри пачки
        var ordered = req.Slots.OrderBy(s => s.StartsAtUtc).ToArray();
        for (int i = 1; i < ordered.Length; i++)
        {
            if (IntervalsOverlap(ordered[i - 1].StartsAtUtc, ordered[i - 1].EndsAtUtc, ordered[i].StartsAtUtc, ordered[i].EndsAtUtc))
                return BadRequest(new { error = "Provided slots overlap each other" });
        }

        // Диапазон, чтобы одним запросом вытащить потенциальные пересечения
        var minStart = ordered.First().StartsAtUtc;
        var maxEnd   = ordered.Last().EndsAtUtc;

        var existing = await _db.AvailabilitySlots
            .Where(x => x.SpecialistUserId == user.Id &&
                        x.StartsAtUtc < maxEnd && x.EndsAtUtc > minStart)
            .Select(x => new { x.StartsAtUtc, x.EndsAtUtc })
            .ToListAsync();

        // Пересечения с существующими
        foreach (var s in ordered)
        {
            if (existing.Any(e => IntervalsOverlap(e.StartsAtUtc, e.EndsAtUtc, s.StartsAtUtc, s.EndsAtUtc)))
                return Conflict(new { error = "Some slots overlap existing availability" });
        }

        // Создаём
        var toAdd = ordered.Select(s => new AvailabilitySlot
        {
            SpecialistUserId = user.Id,
            StartsAtUtc = DateTime.SpecifyKind(s.StartsAtUtc, DateTimeKind.Utc),
            EndsAtUtc   = DateTime.SpecifyKind(s.EndsAtUtc,   DateTimeKind.Utc),
            Note = s.Note,
            IsBooked = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        }).ToList();

        await _db.AvailabilitySlots.AddRangeAsync(toAdd);
        await _db.SaveChangesAsync();

        var resp = toAdd
            .OrderBy(s => s.StartsAtUtc)
            .Select(s => new AvailabilitySlotResponse(s.Id, s.StartsAtUtc, s.EndsAtUtc, s.IsBooked, s.Note))
            .ToList();

        return Ok(resp);
    }

    /// <summary>
    /// Получить свои слоты на интервал [fromUtc; toUtc).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvailabilitySlotResponse>>> List([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        if (toUtc <= fromUtc)
            return BadRequest(new { error = "toUtc must be greater than fromUtc" });

        var list = await _db.AvailabilitySlots
            .Where(x => x.SpecialistUserId == user.Id &&
                        x.StartsAtUtc < toUtc && x.EndsAtUtc > fromUtc)
            .OrderBy(x => x.StartsAtUtc)
            .Select(s => new AvailabilitySlotResponse(s.Id, s.StartsAtUtc, s.EndsAtUtc, s.IsBooked, s.Note))
            .AsNoTracking()
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Удалить свой слот (если он не забронирован).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var slot = await _db.AvailabilitySlots.FirstOrDefaultAsync(x => x.Id == id && x.SpecialistUserId == user.Id);
        if (slot is null) return NotFound();

        if (slot.IsBooked)
            return Conflict(new { error = "Slot already booked and cannot be deleted" });

        _db.AvailabilitySlots.Remove(slot);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ---------- helpers ----------
    private static bool IsThirtyMinuteAligned(DateTime dtUtc)
    {
        // допускаем секунды/миллисекунды = 0
        return dtUtc.Kind == DateTimeKind.Utc && dtUtc.Minute % 30 == 0 && dtUtc.Second == 0 && dtUtc.Millisecond == 0;
    }

    private static bool IntervalsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && bStart < aEnd;
}
