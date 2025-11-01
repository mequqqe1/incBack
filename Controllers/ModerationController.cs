using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(Roles = "Admin")]
public class ModerationController : ControllerBase
{
    private readonly AppDbContext _db;

    public ModerationController(AppDbContext db) => _db = db;

    [HttpGet("specialists/pending")]
    public ActionResult<IEnumerable<SpecialistProfileResponse>> ListPending(int take = 50, int skip = 0)
    {
        var q = _db.SpecialistProfiles
            .Where(p => p.Status == ModerationStatus.Pending)
            .OrderBy(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(p => new SpecialistProfileResponse(
                p.Id, p.UserId, p.About,
                p.CountryCode, p.City, p.AddressLine1, p.AddressLine2, p.Region, p.PostalCode,
                p.ExperienceYears, p.PricePerHour, p.Telegram, p.Phone, p.IsEmailPublic, p.AvatarMimeType,
                p.Status, p.ModerationComment, p.ModeratedAtUtc, p.CreatedAtUtc, p.UpdatedAtUtc,
                p.SpecialistSpecializations.Select(x => x.SpecializationId).ToArray(),
                p.SpecialistSkills.Select(x => x.SkillId).ToArray()));

        return Ok(q.ToList());
    }

    [HttpPatch("specialists/{userId}/status")]
    public async Task<IActionResult> UpdateStatus(string userId, ModerationUpdateRequest req)
    {
        var p = _db.SpecialistProfiles.FirstOrDefault(x => x.UserId == userId);
        if (p is null) return NotFound();

        p.Status = req.Status;
        p.ModerationComment = req.Comment;
        p.ModeratedAtUtc = DateTime.UtcNow;
        p.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}