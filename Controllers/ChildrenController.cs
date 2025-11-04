// Controllers/ChildrenController.cs
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
[Route("api/parent/children")]
[Authorize(Roles = "Parent")]
public class ChildrenController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProfileAccessService _access;

    public ChildrenController(AppDbContext db, UserManager<ApplicationUser> userManager, ProfileAccessService access)
    {
        _db = db; _userManager = userManager; _access = access;
    }

    private async Task<ParentProfile> GetOrCreateOwnProfile(ApplicationUser user)
    {
        var p = await _db.ParentProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (p is not null) return p;

        p = new ParentProfile { UserId = user.Id, CreatedAtUtc = DateTime.UtcNow };
        _db.ParentProfiles.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChildResponse>>> List()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var accessible = await _access.GetAccessibleProfileIdsAsync(me.Id);

        var items = await _db.Children.AsNoTracking()
            .Where(c => accessible.Contains(c.ParentProfileId))
            .OrderBy(c => c.FirstName).ThenBy(c => c.BirthDate)
            .Select(c => new ChildResponse(
                c.Id, c.FirstName, c.LastName, c.BirthDate, c.Sex, c.SupportLevel,
                c.PrimaryDiagnosis, c.NonVerbal, c.CommunicationMethod,
                c.Allergies, c.Medications, c.Triggers, c.CalmingStrategies,
                c.SchoolOrCenter, c.CurrentGoals
            ))
            .ToListAsync();

        return items;
    }

    [HttpPost]
    public async Task<ActionResult<ChildResponse>> Create(UpsertChildRequest req)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        // создаём ребёнка в своём собственном профиле (владельца)
        var profile = await GetOrCreateOwnProfile(me);

        var entity = new Child
        {
            ParentProfileId = profile.Id,
            FirstName = req.FirstName,
            LastName = req.LastName,
            BirthDate = req.BirthDate,
            Sex = req.Sex,
            SupportLevel = req.SupportLevel,
            PrimaryDiagnosis = req.PrimaryDiagnosis,
            NonVerbal = req.NonVerbal,
            CommunicationMethod = req.CommunicationMethod,
            Allergies = req.Allergies,
            Medications = req.Medications,
            Triggers = req.Triggers,
            CalmingStrategies = req.CalmingStrategies,
            SchoolOrCenter = req.SchoolOrCenter,
            CurrentGoals = req.CurrentGoals
        };

        _db.Children.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new ChildResponse(
            entity.Id, entity.FirstName, entity.LastName, entity.BirthDate, entity.Sex, entity.SupportLevel,
            entity.PrimaryDiagnosis, entity.NonVerbal, entity.CommunicationMethod,
            entity.Allergies, entity.Medications, entity.Triggers, entity.CalmingStrategies,
            entity.SchoolOrCenter, entity.CurrentGoals
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChildResponse>> GetById(Guid id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var accessible = await _access.GetAccessibleProfileIdsAsync(me.Id);

        var c = await _db.Children.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && accessible.Contains(x.ParentProfileId));

        if (c is null) return NotFound();

        return new ChildResponse(
            c.Id, c.FirstName, c.LastName, c.BirthDate, c.Sex, c.SupportLevel,
            c.PrimaryDiagnosis, c.NonVerbal, c.CommunicationMethod,
            c.Allergies, c.Medications, c.Triggers, c.CalmingStrategies,
            c.SchoolOrCenter, c.CurrentGoals
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChildResponse>> Update(Guid id, UpsertChildRequest req)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var accessible = await _access.GetAccessibleProfileIdsAsync(me.Id);

        var c = await _db.Children.FirstOrDefaultAsync(x => x.Id == id && accessible.Contains(x.ParentProfileId));
        if (c is null) return NotFound();

        c.FirstName = req.FirstName;
        c.LastName = req.LastName;
        c.BirthDate = req.BirthDate;
        c.Sex = req.Sex;
        c.SupportLevel = req.SupportLevel;
        c.PrimaryDiagnosis = req.PrimaryDiagnosis;
        c.NonVerbal = req.NonVerbal;
        c.CommunicationMethod = req.CommunicationMethod;
        c.Allergies = req.Allergies;
        c.Medications = req.Medications;
        c.Triggers = req.Triggers;
        c.CalmingStrategies = req.CalmingStrategies;
        c.SchoolOrCenter = req.SchoolOrCenter;
        c.CurrentGoals = req.CurrentGoals;
        c.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new ChildResponse(
            c.Id, c.FirstName, c.LastName, c.BirthDate, c.Sex, c.SupportLevel,
            c.PrimaryDiagnosis, c.NonVerbal, c.CommunicationMethod,
            c.Allergies, c.Medications, c.Triggers, c.CalmingStrategies,
            c.SchoolOrCenter, c.CurrentGoals
        );
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var accessible = await _access.GetAccessibleProfileIdsAsync(me.Id);

        var c = await _db.Children.FirstOrDefaultAsync(x => x.Id == id && accessible.Contains(x.ParentProfileId));
        if (c is null) return NotFound();

        _db.Children.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // заметки
    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<ChildNoteResponse>> AddNote(Guid id, ChildNoteRequest req)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();
        if (!await _access.CanAccessChildAsync(me.Id, id)) return Forbid();

        var exists = await _db.Children.AnyAsync(x => x.Id == id);
        if (!exists) return NotFound();

        var note = new ChildNote { ChildId = id, Text = req.Text };
        _db.ChildNotes.Add(note);
        await _db.SaveChangesAsync();
        return new ChildNoteResponse(note.Id, note.Text, note.CreatedAtUtc);
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<ActionResult<IEnumerable<ChildNoteResponse>>> ListNotes(Guid id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me is null) return Unauthorized();
        if (!await _access.CanAccessChildAsync(me.Id, id)) return Forbid();

        var notes = await _db.ChildNotes.AsNoTracking()
            .Where(n => n.ChildId == id)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new ChildNoteResponse(n.Id, n.Text, n.CreatedAtUtc))
            .ToListAsync();

        return notes;
    }
}
