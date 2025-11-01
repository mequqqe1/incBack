using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/specialist/schedule-template")]
[Authorize(Roles = "Specialist")]
public class SpecialistScheduleTemplateController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public SpecialistScheduleTemplateController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    // ------------------- GET -------------------
    [HttpGet]
    public async Task<ActionResult<WeeklyTemplateResponse>> Get()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var tpl = await _db.WeeklyTemplates
            .Include(t => t.Slots)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.SpecialistUserId == user.Id);

        if (tpl is null)
            return Ok(new WeeklyTemplateResponse(Guid.Empty, false, Array.Empty<WeeklyTemplateSlotDto>()));

        var slots = tpl.Slots
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartLocalTime)
            .Select(s => new WeeklyTemplateSlotDto(s.DayOfWeek, s.StartLocalTime, s.EndLocalTime, s.Note))
            .ToArray();

        return Ok(new WeeklyTemplateResponse(tpl.Id, tpl.IsActive, slots));
    }

    // ------------------- PRESETS -------------------
    [HttpGet("presets")]
    public ActionResult<IEnumerable<SchedulePresetInfo>> Presets()
    {
        var list = new[]
        {
            new SchedulePresetInfo("weekdays_10_18", "Будни 10:00–18:00", 30),
            new SchedulePresetInfo("evenings_18_21", "Вечера 18:00–21:00", 30),
            new SchedulePresetInfo("weekends_10_16", "Выходные 10:00–16:00", 30),
            new SchedulePresetInfo("mixed",          "Смешанный",          30),
            new SchedulePresetInfo("empty",          "Пустой",             30),
        };
        return Ok(list);
    }
    [HttpPost("materialize")]
