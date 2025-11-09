// Services/ZeynAIService.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharpAuthDemo.Data;
using INCBack.Models;
using INCBack.Models.Tracker;
using INCBack.Models.ZeynAI;

public class ZeynAIService : IZeynAIService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ZeynAIHub> _hub;
    private readonly OpenAIOptions _opt;
    private readonly IHttpClientFactory _http;
    private readonly IZeynAIAccess _access;

    public ZeynAIService(
        AppDbContext db,
        IHubContext<ZeynAIHub> hub,
        IOptions<OpenAIOptions> opt,
        IHttpClientFactory http,
        IZeynAIAccess access)
    {
        _db = db; _hub = hub; _opt = opt.Value; _http = http; _access = access;
    }

    public async Task<Guid> CreateOrGetConversationAsync(string userId, Guid childId, string? title = null, CancellationToken ct = default)
    {
        if (!await _access.CanAccessChildAsync(userId, childId, ct))
            throw new UnauthorizedAccessException("No access to child");

        var existing = await _db.AIConversations
            .Where(c => c.ParentUserId == userId && c.ChildId == childId && !c.Archived)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (existing != null) return existing.Id;

        var conv = new AIConversation
        {
            ParentUserId = userId,
            ChildId = childId,
            Title = title ?? "Диалог с ZeynAI"
        };
        _db.AIConversations.Add(conv);
        await _db.SaveChangesAsync(ct);
        return conv.Id;
    }

    public async Task<bool> UserOwnsConversationAsync(Guid conversationId, string userId, CancellationToken ct = default)
    {
        return await _db.AIConversations.AnyAsync(c => c.Id == conversationId && c.ParentUserId == userId, ct);
    }

    public async Task RunChatAsync(Guid conversationId, string userId, string userMessage, CancellationToken ct = default)
    {
        // 1) загрузка беседы и проверка прав
        var conv = await _db.AIConversations
            .Include(c => c.Child)
            .SingleOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conv == null || conv.ParentUserId != userId)
            throw new UnauthorizedAccessException("Conversation not found or not yours");

        // контроль доступа к ребёнку (на случай смены владения)
        if (!await _access.CanAccessChildAsync(userId, conv.ChildId, ct))
            throw new UnauthorizedAccessException("No access to child");

        var childId = conv.ChildId;

        // 2) сохраняем сообщение пользователя
        var userMsg = new AIMessage
        {
            ConversationId = conv.Id,
            Role = AIMessageRole.User,
            Content = userMessage
        };
        _db.AIMessages.Add(userMsg);
        conv.UpdatedAtUtc = DateTime.UtcNow;
        conv.TurnCount++;
        await _db.SaveChangesAsync(ct);

        // 3) собираем контекст для модели
        var (systemPrompt, messages) = await BuildPromptAsync(conv, ct);

        // 4) стримим из OpenAI в SignalR
        var assistantText = await StreamFromOpenAIAsync(conversationId, systemPrompt, messages, ct);

        // 5) сохраняем ответ ассистента
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _db.AIMessages.Add(new AIMessage
            {
                ConversationId = conv.Id,
                Role = AIMessageRole.Assistant,
                Content = assistantText,
                Model = _opt.Model
            });
            conv.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // 6) авто-саммаризация по порогу длины — опционально
        // if (conv.TurnCount >= 60) { await SummarizeAsync(conv, ct); }
    }

    // ===================== PRIVATE =====================

    private async Task<(string systemPrompt, List<object> messages)> BuildPromptAsync(AIConversation conv, CancellationToken ct)
    {
        var child = conv.Child!;
        var childDto = new {
            child.FirstName, child.LastName, child.BirthDate, child.Sex,
            child.PrimaryDiagnosis, child.SupportLevel, child.NonVerbal, child.CommunicationMethod
        };

        // История (последние 40 сообщений)
        var history = await _db.AIMessages
            .Where(m => m.ConversationId == conv.Id)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(40)
            .Select(m => new { m.Role, m.Content })
            .ToListAsync(ct);

        // Трекер — 14 дней
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var days = await _db.Set<DailyEntry>()
            .Where(d => d.ChildId == conv.ChildId && d.Date >= from)
            .OrderByDescending(d => d.Date)
            .Take(14)
            .Select(d => new {
                d.Date, d.SleepTotalHours, d.SleepLatencyMin, d.NightWakings, d.SleepQuality,
                d.Mood, d.Anxiety, d.SensoryOverload,
                d.MealsCount, d.Appetite, d.ToiletingStatus,
                d.ParentNote, d.IncidentsCount
            }).ToListAsync(ct);

        // Итоги визитов — 5 шт.
        var outcomes = await _db.Set<BookingOutcome>()
            .Where(o => o.ParentUserId == conv.ParentUserId)
            .OrderByDescending(o => o.CreatedAtUtc).Take(5)
            .Select(o => new { o.CreatedAtUtc, o.Summary, o.Recommendations, o.NextSteps })
            .ToListAsync(ct);

        var systemPrompt = """
            Ты — ZeynAI, помощник для родителей детей с особенностями развития.
            Говори ясно, коротко, по делу. Не ставь диагнозов. Предлагай практичные,
            безопасные шаги и напоминания. Если данных не хватает — вежливо уточняй.
        """;

        var contextJson = JsonSerializer.Serialize(new { child = childDto, tracker = days, visits = outcomes });

        var msgs = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = $"Контекст JSON: {contextJson}" },
        };

        // превращаем историю в chat messages
        foreach (var m in history)
        {
            var role = m.Role switch
            {
                AIMessageRole.User => "user",
                AIMessageRole.Assistant => "assistant",
                AIMessageRole.System => "system",
                AIMessageRole.Tool => "tool",
                _ => "user"
            };
            msgs.Add(new { role, content = m.Content });
        }

        return (systemPrompt, msgs);
    }

    private async Task<string> StreamFromOpenAIAsync(Guid conversationId, string systemPrompt, List<object> messages, CancellationToken ct)
    {
        using var client = _http.CreateClient();
        client.BaseAddress = new Uri("https://api.openai.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);

        var body = new
        {
            model = _opt.Model,
            stream = true,
            messages = messages // уже включает system + context + history
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var group = ZeynAIHub.GroupName(conversationId);
        var sb = new StringBuilder();

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data:")) continue;

            var payload = line.Substring(5).Trim();
            if (payload == "[DONE]") break;

            try
            {
                using var json = JsonDocument.Parse(payload);
                var root = json.RootElement;
                var delta = root.GetProperty("choices")[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var contentEl))
                {
                    var piece = contentEl.GetString();
                    if (!string.IsNullOrEmpty(piece))
                    {
                        sb.Append(piece);
                        await _hub.Clients.Group(group).SendAsync("token", piece, ct);
                    }
                }
            }
            catch
            {
                // неконтентные чанки / heartbeat — игнор
            }
        }

        await _hub.Clients.Group(group).SendAsync("done", ct);
        return sb.ToString();
    }

    // (опционально) саммаризация длинной беседы
    // private async Task SummarizeAsync(AIConversation conv, CancellationToken ct) { ... }
}
