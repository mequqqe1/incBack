// Controllers/CaregiversController.cs
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
[Route("api/parent/caregivers")]
[Authorize(Roles = "Parent")]
public class CaregiversController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFamilyContextService _familyContext;

    public CaregiversController(AppDbContext db, UserManager<ApplicationUser> userManager, IFamilyContextService familyContext)
    { _db = db; _userManager = userManager; _familyContext = familyContext; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CaregiverResponse>>> List()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var items = await _db.CaregiverMembers.AsNoTracking()
            .Where(m => m.ParentProfileId == family.ParentProfileId)
            .OrderByDescending(m => m.IsAdmin).ThenBy(m => m.Email)
            .Select(m => new CaregiverResponse(
                m.Id, m.Email, m.UserId, m.Relation, m.IsAdmin, m.Status, m.InvitedAtUtc, m.AcceptedAtUtc))
            .ToListAsync();

        return items;
    }

    [HttpPost("invite")]
    public async Task<ActionResult<CaregiverResponse>> Invite(InviteCaregiverRequest req)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        if (!await _familyContext.CanManageMembersAsync(me.Id))
            return Forbid();

        var family = await _familyContext.GetCurrentFamilyAsync(me.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var existing = await _db.CaregiverMembers
            .FirstOrDefaultAsync(m => m.ParentProfileId == family.ParentProfileId && m.Email == req.Email);
        if (existing is not null)
            return Conflict(new { error = "Caregiver with this email already invited/added" });

        var targetUser = await _userManager.FindByEmailAsync(req.Email);
        var status = targetUser is null ? CaregiverStatus.Pending : CaregiverStatus.Active;

        var member = new CaregiverMember
        {
            ParentProfileId = family.ParentProfileId,
            Email = req.Email,
            Relation = req.Relation,
            IsAdmin = req.IsAdmin,
            Status = status,
            UserId = targetUser?.Id,
            AcceptedAtUtc = targetUser is null ? null : DateTime.UtcNow
        };

        _db.CaregiverMembers.Add(member);
        await _db.SaveChangesAsync();

        return new CaregiverResponse(member.Id, member.Email, member.UserId, member.Relation,
            member.IsAdmin, member.Status, member.InvitedAtUtc, member.AcceptedAtUtc);
    }

    [HttpPost("{memberId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid memberId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var member = await _db.CaregiverMembers.FirstOrDefaultAsync(m => m.Id == memberId);
        if (member is null) return NotFound();

        if (!string.Equals(member.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        member.UserId = user.Id;
        member.Status = CaregiverStatus.Accepted;
        member.AcceptedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{memberId:guid}")]
    public async Task<ActionResult<CaregiverResponse>> Update(Guid memberId, UpdateCaregiverRequest req)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        if (!await _familyContext.CanManageMembersAsync(me.Id))
            return Forbid();

        var member = await _db.CaregiverMembers
            .Include(m => m.ParentProfile)
            .FirstOrDefaultAsync(m => m.Id == memberId);
        if (member is null) return NotFound();

        var family = await _familyContext.GetCurrentFamilyAsync(me.Id);
        if (family is null || family.ParentProfileId != member.ParentProfileId)
            return Forbid();

        if (req.Relation is not null) member.Relation = req.Relation;
        if (req.IsAdmin.HasValue) member.IsAdmin = req.IsAdmin.Value;
        if (req.Status.HasValue)
        {
            member.Status = req.Status.Value;
            if (req.Status.Value == CaregiverStatus.Revoked)
                member.RevokedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new CaregiverResponse(member.Id, member.Email, member.UserId, member.Relation,
            member.IsAdmin, member.Status, member.InvitedAtUtc, member.AcceptedAtUtc);
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> Remove(Guid memberId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        if (!await _familyContext.CanManageMembersAsync(me.Id))
            return Forbid();

        var member = await _db.CaregiverMembers.Include(m => m.ParentProfile)
            .FirstOrDefaultAsync(m => m.Id == memberId);
        if (member is null) return NotFound();

        var family = await _familyContext.GetCurrentFamilyAsync(me.Id);
        if (family is null || family.ParentProfileId != member.ParentProfileId)
            return Forbid();

        _db.CaregiverMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
