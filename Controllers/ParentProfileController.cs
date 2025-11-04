// Controllers/ParentProfileController.cs
using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Contracts;
using SharpAuthDemo.Data;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/profile")]
[Authorize(Roles = "Parent")]
public class ParentProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParentProfileController(AppDbContext db, UserManager<ApplicationUser> userManager)
    { _db = db; _userManager = userManager; }

    [HttpGet]
    public async Task<ActionResult<ParentProfileResponse>> GetMy()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var profile = await _db.ParentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (profile is null) return NotFound();

        return new ParentProfileResponse(
            profile.Id, profile.UserId, profile.FirstName, profile.LastName,
            profile.CountryCode, profile.City, profile.AddressLine1, profile.AddressLine2, profile.Phone
        );
    }

    [HttpPost] // upsert
    public async Task<ActionResult<ParentProfileResponse>> Upsert(UpsertParentProfileRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var profile = await _db.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile is null)
        {
            profile = new ParentProfile { UserId = user.Id };
            _db.ParentProfiles.Add(profile);
        }

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
