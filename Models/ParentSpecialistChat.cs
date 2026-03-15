using System.ComponentModel.DataAnnotations;

namespace INCBack.Models;

/// <summary>Один чат между родителем и специалистом (одна нить на пару).</summary>
public class ParentSpecialistConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string ParentUserId { get; set; } = null!;
    [Required] public string SpecialistUserId { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ParentSpecialistMessage> Messages { get; set; } = new List<ParentSpecialistMessage>();
}

public class ParentSpecialistMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid ConversationId { get; set; }
    public ParentSpecialistConversation? Conversation { get; set; }

    [Required] public string SenderUserId { get; set; } = null!;
    [Required, MaxLength(4000)] public string Text { get; set; } = "";

    public Guid? BookingId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
