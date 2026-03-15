using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Services;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/parent/family")]
[Authorize(Roles = "Parent")]
public class ParentFamilyController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IFamilyContextService _familyContext;

    public ParentFamilyController(UserManager<ApplicationUser> userManager, AppDbContext db, IFamilyContextService familyContext)
    {
        _userManager = userManager;
        _db = db;
        _familyContext = familyContext;
    }

    /// <summary>Текущая семья: моя роль, владелец, список участников с ролями.</summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetMyFamily()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null)
            return Ok(new { inFamily = false, message = "Create or join a family first (create profile or accept invite)" });

        var profile = await _db.ParentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == family.ParentProfileId);
        if (profile is null) return NotFound();

        var owner = await _userManager.FindByIdAsync(family.OwnerUserId);
        var caregiverList = await _db.CaregiverMembers
            .AsNoTracking()
            .Where(m => m.ParentProfileId == family.ParentProfileId)
            .OrderByDescending(m => m.IsAdmin).ThenBy(m => m.Email)
            .Select(m => new { m.Id, m.Email, m.UserId, m.Relation, m.IsAdmin, m.Status, m.InvitedAtUtc, m.AcceptedAtUtc })
            .ToListAsync();

        var ownerEntry = new
        {
            id = (Guid?)null,
            email = owner?.Email ?? "",
            userId = family.OwnerUserId,
            relation = (string?)null,
            isAdmin = true,
            status = (int)CaregiverStatus.Active,
            invitedAtUtc = profile.CreatedAtUtc,
            acceptedAtUtc = (DateTime?)DateTime.UtcNow,
            role = "owner"
        };

        var memberEntries = caregiverList.Select(m => new
        {
            m.Id,
            m.Email,
            m.UserId,
            m.Relation,
            m.IsAdmin,
            m.Status,
            m.InvitedAtUtc,
            m.AcceptedAtUtc,
            role = m.IsAdmin ? "admin" : "caregiver"
        }).ToList();

        return Ok(new
        {
            inFamily = true,
            myRole = family.Role.ToString().ToLowerInvariant(),
            parentProfileId = family.ParentProfileId,
            ownerUserId = family.OwnerUserId,
            ownerEmail = owner?.Email,
            ownerFullName = owner?.FullName,
            canManageMembers = family.Role is FamilyRole.Owner or FamilyRole.Admin,
            members = new object[] { ownerEntry }.Concat(memberEntries).ToArray()
        });
    }
}
