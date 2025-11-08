using System.ComponentModel.DataAnnotations;

namespace INCBack.Models.Tracker;

public class DailyIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid DailyEntryId { get; set; }
    public DailyEntry? DailyEntry { get; set; }

    [Required] public DateTime TimeUtc { get; set; }
    [Range(1,5)] public int Intensity { get; set; } = 1;
    [Range(0,3600)] public int? DurationSec { get; set; }
    public bool Injury { get; set; }

    // Теги (легкий ABC): JSON-массивы строк
    public string? AntecedentJson { get; set; }   // ["шум","ожидание"]
    public string? BehaviorJson { get; set; }     // ["крик","бегство"]
    public string? ConsequenceJson { get; set; }  // ["объятие","переключение"]

    [MaxLength(600)] public string? Notes { get; set; }
}