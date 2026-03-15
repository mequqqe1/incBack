using System.ComponentModel.DataAnnotations;
using INCBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;
using SharpAuthDemo.Models;

namespace SharpAuthDemo.Controllers;

[ApiController]
[Route("api/specialist/chat")]
[Authorize(Roles = "Specialist")]
public class SpecialistChatController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public SpecialistChatController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>Список чатов с родителями.</summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<object>>> ListConversations()
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var list = await _db.ParentSpecialistConversations
            .AsNoTracking()
            .Where(c => c.SpecialistUserId == spec.Id)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new
            {
                c.Id,
                ParentUserId = c.ParentUserId,
                LastMessageAt = c.UpdatedAtUtc,
                UnreadCount = c.Messages.Count(m => m.SenderUserId != spec.Id && m.ReadAtUtc == null)
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Сообщения чата с родителем.</summary>
    [HttpGet("conversations/{conversationOrParentId}/messages")]
    public async Task<ActionResult<IEnumerable<object>>> GetMessages(
        string conversationOrParentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var conv = await ResolveConversation(spec.Id, conversationOrParentId);
        if (conv is null) return NotFound();

        take = Math.Clamp(take, 1, 200);
        var messages = await _db.ParentSpecialistMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conv.Id)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip).Take(take)
            .Select(m => new { m.Id, m.SenderUserId, m.Text, m.BookingId, m.CreatedAtUtc, m.ReadAtUtc })
            .ToListAsync();
        messages.Reverse();

        return Ok(messages);
    }

    /// <summary>Отправить сообщение родителю.</summary>
    [HttpPost("conversations/{parentUserId}/messages")]
    public async Task<ActionResult<object>> SendMessage(string parentUserId, [FromBody] SpecialistSendMessageRequest req)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var conv = await _db.ParentSpecialistConversations
            .FirstOrDefaultAsync(c => c.SpecialistUserId == spec.Id && c.ParentUserId == parentUserId);
        if (conv is null) return NotFound(new { error = "No conversation with this parent. Parent must write first." });

        var msg = new ParentSpecialistMessage
        {
            ConversationId = conv.Id,
            SenderUserId = spec.Id,
            Text = req.Text.Trim(),
            BookingId = req.BookingId
        };
        _db.ParentSpecialistMessages.Add(msg);
        conv.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { msg.Id, msg.CreatedAtUtc });
    }

    /// <summary>Отметить сообщения от родителя как прочитанные.</summary>
    [HttpPost("conversations/{conversationOrParentId}/read")]
    public async Task<IActionResult> MarkRead(string conversationOrParentId)
    {
        var spec = await _userManager.GetUserAsync(User);
        if (spec is null) return Unauthorized();

        var conv = await ResolveConversation(spec.Id, conversationOrParentId);
        if (conv is null) return NotFound();

        var now = DateTime.UtcNow;
        await _db.ParentSpecialistMessages
            .Where(m => m.ConversationId == conv.Id && m.SenderUserId != spec.Id && m.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAtUtc, now));

        return NoContent();
    }

    private async Task<ParentSpecialistConversation?> ResolveConversation(string specialistUserId, string conversationOrParentId)
    {
        if (Guid.TryParse(conversationOrParentId, out var guid))
            return await _db.ParentSpecialistConversations
                .FirstOrDefaultAsync(c => c.Id == guid && c.SpecialistUserId == specialistUserId);
        return await _db.ParentSpecialistConversations
            .FirstOrDefaultAsync(c => c.SpecialistUserId == specialistUserId && c.ParentUserId == conversationOrParentId);
    }

    public record SpecialistSendMessageRequest([Required, MinLength(1), MaxLength(4000)] string Text, Guid? BookingId = null);
}
