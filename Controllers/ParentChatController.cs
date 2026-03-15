using System.ComponentModel.DataAnnotations;
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
[Route("api/parent/chat")]
[Authorize(Roles = "Parent")]
public class ParentChatController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IFamilyContextService _familyContext;

    public ParentChatController(UserManager<ApplicationUser> userManager, AppDbContext db, IFamilyContextService familyContext)
    {
        _userManager = userManager;
        _db = db;
        _familyContext = familyContext;
    }

    /// <summary>Список чатов со специалистами (последнее сообщение и время).</summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<object>>> ListConversations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var parentUserId = family.OwnerUserId;
        var list = await _db.ParentSpecialistConversations
            .AsNoTracking()
            .Where(c => c.ParentUserId == parentUserId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new
            {
                c.Id,
                SpecialistUserId = c.SpecialistUserId,
                LastMessageAt = c.UpdatedAtUtc,
                UnreadCount = c.Messages.Count(m => m.SenderUserId != parentUserId && m.ReadAtUtc == null)
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Сообщения чата со специалистом (по conversationId или по specialistUserId).</summary>
    [HttpGet("conversations/{conversationOrSpecialistId}/messages")]
    public async Task<ActionResult<IEnumerable<object>>> GetMessages(
        string conversationOrSpecialistId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var conv = await ResolveConversation(family.OwnerUserId, conversationOrSpecialistId);
        if (conv is null) return NotFound();

        take = Math.Clamp(take, 1, 200);
        var messages = await _db.ParentSpecialistMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conv.Id)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip).Take(take)
            .Select(m => new
            {
                m.Id,
                m.SenderUserId,
                m.Text,
                m.BookingId,
                m.CreatedAtUtc,
                m.ReadAtUtc
            })
            .ToListAsync();
        messages.Reverse();

        return Ok(messages);
    }

    /// <summary>Отправить сообщение специалисту. Создаёт чат при первом сообщении.</summary>
    [HttpPost("conversations/{specialistUserId}/messages")]
    public async Task<ActionResult<object>> SendMessage(string specialistUserId, [FromBody] SendMessageRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var conv = await GetOrCreateConversation(family.OwnerUserId, specialistUserId);
        if (conv is null) return BadRequest(new { error = "Specialist not found or not approved" });

        var msg = new ParentSpecialistMessage
        {
            ConversationId = conv.Id,
            SenderUserId = user.Id,
            Text = req.Text.Trim(),
            BookingId = req.BookingId
        };
        _db.ParentSpecialistMessages.Add(msg);
        conv.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { msg.Id, msg.CreatedAtUtc });
    }

    /// <summary>Отметить сообщения от специалиста как прочитанные.</summary>
    [HttpPost("conversations/{conversationOrSpecialistId}/read")]
    public async Task<IActionResult> MarkRead(string conversationOrSpecialistId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var family = await _familyContext.GetCurrentFamilyAsync(user.Id);
        if (family is null) return NotFound(new { error = "No family" });

        var conv = await ResolveConversation(family.OwnerUserId, conversationOrSpecialistId);
        if (conv is null) return NotFound();

        var now = DateTime.UtcNow;
        await _db.ParentSpecialistMessages
            .Where(m => m.ConversationId == conv.Id && m.SenderUserId != family.OwnerUserId && m.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAtUtc, now));

        return NoContent();
    }

    private async Task<ParentSpecialistConversation?> ResolveConversation(string parentUserId, string conversationOrSpecialistId)
    {
        if (Guid.TryParse(conversationOrSpecialistId, out var guid))
            return await _db.ParentSpecialistConversations
                .FirstOrDefaultAsync(c => c.Id == guid && c.ParentUserId == parentUserId);
        return await _db.ParentSpecialistConversations
            .FirstOrDefaultAsync(c => c.ParentUserId == parentUserId && c.SpecialistUserId == conversationOrSpecialistId);
    }

    private async Task<ParentSpecialistConversation?> GetOrCreateConversation(string parentUserId, string specialistUserId)
    {
        var approved = await _db.SpecialistProfiles
            .AnyAsync(p => p.UserId == specialistUserId && p.Status == ModerationStatus.Approved);
        if (!approved) return null;

        var conv = await _db.ParentSpecialistConversations
            .FirstOrDefaultAsync(c => c.ParentUserId == parentUserId && c.SpecialistUserId == specialistUserId);
        if (conv != null) return conv;

        conv = new ParentSpecialistConversation { ParentUserId = parentUserId, SpecialistUserId = specialistUserId };
        _db.ParentSpecialistConversations.Add(conv);
        await _db.SaveChangesAsync();
        return conv;
    }

    public record SendMessageRequest([Required, MinLength(1), MaxLength(4000)] string Text, Guid? BookingId = null);
}
