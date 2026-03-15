using System.ComponentModel.DataAnnotations;

namespace INCBack.Models.Tracker;

public class DailySession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DailyEntryId { get; set; }
    public DailyEntry? DailyEntry { get; set; }

    [Required, MaxLength(80)] public string Type { get; set; } = "";
    [Range(1,600)] public int DurationMin { get; set; }
    [Range(1,5)]  public int? Quality { get; set; }
    public string? GoalTagsJson { get; set; }
    [MaxLength(600)] public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Кто внёс сессию (UserId опекуна или родителя) — для семейного календаря.</summary>
    public string? RecordedByUserId { get; set; }
}