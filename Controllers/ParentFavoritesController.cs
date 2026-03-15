using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;
using SharpAuthDemo.Services;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/favorites")]
[Authorize(Roles = "Parent")]
public class ParentFavoritesController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IFamilyContextService _familyContext;

    public ParentFavoritesController(UserManager<ApplicationUser> userManager, AppDbContext db, IFamilyContextService familyContext)
    {
        _userManager = userManager;
        _db = db;
        _familyContext = familyContext;
    }

    /// <summary>Список избранных специалистов (id + базовая инфа для карточки).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var specialistUserIds = await _db.ParentFavoriteSpecialists
            .AsNoTracking()
            .Where(f => f.ParentUserId == family.OwnerUserId)
            .Select(f => f.SpecialistUserId)
            .ToListAsync();

        if (specialistUserIds.Count == 0)
            return Ok(Array.Empty<object>());

        var profiles = await _db.SpecialistProfiles
            .AsNoTracking()
            .Where(p => p.Status == ModerationStatus.Approved && specialistUserIds.Contains(p.UserId))
            .Select(p => new
            {
                p.UserId,
                FullName = p.UserId,
                p.City,
                p.PricePerHour,
                p.About,
                Specializations = p.SpecialistSpecializations.Select(s => s.Specialization.Name).ToList(),
                Skills = p.SpecialistSkills.Select(s => s.Skill.Name).ToList(),
                HasAvatar = p.AvatarMimeType != null
            })
            .ToListAsync();

        return Ok(profiles);
    }

    /// <summary>Добавить специалиста в избранное.</summary>
    [HttpPost("{specialistUserId}")]
    public async Task<IActionResult> Add(string specialistUserId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var exists = await _db.SpecialistProfiles
            .AnyAsync(p => p.UserId == specialistUserId && p.Status == ModerationStatus.Approved);
        if (!exists) return NotFound(new { error = "Specialist not found or not approved" });

        var already = await _db.ParentFavoriteSpecialists
            .AnyAsync(f => f.ParentUserId == family.OwnerUserId && f.SpecialistUserId == specialistUserId);
        if (already) return NoContent();

        _db.ParentFavoriteSpecialists.Add(new ParentFavoriteSpecialist
        {
            ParentUserId = family.OwnerUserId,
            SpecialistUserId = specialistUserId
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Удалить из избранного.</summary>
    [HttpDelete("{specialistUserId}")]
    public async Task<IActionResult> Remove(string specialistUserId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var deleted = await _db.ParentFavoriteSpecialists
            .Where(f => f.ParentUserId == family.OwnerUserId && f.SpecialistUserId == specialistUserId)
            .ExecuteDeleteAsync();

        return deleted > 0 ? NoContent() : NotFound();
    }

    /// <summary>Проверить, в избранном ли специалист.</summary>
    [HttpGet("{specialistUserId}")]
    public async Task<ActionResult<object>> IsFavorite(string specialistUserId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var isFav = await _db.ParentFavoriteSpecialists
            .AnyAsync(f => f.ParentUserId == family.OwnerUserId && f.SpecialistUserId == specialistUserId);

        return Ok(new { isFavorite = isFav });
    }
}
