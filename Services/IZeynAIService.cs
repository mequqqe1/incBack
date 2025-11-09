// Services/IZeynAIService.cs
public interface IZeynAIService
{
    Task<Guid> CreateOrGetConversationAsync(string userId, Guid childId, string? title = null, CancellationToken ct = default);
    Task RunChatAsync(Guid conversationId, string userId, string userMessage, CancellationToken ct = default);

    // Дополнительно (если удобно в контроллерах):
    Task<bool> UserOwnsConversationAsync(Guid conversationId, string userId, CancellationToken ct = default);
}