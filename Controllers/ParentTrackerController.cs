using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using INCBack.Models;
using INCBack.Models.Tracker;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SharpAuthDemo.Data;

namespace INCBack.Controllers;

[ApiController]
[Authorize(Roles = "Parent")]
[Route("api/parent/children/{childId:guid}/tracker")]
public class ParentTrackerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ParentTrackerController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db; _users = users;
    }
    
    private async Task<ParentProfile?> GetCurrentParent()
    {
        var uid = _users.GetUserId(User)!;
        return await _db.ParentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == uid);
    }

    private async Task<bool> ChildBelongsToCurrentParent(Guid childId)
    {
        var pp = await GetCurrentParent();
        if (pp is null) return false;
        return await _db.Children.AsNoTracking()
            .AnyAsync(c => c.Id == childId && c.ParentProfileId == pp.Id);
    }

    // нормализуем дату: только день в UTC
    private static DateOnly DayFromUtc(DateTime anyUtc)
    {
        var dt = DateTime.SpecifyKind(anyUtc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(dt.Date);
    }

    private static string? SerializeOrNull(string[]? xs)
        => xs is { Length: > 0 } ? JsonSerializer.Serialize(xs, _json) : null;

    private static string[] DeserializeOrEmpty(string? json)
        => json is null ? Array.Empty<string>()
                        : (JsonSerializer.Deserialize<string[]>(json, _json) ?? Array.Empty<string>());
    
    public record EntryUpsertDto(
        DateTime dateUtc,
        double? sleepTotalHours, int? sleepLatencyMin, int? nightWakings, int? sleepQuality,
        int mood, int? anxiety, bool sensoryOverload,
        int? mealsCount, int appetite, string? dietNotes,
        string? communicationLevel, string? newSkillObserved,
        int toiletingStatus, bool? selfCareDressing, bool? selfCareHygiene,
        bool? homeTasksDone, bool? rewardUsed,
        string[]? triggers, string[]? environmentChanges,
        string? parentNote
    );

    public record IncidentDto(
        DateTime timeUtc, int intensity, int? durationSec, bool injury,
        string[]? antecedent, string[]? behavior, string[]? consequence,
        string? notes
    );

    public record MedIntakeDto(
        string drug, string? dose, DateTime timeUtc, bool taken, string[]? sideEffects
    );

    public record SessionDto(
        string type, int durationMin, int? quality, string[]? goalTags, string? notes
    );

    // компактный VM дня для фронта (как Flo: одна карточка)
    public record DayVm(
        Guid? id,
        DateOnly date,
        double? sleepTotalHours, int? sleepLatencyMin, int? nightWakings, int? sleepQuality,
        int mood, int? anxiety, bool sensoryOverload,
        int? mealsCount, int appetite, string? dietNotes,
        string? communicationLevel, string? newSkillObserved,
        int toiletingStatus, bool? selfCareDressing, bool? selfCareHygiene,
        bool? homeTasksDone, bool? rewardUsed,
        string[] triggers, string[] environmentChanges,
        string? parentNote,
        int incidentsCount,
        IEnumerable<object> incidents,
        IEnumerable<object> medIntakes,
        IEnumerable<object> sessions
    );

    private static DayVm ToVm(DailyEntry e)
    {
        return new DayVm(
            e.Id == Guid.Empty ? null : e.Id,
            e.Date,
            e.SleepTotalHours, e.SleepLatencyMin, e.NightWakings, e.SleepQuality,
            (int)e.Mood, e.Anxiety, e.SensoryOverload,
            e.MealsCount, (int)e.Appetite, e.DietNotes,
            e.CommunicationLevel, e.NewSkillObserved,
            (int)e.ToiletingStatus, e.SelfCareDressing, e.SelfCareHygiene,
            e.HomeTasksDone, e.RewardUsed,
            DeserializeOrEmpty(e.TriggersJson),
            DeserializeOrEmpty(e.EnvironmentChangesJson),
            e.ParentNote,
            e.IncidentsCount,
            e.Incidents.OrderBy(i => i.TimeUtc).Select(i => new {
                i.Id, i.TimeUtc, i.Intensity, i.DurationSec, i.Injury,
                antecedent = DeserializeOrEmpty(i.AntecedentJson),
                behavior = DeserializeOrEmpty(i.BehaviorJson),
                consequence = DeserializeOrEmpty(i.ConsequenceJson),
                i.Notes
            }),
            e.MedIntakes.OrderBy(m => m.TimeUtc).Select(m => new {
                m.Id, m.Drug, m.Dose, m.TimeUtc, m.Taken,
                sideEffects = DeserializeOrEmpty(m.SideEffectsJson)
            }),
            e.Sessions.OrderBy(s => s.CreatedAtUtc).Select(s => new {
                s.Id, s.Type, s.DurationMin, s.Quality,
                goalTags = DeserializeOrEmpty(s.GoalTagsJson),
                s.Notes
            })

        );
    }

    // удобный сортировщик для сессий (если Time отсутствует)
    // добавь это extension рядом либо во внутреннем static классе:
    // public static DateTime TimeUtcFallback(this DailySession s) => s.CreatedAtUtc ?? DateTime.MinValue;
    // но в модели DailySession не было CreatedAt; добавим:
    // (см. примечание ниже)
    // Для упрощения:
    private static DateTime TimeUtcFallback(DailySession s) => DateTime.MinValue;

    // ───────────────────────── ENDPOINTS ─────────────────────────

    /// GET день (или пустой шаблон, если нет записи)
    [HttpGet("day")]
    public async Task<ActionResult<DayVm>> GetDay(Guid childId, [FromQuery] DateTime dateUtc)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var d = DayFromUtc(dateUtc);

        var entry = await _db.DailyEntries
            .Include(x => x.Incidents).Include(x => x.MedIntakes).Include(x => x.Sessions)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChildId == childId && x.Date == d);

        if (entry is null)
        {
            // пустой «день-шаблон» (как Flo), id = null
            entry = new DailyEntry { ChildId = childId, Date = d };
        }

        return Ok(ToVm(entry));
    }

    /// GET диапазон дней (лента/календрары) — маркеры наличия записей
    public record DayMarker(DateOnly date, bool hasEntry, int incidentsCount);
    [HttpGet("days")]
    public async Task<ActionResult<IEnumerable<DayMarker>>> GetDays(Guid childId, DateTime fromUtc, DateTime toUtc)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var from = DayFromUtc(fromUtc);
        var to   = DayFromUtc(toUtc);

        var rows = await _db.DailyEntries.AsNoTracking()
            .Where(x => x.ChildId == childId && x.Date >= from && x.Date <= to)
            .Select(x => new DayMarker(x.Date, true, x.IncidentsCount))
            .ToListAsync();

        return Ok(rows);
    }

    /// UPSERT день (основные поля)
    [HttpPost("day")]
    public async Task<IActionResult> UpsertDay(Guid childId, [FromBody] EntryUpsertDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var d = DayFromUtc(dto.dateUtc);

        var e = await _db.DailyEntries.FirstOrDefaultAsync(x => x.ChildId == childId && x.Date == d);
        if (e is null)
        {
            e = new DailyEntry { ChildId = childId, Date = d };
            _db.DailyEntries.Add(e);
        }

        // Валидация-диапазоны
        if (dto.sleepTotalHours is < 0 or > 16) return Problem("sleepTotalHours must be 0..16", statusCode:400);
        if (dto.sleepLatencyMin is < 0 or > 600) return Problem("sleepLatencyMin must be 0..600", statusCode:400);
        if (dto.nightWakings is < 0 or > 20) return Problem("nightWakings must be 0..20", statusCode:400);
        if (dto.sleepQuality is < 1 or > 5 && dto.sleepQuality is not null) return Problem("sleepQuality 1..5", statusCode:400);
        if (dto.anxiety is < 0 or > 3 && dto.anxiety is not null) return Problem("anxiety 0..3", statusCode:400);
        if (dto.mealsCount is < 0 or > 10 && dto.mealsCount is not null) return Problem("mealsCount 0..10", statusCode:400);

        e.SleepTotalHours = dto.sleepTotalHours;
        e.SleepLatencyMin = dto.sleepLatencyMin;
        e.NightWakings    = dto.nightWakings;
        e.SleepQuality    = dto.sleepQuality;

        e.Mood            = (MoodLevel)Math.Clamp(dto.mood, 0, 5);
        e.Anxiety         = dto.anxiety;
        e.SensoryOverload = dto.sensoryOverload;

        e.MealsCount      = dto.mealsCount;
        e.Appetite        = (AppetiteLevel)Math.Clamp(dto.appetite, 0, 3);
        e.DietNotes       = string.IsNullOrWhiteSpace(dto.dietNotes) ? null : dto.dietNotes.Trim();

        e.CommunicationLevel = string.IsNullOrWhiteSpace(dto.communicationLevel) ? null : dto.communicationLevel.Trim();
        e.NewSkillObserved   = string.IsNullOrWhiteSpace(dto.newSkillObserved) ? null : dto.newSkillObserved.Trim();

        e.ToiletingStatus    = (ToiletingStatus)Math.Clamp(dto.toiletingStatus, 0, 3);
        e.SelfCareDressing   = dto.selfCareDressing;
        e.SelfCareHygiene    = dto.selfCareHygiene;

        e.HomeTasksDone      = dto.homeTasksDone;
        e.RewardUsed         = dto.rewardUsed;

        e.TriggersJson           = SerializeOrNull(dto.triggers);
        e.EnvironmentChangesJson = SerializeOrNull(dto.environmentChanges);
        e.ParentNote             = string.IsNullOrWhiteSpace(dto.parentNote) ? null : dto.parentNote.Trim();
        e.UpdatedAtUtc           = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─────────── инциденты (create/update/delete, batch) ───────────

    [HttpPost("incidents")]
    public async Task<IActionResult> AddIncident(Guid childId, [FromQuery] DateTime dateUtc, [FromBody] IncidentDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var d = DayFromUtc(dateUtc);

        if (dto.intensity is < 1 or > 5) return Problem("intensity 1..5", statusCode:400);
        if (dto.durationSec is < 0 or > 3600) return Problem("durationSec 0..3600", statusCode:400);

        var e = await _db.DailyEntries.FirstOrDefaultAsync(x => x.ChildId == childId && x.Date == d)
              ?? _db.DailyEntries.Add(new DailyEntry { ChildId = childId, Date = d }).Entity;

        var inc = new DailyIncident
        {
            DailyEntryId   = e.Id,
            TimeUtc        = DateTime.SpecifyKind(dto.timeUtc, DateTimeKind.Utc),
            Intensity      = dto.intensity,
            DurationSec    = dto.durationSec,
            Injury         = dto.injury,
            AntecedentJson = SerializeOrNull(dto.antecedent),
            BehaviorJson   = SerializeOrNull(dto.behavior),
            ConsequenceJson= SerializeOrNull(dto.consequence),
            Notes          = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim()
        };
        _db.DailyIncidents.Add(inc);

        // корректно пересчитаем после вставки
        await _db.SaveChangesAsync();
        e.IncidentsCount = await _db.DailyIncidents.CountAsync(x => x.DailyEntryId == e.Id);
        await _db.SaveChangesAsync();

        return Ok(new { inc.Id });
    }

    public record IncidentUpdateDto(
        int? intensity, int? durationSec, bool? injury,
        string[]? antecedent, string[]? behavior, string[]? consequence,
        string? notes
    );

    [HttpPatch("incidents/{id:guid}")]
    public async Task<IActionResult> UpdateIncident(Guid childId, Guid id, [FromBody] IncidentUpdateDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();

        var inc = await _db.DailyIncidents
            .Include(i => i.DailyEntry)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inc is null || inc.DailyEntry is null || inc.DailyEntry.ChildId != childId) return NotFound();

        if (dto.intensity is { } it && (it < 1 || it > 5)) return Problem("intensity 1..5", statusCode:400);
        if (dto.durationSec is { } du && (du < 0 || du > 3600)) return Problem("durationSec 0..3600", statusCode:400);

        if (dto.intensity is { } i) inc.Intensity = i;
        if (dto.durationSec is { } ds) inc.DurationSec = ds;
        if (dto.injury is { } inj) inc.Injury = inj;
        if (dto.antecedent is { }) inc.AntecedentJson = SerializeOrNull(dto.antecedent);
        if (dto.behavior   is { }) inc.BehaviorJson   = SerializeOrNull(dto.behavior);
        if (dto.consequence is { }) inc.ConsequenceJson = SerializeOrNull(dto.consequence);
        if (dto.notes is not null) inc.Notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("incidents/{id:guid}")]
    public async Task<IActionResult> DeleteIncident(Guid childId, Guid id)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var inc = await _db.DailyIncidents.Include(i => i.DailyEntry).FirstOrDefaultAsync(i => i.Id == id);
        if (inc is null || inc.DailyEntry is null || inc.DailyEntry.ChildId != childId) return NotFound();

        var entryId = inc.DailyEntryId;
        _db.DailyIncidents.Remove(inc);
        await _db.SaveChangesAsync();

        var cnt = await _db.DailyIncidents.CountAsync(x => x.DailyEntryId == entryId);
        var entry = await _db.DailyEntries.FindAsync(entryId);
        if (entry is not null)
        {
            entry.IncidentsCount = cnt;
            await _db.SaveChangesAsync();
        }
        return NoContent();
    }

    // ─────────── медикаменты (create/update/delete) ───────────

    [HttpPost("med-intakes")]
    public async Task<IActionResult> AddMed(Guid childId, [FromQuery] DateTime dateUtc, [FromBody] MedIntakeDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var d = DayFromUtc(dateUtc);

        var e = await _db.DailyEntries.FirstOrDefaultAsync(x => x.ChildId == childId && x.Date == d)
              ?? _db.DailyEntries.Add(new DailyEntry { ChildId = childId, Date = d }).Entity;

        var m = new DailyMedIntake
        {
            DailyEntryId = e.Id,
            Drug = dto.drug.Trim(),
            Dose = string.IsNullOrWhiteSpace(dto.dose) ? null : dto.dose.Trim(),
            TimeUtc = DateTime.SpecifyKind(dto.timeUtc, DateTimeKind.Utc),
            Taken = dto.taken,
            SideEffectsJson = SerializeOrNull(dto.sideEffects)
        };

        if (string.IsNullOrWhiteSpace(m.Drug) || m.Drug.Length > 200)
            return Problem("drug is required (<=200)", statusCode:400);

        _db.DailyMedIntakes.Add(m);
        await _db.SaveChangesAsync();
        return Ok(new { m.Id });
    }

    public record MedUpdateDto(string? dose, bool? taken, string[]? sideEffects);
    [HttpPatch("med-intakes/{id:guid}")]
    public async Task<IActionResult> UpdateMed(Guid childId, Guid id, [FromBody] MedUpdateDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var m = await _db.DailyMedIntakes.Include(x => x.DailyEntry).FirstOrDefaultAsync(x => x.Id == id);
        if (m is null || m.DailyEntry is null || m.DailyEntry.ChildId != childId) return NotFound();

        if (dto.dose is not null) m.Dose = string.IsNullOrWhiteSpace(dto.dose) ? null : dto.dose.Trim();
        if (dto.taken is { } tk) m.Taken = tk;
        if (dto.sideEffects is { }) m.SideEffectsJson = SerializeOrNull(dto.sideEffects);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("med-intakes/{id:guid}")]
    public async Task<IActionResult> DeleteMed(Guid childId, Guid id)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var m = await _db.DailyMedIntakes.Include(x => x.DailyEntry).FirstOrDefaultAsync(x => x.Id == id);
        if (m is null || m.DailyEntry is null || m.DailyEntry.ChildId != childId) return NotFound();

        _db.DailyMedIntakes.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─────────── сессии (create/update/delete) ───────────

    [HttpPost("sessions")]
    public async Task<IActionResult> AddSession(Guid childId, [FromQuery] DateTime dateUtc, [FromBody] SessionDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var d = DayFromUtc(dateUtc);

        if (string.IsNullOrWhiteSpace(dto.type) || dto.type.Length > 80)
            return Problem("type is required (<=80)", statusCode:400);
        if (dto.durationMin < 1 || dto.durationMin > 600)
            return Problem("durationMin 1..600", statusCode:400);
        if (dto.quality is { } q && (q < 1 || q > 5))
            return Problem("quality 1..5", statusCode:400);

        var e = await _db.DailyEntries.FirstOrDefaultAsync(x => x.ChildId == childId && x.Date == d)
              ?? _db.DailyEntries.Add(new DailyEntry { ChildId = childId, Date = d }).Entity;

        var s = new DailySession
        {
            DailyEntryId = e.Id,
            Type = dto.type.Trim(),
            DurationMin = dto.durationMin,
            Quality = dto.quality,
            GoalTagsJson = SerializeOrNull(dto.goalTags),
            Notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim()
        };

        _db.DailySessions.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new { s.Id });
    }

    public record SessionUpdateDto(int? durationMin, int? quality, string[]? goalTags, string? notes);
    [HttpPatch("sessions/{id:guid}")]
    public async Task<IActionResult> UpdateSession(Guid childId, Guid id, [FromBody] SessionUpdateDto dto)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var s = await _db.DailySessions.Include(x => x.DailyEntry).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null || s.DailyEntry is null || s.DailyEntry.ChildId != childId) return NotFound();

        if (dto.durationMin is { } dm && (dm < 1 || dm > 600)) return Problem("durationMin 1..600", statusCode:400);
        if (dto.quality is { } q && (q < 1 || q > 5)) return Problem("quality 1..5", statusCode:400);

        if (dto.durationMin is { } dmin) s.DurationMin = dmin;
        if (dto.quality is { } qq) s.Quality = qq;
        if (dto.goalTags is { }) s.GoalTagsJson = SerializeOrNull(dto.goalTags);
        if (dto.notes is not null) s.Notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid childId, Guid id)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var s = await _db.DailySessions.Include(x => x.DailyEntry).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null || s.DailyEntry is null || s.DailyEntry.ChildId != childId) return NotFound();

        _db.DailySessions.Remove(s);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ─────────── месячная/недельная аналитика ───────────

    public record WeeklySummary(
        double avgSleep, int totalIncidents,
        Dictionary<string,int> topTriggers,
        double? medAdherence
    );

    [HttpGet("summary/week")]
    public async Task<ActionResult<WeeklySummary>> GetWeek(Guid childId, DateTime weekStartUtc)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var start = DayFromUtc(weekStartUtc);
        var end = start.AddDays(6);

        var entries = await _db.DailyEntries
            .Where(x => x.ChildId == childId && x.Date >= start && x.Date <= end)
            .Include(x => x.Incidents)
            .Include(x => x.MedIntakes)
            .ToListAsync();

        var avgSleep = entries.Where(e => e.SleepTotalHours.HasValue).DefaultIfEmpty()
            .Average(e => e?.SleepTotalHours ?? 0);

        var totalIncidents = entries.Sum(e => e.Incidents.Count);

        var dict = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        foreach (var inc in e.Incidents)
            foreach (var t in DeserializeOrEmpty(inc.AntecedentJson))
                dict[t] = dict.TryGetValue(t, out var c) ? c+1 : 1;

        var topTriggers = dict.OrderByDescending(kv => kv.Value).Take(5)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        var meds = entries.SelectMany(e => e.MedIntakes).ToList();
        double? medAdh = meds.Count == 0 ? null : Math.Round(meds.Count(m => m.Taken) * 1.0 / meds.Count, 2);

        return Ok(new WeeklySummary(Math.Round(avgSleep,2), totalIncidents, topTriggers, medAdh));
    }

    public record MonthlySummary(int daysWithEntries, double avgSleep, int incidents);
    [HttpGet("summary/month")]
    public async Task<ActionResult<MonthlySummary>> GetMonth(Guid childId, int year, int month)
    {
        if (!await ChildBelongsToCurrentParent(childId)) return Forbid();
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        var entries = await _db.DailyEntries
            .Where(x => x.ChildId == childId && x.Date >= first && x.Date <= last)
            .Include(x => x.Incidents)
            .ToListAsync();

        var daysWithEntries = entries.Count;
        var avgSleep = entries.Where(e => e.SleepTotalHours.HasValue).DefaultIfEmpty()
            .Average(e => e?.SleepTotalHours ?? 0);
        var incidents = entries.Sum(e => e.Incidents.Count);

        return Ok(new MonthlySummary(daysWithEntries, Math.Round(avgSleep,2), incidents));
    }

    // ─────────── пресеты (для чипов в UI) ───────────
    public record PresetsVm(string[] antecedent, string[] behavior, string[] consequence, string[] triggers, string[] environments, string[] sessionTypes);
    [HttpGet("presets")]
    [AllowAnonymous] // можно и без авторизации; если что — убери
    public ActionResult<PresetsVm> GetPresets()
    {
        // позже можно вынести в БД/конфиг
        var antecedent = new[] { "шум", "ожидание", "смена_активности", "толпа", "голод", "усталость" };
        var behavior   = new[] { "крик", "бросание", "бегство", "укус", "удар", "повторение" };
        var consequence= new[] { "тайм-аут", "объятие", "переключение", "визуальная_подсказка", "сенсорный_перерыв" };
        var triggers   = new[] { "шум", "яркий_свет", "поездка", "гости", "новое_место", "ожидание" };
        var env        = new[] { "садик", "дом", "улица", "центр", "магазин", "поездка" };
        var sessions   = new[] { "логопед", "ABA", "ЛФК", "сенсорная", "дефектолог" };

        return Ok(new PresetsVm(antecedent, behavior, consequence, triggers, env, sessions));
    }
}
