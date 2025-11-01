using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/specialist")]
[Authorize(Roles = "Specialist")]
public class SpecialistController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    private const int MaxAvatarBytes = 2 * 1024 * 1024;   // 2MB
    private const int MaxDiplomaBytes = 10 * 1024 * 1024; // 10MB

    public SpecialistController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<SpecialistProfileResponse>> GetMyProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var profile = await _db.SpecialistProfiles
            .Include(p => p.SpecialistSpecializations)
            .Include(p => p.SpecialistSkills)
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (profile is null)
        {
            profile = new SpecialistProfile { UserId = user.Id, City = "Алматы", AddressLine1 = "-", CountryCode = "KZ" };
            _db.SpecialistProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        return Ok(ToResponse(profile));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<SpecialistProfileResponse>> UpsertMyProfile(SpecialistProfileUpsertRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var profile = await _db.SpecialistProfiles
            .Include(p => p.SpecialistSpecializations)
            .Include(p => p.SpecialistSkills)
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (profile is null)
        {
            profile = new SpecialistProfile { UserId = user.Id };
            _db.SpecialistProfiles.Add(profile);
        }

        profile.About = req.About;
        profile.CountryCode = req.CountryCode;
        profile.City = req.City;
        profile.AddressLine1 = req.AddressLine1;
        profile.AddressLine2 = req.AddressLine2;
        profile.Region = req.Region;
        profile.PostalCode = req.PostalCode;
        profile.ExperienceYears = req.ExperienceYears;
        profile.PricePerHour = req.PricePerHour;
        profile.Telegram = req.Telegram;
        profile.Phone = req.Phone;
        profile.IsEmailPublic = req.IsEmailPublic;

        profile.Status = ModerationStatus.Pending;
        profile.ModerationComment = null;
        profile.ModeratedAtUtc = null;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(profile));
    }

    [HttpPut("profile/avatar-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<SpecialistProfileResponse>> UploadAvatar([FromForm] AvatarUploadForm form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var file = form.File;
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Empty file" });

        if (file.Length > MaxAvatarBytes)
            return BadRequest(new { error = "Avatar file too large (max 2MB)" });

        byte[] buffer;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            buffer = ms.ToArray();
        }
        var base64 = Convert.ToBase64String(buffer);

        var profile = await _db.SpecialistProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id)
                      ?? new SpecialistProfile { UserId = user.Id, City = "Алматы", AddressLine1 = "-", CountryCode = "KZ" };
        if (profile.Id == default) _db.SpecialistProfiles.Add(profile);

        profile.AvatarMimeType = file.ContentType;
        profile.AvatarBase64 = base64;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        profile.Status = ModerationStatus.Pending;
        profile.ModerationComment = null;
        profile.ModeratedAtUtc = null;

        _db.SpecialistProfiles.Update(profile);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(profile));
    }

    [HttpPost("profile/diplomas/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<DiplomaResponse>> UploadDiplomaFile([FromForm] DiplomaUploadForm form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var file = form.File;
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Empty file" });

        if (file.Length > MaxDiplomaBytes)
            return BadRequest(new { error = "Diploma file too large (max 10MB)" });

        byte[] buffer;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            buffer = ms.ToArray();
        }
        var base64 = Convert.ToBase64String(buffer);

        var profile = await _db.SpecialistProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id)
                      ?? new SpecialistProfile { UserId = user.Id, City = "Алматы", AddressLine1 = "-", CountryCode = "KZ" };
        if (profile.Id == default) _db.SpecialistProfiles.Add(profile);

        var d = new SpecialistDiploma
        {
            UserId = user.Id,
            SpecialistProfileId = profile.Id,
            Title = form.Title,
            FileName = string.IsNullOrWhiteSpace(form.FileName) ? file.FileName : form.FileName,
            MimeType = file.ContentType,
            Base64Data = base64,
            UploadedAtUtc = DateTime.UtcNow
        };

        _db.SpecialistDiplomas.Add(d);

        profile.Status = ModerationStatus.Pending;
        profile.ModerationComment = null;
        profile.ModeratedAtUtc = null;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new DiplomaResponse(d.Id, d.Title, d.FileName, d.MimeType, d.UploadedAtUtc));
    }


    [HttpGet("profile/diplomas")]
    public async Task<ActionResult<IEnumerable<DiplomaResponse>>> GetMyDiplomas()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var list = await _db.SpecialistDiplomas
            .Where(x => x.UserId == user.Id)
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAtUtc)
            .Select(d => new DiplomaResponse(d.Id, d.Title, d.FileName, d.MimeType, d.UploadedAtUtc))
            .ToListAsync();

        return Ok(list);
    }
    
    [HttpDelete("profile/diplomas/{id:guid}")]
    public async Task<IActionResult> DeleteMyDiploma(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var d = await _db.SpecialistDiplomas.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
        if (d is null) return NotFound();

        _db.SpecialistDiplomas.Remove(d);

        var profile = await _db.SpecialistProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (profile is not null)
        {
            profile.Status = ModerationStatus.Pending;
            profile.ModerationComment = null;
            profile.ModeratedAtUtc = null;
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("profile/specializations")]
    public async Task<IActionResult> SetMySpecializations(SetSpecializationsRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // гарантируем наличие профиля
        var profile = await _db.SpecialistProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (profile is null)
        {
            profile = new SpecialistProfile
            {
                UserId = user.Id,
                CountryCode = "KZ",
                City = "Алматы",
                AddressLine1 = "-"
            };
            _db.SpecialistProfiles.Add(profile);
            await _db.SaveChangesAsync(); // важно: чтобы у профиля был Id перед связями
        }

        // валидируем id из справочника
        var validIds = await _db.Specializations
            .Where(s => req.SpecializationIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();

        // удаляем старые связи пачкой (без трекинга) — избегаем concurrency
        await _db.SpecialistSpecializations
            .Where(x => x.SpecialistProfileId == profile.Id)
            .ExecuteDeleteAsync();

        // вставляем новые связи
        var links = validIds.Select(id => new SpecialistSpecialization
        {
            SpecialistProfileId = profile.Id,
            SpecializationId = id
        });
        await _db.SpecialistSpecializations.AddRangeAsync(links);

        // обновляем профиль (сброс модерации)
        profile.Status = ModerationStatus.Pending;
        profile.ModerationComment = null;
        profile.ModeratedAtUtc = null;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }


    [HttpPut("profile/skills")]
    public async Task<IActionResult> SetMySkills(SetSkillsRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var profile = await _db.SpecialistProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (profile is null)
        {
            profile = new SpecialistProfile
            {
                UserId = user.Id,
                CountryCode = "KZ",
                City = "Алматы",
                AddressLine1 = "-"
            };
            _db.SpecialistProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        var validIds = await _db.Skills
            .Where(s => req.SkillIds.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();

        await _db.SpecialistSkills
            .Where(x => x.SpecialistProfileId == profile.Id)
            .ExecuteDeleteAsync();

        var links = validIds.Select(id => new SpecialistSkill
        {
            SpecialistProfileId = profile.Id,
            SkillId = id
        });
        await _db.SpecialistSkills.AddRangeAsync(links);

        profile.Status = ModerationStatus.Pending;
        profile.ModerationComment = null;
        profile.ModeratedAtUtc = null;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }


    private static SpecialistProfileResponse ToResponse(SpecialistProfile p) =>
        new(
            p.Id, p.UserId, p.About,
            p.CountryCode, p.City, p.AddressLine1, p.AddressLine2, p.Region, p.PostalCode,
            p.ExperienceYears, p.PricePerHour, p.Telegram, p.Phone, p.IsEmailPublic, p.AvatarMimeType,
            p.Status, p.ModerationComment, p.ModeratedAtUtc, p.CreatedAtUtc, p.UpdatedAtUtc,
            p.SpecialistSpecializations.Select(x => x.SpecializationId).ToArray(),
            p.SpecialistSkills.Select(x => x.SkillId).ToArray()
        );

    private static bool TryGetBase64Size(string base64, out int bytes)
    {
        bytes = 0;
        try
        {
            var len = base64.Length;
            var padding = base64.EndsWith("==") ? 2 : base64.EndsWith("=") ? 1 : 0;
            bytes = (len * 3) / 4 - padding;
            return true;
        }
        catch { return false; }
    }
}
