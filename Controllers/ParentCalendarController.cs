using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCBack.Models;
using INCBack.Models.Tracker;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;
using SharpAuthDemo.Services;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/calendar")]
[Authorize(Roles = "Parent")]
public class ParentCalendarController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IFamilyContextService _familyContext;

    public ParentCalendarController(UserManager<ApplicationUser> userManager, AppDbContext db, IFamilyContextService familyContext)
    {
        _userManager = userManager;
        _db = db;
        _familyContext = familyContext;
    }

    /// <summary>События для семейного календаря: брони (кто ведёт), приёмы лекарств (кто внёс), сессии (кто внёс).</summary>
    [HttpGet("events")]
    public async Task<ActionResult<IEnumerable<object>>> GetEvents(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] Guid? childId = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var parentProfileId = family.ParentProfileId;
        var events = new List<object>();

        // Брони: время, ребёнок, специалист, кто ведёт (опекун)
        var bookingsQuery = _db.Bookings
            .AsNoTracking()
            .Where(b => b.ParentUserId == family.OwnerUserId && b.StartsAtUtc < toUtc && b.EndsAtUtc > fromUtc);
        if (childId.HasValue) bookingsQuery = bookingsQuery.Where(b => b.ChildId == childId);

        var bookings = await bookingsQuery
            .Select(b => new { b.Id, b.ChildId, b.SpecialistUserId, b.StartsAtUtc, b.EndsAtUtc, b.Status, b.AssignedCaregiverMemberId })
            .ToListAsync();

        var caregiverIds = bookings.Where(b => b.AssignedCaregiverMemberId.HasValue).Select(b => b.AssignedCaregiverMemberId!.Value).Distinct().ToList();
        Dictionary<Guid, (string Email, string? Relation)> caregivers = caregiverIds.Count > 0
            ? await _db.CaregiverMembers.AsNoTracking().Where(m => caregiverIds.Contains(m.Id)).Select(m => new { m.Id, m.Email, m.Relation }).ToDictionaryAsync(m => m.Id, m => (m.Email, m.Relation))
            : new Dictionary<Guid, (string, string?)>();

        foreach (var b in bookings)
        {
            object? assignedTo = null;
            if (b.AssignedCaregiverMemberId.HasValue && caregivers.TryGetValue(b.AssignedCaregiverMemberId.Value, out var c))
                assignedTo = new { email = c.Email, relation = c.Relation };

            events.Add(new
            {
                type = "booking",
                id = b.Id,
                childId = b.ChildId,
                specialistUserId = b.SpecialistUserId,
                startsAtUtc = b.StartsAtUtc,
                endsAtUtc = b.EndsAtUtc,
                status = (int)b.Status,
                assignedCaregiver = assignedTo
            });
        }

        // Дети семьи (для фильтра по childId)
        var childrenIds = await _db.Children.Where(c => c.ParentProfileId == family.ParentProfileId).Select(c => c.Id).ToListAsync();
        List<Guid> forChildren = childId.HasValue ? (childrenIds.Contains(childId.Value) ? new List<Guid> { childId.Value } : new List<Guid>()) : childrenIds;
        if (forChildren.Count == 0) return Ok(events);

        // Приёмы лекарств с датой в диапазоне
        var entryIds = await _db.DailyEntries
            .AsNoTracking()
            .Where(e => forChildren.Contains(e.ChildId) && e.Date >= DateOnly.FromDateTime(fromUtc) && e.Date <= DateOnly.FromDateTime(toUtc))
            .Select(e => e.Id)
            .ToListAsync();

        if (entryIds.Count > 0)
        {
            var meds = await _db.DailyMedIntakes
                .AsNoTracking()
                .Where(m => entryIds.Contains(m.DailyEntryId))
                .Select(m => new { m.Id, m.DailyEntryId, m.Drug, m.Dose, m.TimeUtc, m.Taken, m.RecordedByUserId })
                .ToListAsync();

            var entryToChild = await _db.DailyEntries.AsNoTracking().Where(e => entryIds.Contains(e.Id)).Select(e => new { e.Id, e.ChildId }).ToDictionaryAsync(x => x.Id);

            foreach (var m in meds)
            {
                if (!entryToChild.TryGetValue(m.DailyEntryId, out var e)) continue;
                events.Add(new
                {
                    type = "med_intake",
                    id = m.Id,
                    childId = e.ChildId,
                    drug = m.Drug,
                    dose = m.Dose,
                    timeUtc = m.TimeUtc,
                    taken = m.Taken,
                    recordedByUserId = m.RecordedByUserId
                });
            }

            var sessions = await _db.DailySessions
                .AsNoTracking()
                .Where(s => entryIds.Contains(s.DailyEntryId))
                .Select(s => new { s.Id, s.DailyEntryId, s.Type, s.DurationMin, s.CreatedAtUtc, s.RecordedByUserId })
                .ToListAsync();

            foreach (var s in sessions)
            {
                if (!entryToChild.TryGetValue(s.DailyEntryId, out var e)) continue;
                events.Add(new
                {
                    type = "session",
                    id = s.Id,
                    childId = e.ChildId,
                    sessionType = s.Type,
                    durationMin = s.DurationMin,
                    timeUtc = s.CreatedAtUtc,
                    recordedByUserId = s.RecordedByUserId
                });
            }
        }

        return Ok(events);
    }
}
