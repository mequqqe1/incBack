// Models/ZeynAI/AIMessage.cs
using System.ComponentModel.DataAnnotations;

namespace INCBack.Models.ZeynAI;

public enum AIMessageRole { System=0, User=1, Assistant=2, Tool=3 }

public class AIMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid ConversationId { get; set; }
    public AIConversation? Conversation { get; set; }

    [Required] public AIMessageRole Role { get; set; }
    [Required] public string Content { get; set; } = "";          // текст (делим на чанки при стриме и склеиваем)

    // опционально для аналитики/диагностики
    public string? Model { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public string? FinishReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}