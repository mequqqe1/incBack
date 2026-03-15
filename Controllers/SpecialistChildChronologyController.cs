using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCBack.Models;
using INCBack.Models.Tracker;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

/// <summary>Хронология ребёнка для специалиста — доступна только если есть/была бронь с этим ребёнком.</summary>
[ApiController]
[Route("api/specialist/children/{childId:guid}/chronology")]
[Authorize(Roles = "Specialist")]
public class SpecialistChildChronologyController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public SpecialistChildChronologyController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> Get(Guid childId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var hasAccess = await _db.Bookings
            .AnyAsync(b => b.ChildId == childId && b.SpecialistUserId == spec.Id);
        if (!hasAccess) return NotFound(new { error = "No booking with this child. Access denied." });

        var items = new List<object>();

        var notesQuery = _db.ChildNotes.AsNoTracking().Where(n => n.ChildId == childId);
        if (fromUtc.HasValue) notesQuery = notesQuery.Where(n => n.CreatedAtUtc >= fromUtc);
        if (toUtc.HasValue) notesQuery = notesQuery.Where(n => n.CreatedAtUtc <= toUtc);
        var notes = await notesQuery.OrderBy(n => n.CreatedAtUtc).Select(n => new { n.Id, n.Text, n.CreatedAtUtc }).ToListAsync();
        foreach (var n in notes)
            items.Add(new { type = "note", id = n.Id, atUtc = n.CreatedAtUtc, summary = n.Text.Length > 200 ? n.Text[..200] + "…" : n.Text });

        var docsQuery = _db.ChildDocuments.AsNoTracking().Where(d => d.ChildId == childId);
        if (fromUtc.HasValue) docsQuery = docsQuery.Where(d => d.CreatedAtUtc >= fromUtc);
        if (toUtc.HasValue) docsQuery = docsQuery.Where(d => d.CreatedAtUtc <= toUtc);
        var docs = await docsQuery.OrderBy(d => d.CreatedAtUtc).Select(d => new { d.Id, d.FileName, d.CreatedAtUtc }).ToListAsync();
        foreach (var d in docs)
            items.Add(new { type = "document", id = d.Id, atUtc = d.CreatedAtUtc, summary = d.FileName });

        var bookingsQuery = _db.Bookings.AsNoTracking().Include(b => b.Outcome).Where(b => b.ChildId == childId);
        if (fromUtc.HasValue) bookingsQuery = bookingsQuery.Where(b => b.EndsAtUtc >= fromUtc);
        if (toUtc.HasValue) bookingsQuery = bookingsQuery.Where(b => b.StartsAtUtc <= toUtc);
        var bookings = await bookingsQuery.OrderBy(b => b.StartsAtUtc).ToListAsync();
        foreach (var b in bookings)
        {
            items.Add(new { type = "booking", id = b.Id, atUtc = b.StartsAtUtc, specialistUserId = b.SpecialistUserId, status = (int)b.Status, summary = $"Встреча {b.StartsAtUtc:yyyy-MM-dd HH:mm}" });
            if (b.Outcome != null)
                items.Add(new { type = "outcome", id = b.Outcome.Id, bookingId = b.Id, atUtc = b.Outcome.CreatedAtUtc, summary = b.Outcome.Summary != null && b.Outcome.Summary.Length > 300 ? b.Outcome.Summary[..300] + "…" : (b.Outcome.Summary ?? "") });
        }

        var entriesQuery = _db.DailyEntries.AsNoTracking().Where(e => e.ChildId == childId);
        if (fromUtc.HasValue) entriesQuery = entriesQuery.Where(e => e.Date >= DateOnly.FromDateTime(fromUtc.Value));
        if (toUtc.HasValue) entriesQuery = entriesQuery.Where(e => e.Date <= DateOnly.FromDateTime(toUtc.Value));
        var entries = await entriesQuery.OrderBy(e => e.Date).Select(e => new { e.Id, e.Date, e.ParentNote, e.CreatedAtUtc }).ToListAsync();
        foreach (var e in entries)
            items.Add(new { type = "daily_entry", id = e.Id, atUtc = e.CreatedAtUtc, date = e.Date.ToString("yyyy-MM-dd"), summary = e.ParentNote != null && e.ParentNote.Length > 200 ? e.ParentNote[..200] + "…" : (e.ParentNote ?? "Запись за день") });

        var entryIds = entries.Select(e => e.Id).ToList();
        if (entryIds.Count > 0)
        {
            var meds = await _db.DailyMedIntakes.AsNoTracking().Where(m => entryIds.Contains(m.DailyEntryId)).Select(m => new { m.Id, m.Drug, m.Dose, m.TimeUtc, m.Taken }).ToListAsync();
            foreach (var m in meds)
            {
                if (fromUtc.HasValue && m.TimeUtc < fromUtc.Value) continue;
                if (toUtc.HasValue && m.TimeUtc > toUtc.Value) continue;
                items.Add(new { type = "med_intake", id = m.Id, atUtc = m.TimeUtc, summary = $"{m.Drug}" + (m.Dose != null ? $" {m.Dose}" : "") + (m.Taken ? " (принято)" : " (не принято)") });
            }
            var sessions = await _db.DailySessions.AsNoTracking().Where(s => entryIds.Contains(s.DailyEntryId)).Select(s => new { s.Id, s.Type, s.DurationMin, s.CreatedAtUtc }).ToListAsync();
            foreach (var s in sessions)
            {
                if (fromUtc.HasValue && s.CreatedAtUtc < fromUtc.Value) continue;
                if (toUtc.HasValue && s.CreatedAtUtc > toUtc.Value) continue;
                items.Add(new { type = "session", id = s.Id, atUtc = s.CreatedAtUtc, summary = $"{s.Type}, {s.DurationMin} мин" });
            }
            var incidents = await _db.DailyIncidents.AsNoTracking().Where(i => entryIds.Contains(i.DailyEntryId)).Select(i => new { i.Id, i.TimeUtc, i.Intensity, i.Notes }).ToListAsync();
            foreach (var i in incidents)
            {
                if (fromUtc.HasValue && i.TimeUtc < fromUtc.Value) continue;
                if (toUtc.HasValue && i.TimeUtc > toUtc.Value) continue;
                items.Add(new { type = "incident", id = i.Id, atUtc = i.TimeUtc, intensity = i.Intensity, summary = i.Notes != null && i.Notes.Length > 150 ? i.Notes[..150] + "…" : (i.Notes ?? $"Инцидент ({i.Intensity})") });
            }
        }

        items.Sort((a, b) => GetAt(a).CompareTo(GetAt(b)));
        return Ok(items);

        static DateTime GetAt(object x)
        {
            var t = x.GetType().GetProperty("atUtc");
            return t?.GetValue(x) is DateTime d ? d : DateTime.MinValue;
        }
    }
}
