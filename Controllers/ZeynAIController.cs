// Controllers/ZeynAIConversationsController.cs
using INCBack.Models.ZeynAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharpAuthDemo.Data;

[ApiController]
[Route("api/zeynai")]
[Authorize(Roles = "Parent")]
public class ZeynAIConversationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IZeynAIService _svc;
    public ZeynAIConversationsController(AppDbContext db, IZeynAIService svc) { _db = db; _svc = svc; }

    // Список бесед (неархив)
    [HttpGet("conversations")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = User.FindFirst("sub")!.Value;
        var items = await _db.AIConversations
            .Where(c => c.ParentUserId == userId && !c.Archived)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new { c.Id, c.ChildId, c.Title, c.TurnCount, c.UpdatedAtUtc })
            .ToListAsync(ct);
        return Ok(items);
    }

    // Создать (или получить активную) для ребёнка
    public record CreateReq(Guid ChildId, string? Title);
    [HttpPost("conversations")]
    public async Task<IActionResult> Create([FromBody] CreateReq req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")!.Value;
        var id = await _svc.CreateOrGetConversationAsync(userId, req.ChildId, req.Title, ct);
        return Ok(new { id });
    }

    // История сообщений (пагинация)
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> Messages(Guid conversationId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var userId = User.FindFirst("sub")!.Value;
        var conv = await _db.AIConversations.SingleOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conv == null || conv.ParentUserId != userId) return NotFound();

        var msgs = await _db.AIMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAtUtc)
            .Skip(skip).Take(take)
            .Select(m => new { m.Id, role = m.Role.ToString().ToLower(), m.Content, m.CreatedAtUtc })
            .ToListAsync(ct);

        return Ok(msgs);
    }

    // Отправить сообщение пользователя и запустить стрим
    public record UserMsgReq(string Message);
    [HttpPost("conversations/{conversationId:guid}/send")]
    public async Task<IActionResult> Send(Guid conversationId, [FromBody] UserMsgReq req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")!.Value;
        await _svc.RunChatAsync(conversationId, userId, req.Message, ct);
        return Accepted();
    }

    // Архивирование / переименование
    public record PatchReq(string? Title, bool? Archived);
    [HttpPatch("conversations/{conversationId:guid}")]
    public async Task<IActionResult> Patch(Guid conversationId, [FromBody] PatchReq req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")!.Value;
        var conv = await _db.AIConversations.SingleOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conv == null || conv.ParentUserId != userId) return NotFound();

        if (req.Title != null) conv.Title = req.Title;
        if (req.Archived.HasValue) conv.Archived = req.Archived.Value;
        conv.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
