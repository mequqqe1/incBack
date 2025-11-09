// Models/ZeynAI/AIConversation.cs
using System.ComponentModel.DataAnnotations;

namespace INCBack.Models.ZeynAI;

public class AIConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string ParentUserId { get; set; } = null!;
    [Required] public Guid ChildId { get; set; }

    [MaxLength(200)] public string? Title { get; set; }           // можно автозаполнять первой фразой
    public bool Archived { get; set; } = false;

    // для компрессии длинных историй
    public string? Summary { get; set; }
    public int TurnCount { get; set; } = 0;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Child? Child { get; set; }
    public ICollection<AIMessage> Messages { get; set; } = new List<AIMessage>();
}