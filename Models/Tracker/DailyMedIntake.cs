using System.ComponentModel.DataAnnotations;

namespace INCBack.Models.Tracker;

public class DailyMedIntake
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid DailyEntryId { get; set; }
    public DailyEntry? DailyEntry { get; set; }

    [Required, MaxLength(200)] public string Drug { get; set; } = "";
    [MaxLength(100)] public string? Dose { get; set; }
    [Required] public DateTime TimeUtc { get; set; }
    public bool Taken { get; set; } = true;
    public string? SideEffectsJson { get; set; } // ["сонливость"]
}