// Controllers/ParentProfileController.cs
using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Contracts;
using SharpAuthDemo.Data;
using SharpAuthDemo.Services;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/profile")]
[Authorize(Roles = "Parent")]
public class ParentProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFamilyContextService _familyContext;

    public ParentProfileController(AppDbContext db, UserManager<ApplicationUser> userManager, IFamilyContextService familyContext)
    { _db = db; _userManager = userManager; _familyContext = familyContext; }

    [HttpGet]
    public async Task<ActionResult<ParentProfileResponse>> GetMy()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family. Create profile or accept invite." });

        var profile = await _db.ParentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == family.ParentProfileId);
        if (profile is null) return NotFound();

        return new ParentProfileResponse(
            profile.Id, profile.UserId, profile.FirstName, profile.LastName,
            profile.CountryCode, profile.City, profile.AddressLine1, profile.AddressLine2, profile.Phone
        );
    }

    [HttpPost] // upsert — только владелец семьи
    public async Task<ActionResult<ParentProfileResponse>> Upsert(UpsertParentProfileRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null)
        {
            var p = await _db.ParentProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (p is null)
            {
                p = new ParentProfile { UserId = user.Id };
                _db.ParentProfiles.Add(p);
                await _db.SaveChangesAsync();
            }
            family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        }

        if (family!.Role != FamilyRole.Owner)
            return Forbid(); // только владелец редактирует профиль семьи

        var profile = await _db.ParentProfiles.FirstOrDefaultAsync(p => p.Id == family.ParentProfileId);
        if (profile is null) return NotFound();

        profile.FirstName = req.FirstName ?? profile.FirstName;
        profile.LastName = req.LastName ?? profile.LastName;
        profile.CountryCode = req.CountryCode ?? profile.CountryCode;
        profile.City = req.City ?? profile.City;
        profile.AddressLine1 = req.AddressLine1 ?? profile.AddressLine1;
        profile.AddressLine2 = req.AddressLine2 ?? profile.AddressLine2;
        profile.Phone = req.Phone ?? profile.Phone;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new ParentProfileResponse(
            profile.Id, profile.UserId, profile.FirstName, profile.LastName,
            profile.CountryCode, profile.City, profile.AddressLine1, profile.AddressLine2, profile.Phone
        );
    }
}