[Authorize(Roles = "Specialist")]
public async Task<ActionResult> MaterializeTemplate(MaterializeTemplateRequest req)
{
    var user = await _userManager.GetUserAsync(User);
    if (user is null)
        return Unauthorized();

    // --- базовая валидация ---
    if (req.ToDateUtc <= req.FromDateUtc)
        return BadRequest(new { error = "ToDateUtc must be greater than FromDateUtc" });

    var maxRange = req.FromDateUtc.AddDays(90);
    if (req.ToDateUtc > maxRange)
        return BadRequest(new { error = "Range must not exceed 90 days" });

    // --- загружаем шаблон ---
    var template = await _db.WeeklyTemplates
        .Include(t => t.Slots)
        .FirstOrDefaultAsync(t => t.SpecialistUserId == user.Id);

    if (template == null || !template.IsActive)
        return BadRequest(new { error = "No active weekly template found" });

    var slotsToAdd = new List<AvailabilitySlot>();

    // --- генерируем реальные даты из шаблона ---
    var current = req.FromDateUtc.Date;
    var endDate = req.ToDateUtc.Date;

    while (current < endDate)
    {
        var dayOfWeek = (int)current.DayOfWeek;
        var daySlots = template.Slots.Where(s => s.DayOfWeek == dayOfWeek);

        foreach (var s in daySlots)
        {
            // если нужно — пропускаем прошлое
            var startsAt = current.Add(s.StartLocalTime.ToTimeSpan());
            var endsAt = current.Add(s.EndLocalTime.ToTimeSpan());

            if (req.SkipPast && endsAt <= DateTime.UtcNow)
                continue;

            var slot = new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                SpecialistUserId = user.Id,
                StartsAtUtc = startsAt,
                EndsAtUtc = endsAt,
                Note = s.Note,
                IsBooked = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            slotsToAdd.Add(slot);
        }

        current = current.AddDays(1);
    }

    if (slotsToAdd.Count == 0)
        return Ok(new { message = "No slots to materialize (possibly all in the past)" });

    // --- удаляем старые пересекающиеся ---
    var startUtc = req.FromDateUtc.Date;
    var endUtc = req.ToDateUtc.Date;
    await _db.AvailabilitySlots
        .Where(a => a.SpecialistUserId == user.Id &&
                    a.StartsAtUtc >= startUtc &&
                    a.EndsAtUtc < endUtc)
        .ExecuteDeleteAsync();

    // --- добавляем новые ---
    _db.AvailabilitySlots.AddRange(slotsToAdd);
    await _db.SaveChangesAsync();

    return Ok(new
    {
        created = slotsToAdd.Count,
        range = new { from = req.FromDateUtc, to = req.ToDateUtc }
    });
}


    // ------------------- UPSERT (основной метод) -------------------
    [HttpPut]
   private async Task<ActionResult<WeeklyTemplateResponse>> UpsertTemplate(UpsertWeeklyTemplateRequest req)
{
    var user = await _userManager.GetUserAsync(User);
    if (user is null)
        return Unauthorized();

    if (req.Slots == null || req.Slots.Length == 0)
        return BadRequest(new { error = "Slots required" });

    // базовая валидация
    foreach (var s in req.Slots)
    {
        if (s.EndLocalTime <= s.StartLocalTime)
            return BadRequest(new { error = "EndLocalTime must be > StartLocalTime" });
        if (!AlignedTo30(s.StartLocalTime) || !AlignedTo30(s.EndLocalTime))
            return BadRequest(new { error = "Times must be aligned to 30 minutes" });
    }

    // проверка пересечений
    foreach (var group in req.Slots.GroupBy(x => x.DayOfWeek))
    {
        var list = group.OrderBy(x => x.StartLocalTime).ToList();
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i - 1].EndLocalTime > list[i].StartLocalTime)
                return BadRequest(new { error = $"Overlap in day {group.Key}" });
        }
    }

    // --- полностью удаляем старый шаблон ---
    var oldTemplate = await _db.WeeklyTemplates
        .FirstOrDefaultAsync(t => t.SpecialistUserId == user.Id);

    if (oldTemplate != null)
    {
        await _db.WeeklyTemplateSlots
            .Where(x => x.WeeklyTemplateId == oldTemplate.Id)
            .ExecuteDeleteAsync();

        _db.WeeklyTemplates.Remove(oldTemplate);
        await _db.SaveChangesAsync();
    }

    // --- создаём новый ---
    var newTemplate = new WeeklyTemplate
    {
        SpecialistUserId = user.Id,
        IsActive = req.IsActive,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
        Slots = req.Slots.Select(s => new WeeklyTemplateSlot
        {
            DayOfWeek = s.DayOfWeek,
            StartLocalTime = s.StartLocalTime,
            EndLocalTime = s.EndLocalTime,
            Note = s.Note
        }).ToList()
    };

    _db.WeeklyTemplates.Add(newTemplate);
    await _db.SaveChangesAsync();

    // --- ответ ---
    var resp = new WeeklyTemplateResponse(
        newTemplate.Id,
        newTemplate.IsActive,
        newTemplate.Slots
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartLocalTime)
            .Select(s => new WeeklyTemplateSlotDto(s.DayOfWeek, s.StartLocalTime, s.EndLocalTime, s.Note))
            .ToArray()
    );

    return Ok(resp);
}


    // ------------------- FROM PRESET -------------------
    [HttpPost("from-preset")]
    public async Task<ActionResult<WeeklyTemplateResponse>> GenerateFromPreset(GenerateFromPresetRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        int[] days = req.DaysOfWeek ?? req.PresetCode switch
        {
            "weekdays_10_18" => new[] { 1, 2, 3, 4, 5 },
            "evenings_18_21" => new[] { 1, 2, 3, 4, 5 },
            "weekends_10_16" => new[] { 6, 0 },
            "mixed"          => new[] { 1, 2, 3, 4, 5 },
            "empty"          => Array.Empty<int>(),
            _ => new[] { 1, 2, 3, 4, 5 }
        };

        if (req.EndLocalTime <= req.StartLocalTime)
            return BadRequest(new { error = "EndLocalTime must be greater than StartLocalTime" });

        var slotMin = req.SlotMinutes is 15 or 30 or 60 ? req.SlotMinutes : 30;

        var slots = new List<WeeklyTemplateSlotDto>();
        if (req.PresetCode == "mixed")
        {
            var monWedFri = new[] { 1, 3, 5 };
            var tueThu = new[] { 2, 4 };
            slots.AddRange(BuildDaySlots(monWedFri, new TimeOnly(10, 0), new TimeOnly(13, 0), 30, null, req.Note));
            slots.AddRange(BuildDaySlots(tueThu, new TimeOnly(16, 0), new TimeOnly(20, 0), 30, null, req.Note));
        }
        else if (req.PresetCode != "empty")
        {
            var breaks = req.Breaks ?? Array.Empty<BreakDto>();
            slots.AddRange(BuildDaySlots(days, req.StartLocalTime, req.EndLocalTime, slotMin, breaks, req.Note));
        }

        ValidateSlots(slots);

        var upsertReq = new UpsertWeeklyTemplateRequest(
            slots.ToArray(),
            req.IsActive
        );

        return await UpsertTemplate(upsertReq);

    }

    // ------------------- HELPERS -------------------

    private static void ValidateSlots(IEnumerable<WeeklyTemplateSlotDto> slots)
    {
        foreach (var s in slots)
        {
            if (s.EndLocalTime <= s.StartLocalTime)
                throw new ArgumentException("EndLocalTime must be greater than StartLocalTime");
            if (!AlignedTo30(s.StartLocalTime) || !AlignedTo30(s.EndLocalTime))
                throw new ArgumentException("Times must be aligned to 30 minutes (HH:mm like 09:00 / 09:30)");
        }

        foreach (var group in slots.GroupBy(x => x.DayOfWeek))
        {
            var list = group.OrderBy(x => x.StartLocalTime).ToList();
            for (int i = 1; i < list.Count; i++)
                if (list[i - 1].EndLocalTime > list[i].StartLocalTime)
                    throw new ArgumentException($"Overlap in day {group.Key}");
        }
    }

    private static IEnumerable<WeeklyTemplateSlotDto> BuildDaySlots(
        IEnumerable<int> days,
        TimeOnly start,
        TimeOnly end,
        int stepMin,
        IEnumerable<BreakDto>? breaks,
        string? note)
    {
        breaks ??= Array.Empty<BreakDto>();
        foreach (var d in days.Distinct().OrderBy(x => x))
        {
            var t = start;
            while (t < end)
            {
                var next = t.AddMinutes(stepMin);
                if (next > end) break;
                if (!breaks.Any(b => t < b.To && b.From < next))
                    yield return new WeeklyTemplateSlotDto(d, t, next, note);
                t = next;
            }
        }
    }

    private static bool AlignedTo30(TimeOnly t)
        => t.Minute % 30 == 0 && t.Second == 0 && t.Millisecond == 0;
}
