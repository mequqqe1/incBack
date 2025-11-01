using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/specialists")]
public class PublicSpecialistsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicSpecialistsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Каталог одобренных специалистов с фильтрацией по городу, специализации и навыку.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SpecialistCatalogItem>>> GetApproved(
        [FromQuery] string? city,
        [FromQuery] int? specializationId,
        [FromQuery] int? skillId,
        [FromQuery] string? q)
    {
        var query = _db.SpecialistProfiles
            .AsNoTracking()
            .Where(p => p.Status == ModerationStatus.Approved);

        // Фильтр по городу
        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.ToLower();
            query = query.Where(p => (p.City ?? "").ToLower().Contains(c));
        }

        // Фильтр по специализации
        if (specializationId is not null)
        {
            query = query.Where(p =>
                p.SpecialistSpecializations.Any(s => s.SpecializationId == specializationId));
        }

        // Фильтр по навыку
        if (skillId is not null)
        {
            query = query.Where(p =>
                p.SpecialistSkills.Any(s => s.SkillId == skillId));
        }

        // Поиск по свободному тексту (About/City)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLower();
            query = query.Where(p =>
                (p.About ?? "").ToLower().Contains(lower) ||
                (p.City  ?? "").ToLower().Contains(lower));
        }

        // Если нужно явно подтянуть названия (для серверной проекции не обязательно, но можно):
        // .Include(p => p.SpecialistSpecializations).ThenInclude(ss => ss.Specialization)
        // .Include(p => p.SpecialistSkills).ThenInclude(sk => sk.Skill)

        var list = await query
            .OrderBy(p => p.UserId)
            .Select(p => new SpecialistCatalogItem(
                p.UserId,
                /* FullName */ p.UserId, // замените на реальное имя, если появится
                p.City ?? "",
                p.PricePerHour,
                p.About,
                p.SpecialistSpecializations.Select(s => s.Specialization.Name).ToList(),
                p.SpecialistSkills.Select(s => s.Skill.Name).ToList(),
                p.AvatarMimeType != null
            ))
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Публичный профиль специалиста по ID.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<SpecialistPublicProfile>> GetById(string userId)
    {
        var p = await _db.SpecialistProfiles
            .AsNoTracking()
            .Include(p => p.SpecialistSpecializations).ThenInclude(ss => ss.Specialization)
            .Include(p => p.SpecialistSkills).ThenInclude(sk => sk.Skill)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == ModerationStatus.Approved);

        if (p is null)
            return NotFound(new { error = "Specialist not found or not approved" });

        return Ok(new SpecialistPublicProfile(
            p.UserId,
            /* FullName */ p.UserId, // замените на реальное имя, если появится
            p.City ?? "",
            p.About,
            p.PricePerHour,
            p.Telegram,
            p.Phone,
            p.SpecialistSpecializations.Select(s => s.Specialization.Name).ToList(),
            p.SpecialistSkills.Select(s => s.Skill.Name).ToList(),
            p.AvatarMimeType != null
        ));
    }

    /// <summary>
    /// Свободные слоты специалиста за период.
    /// </summary>
    [HttpGet("{userId}/availability")]
    public async Task<ActionResult<IEnumerable<AvailabilitySlotResponse>>> FreeSlots(
        string userId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc)
    {
        var approved = await _db.SpecialistProfiles
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.Status == ModerationStatus.Approved);

        if (!approved)
            return NotFound(new { error = "Specialist not found or not approved" });

        var list = await _db.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.SpecialistUserId == userId &&
                        !s.IsBooked &&
                        s.StartsAtUtc < toUtc &&
                        s.EndsAtUtc   > fromUtc)
            .OrderBy(s => s.StartsAtUtc)
            .Select(s => new AvailabilitySlotResponse(
                s.Id,
                s.StartsAtUtc,
                s.EndsAtUtc,
                s.IsBooked,
                s.Note
            ))
            .ToListAsync();

        return Ok(list);
    }
}

/// <summary>
/// DTO для каталога специалистов
/// </summary>
public record SpecialistCatalogItem(
    string UserId,
    string FullName,
    string City,
    decimal? PricePerHour,
    string? About,
    List<string> Specializations,
    List<string> Skills,
    bool HasAvatar
);

/// <summary>
/// DTO для публичного профиля специалиста
/// </summary>
public record SpecialistPublicProfile(
    string UserId,
    string FullName,
    string City,
    string? About,
    decimal? PricePerHour,
    string? Telegram,
    string? Phone,
    List<string> Specializations,
    List<string> Skills,
    bool HasAvatar
);

/// <summary>
/// DTO для слотов доступности
/// </summary>
public record AvailabilitySlotResponse(
    Guid Id,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsBooked,
    string? Note
);
