using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

public class WeeklyTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string SpecialistUserId { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<WeeklyTemplateSlot> Slots { get; set; } = new();
}

public class WeeklyTemplateSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid WeeklyTemplateId { get; set; }
    public WeeklyTemplate WeeklyTemplate { get; set; } = null!;

    /// <summary>0 = Sunday ... 6 = Saturday (как в System.DayOfWeek)</summary>
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    /// <summary>Локальное время старта (без даты)</summary>
    [Required] public TimeOnly StartLocalTime { get; set; }

    /// <summary>Локальное время окончания (без даты)</summary>
    [Required] public TimeOnly EndLocalTime { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }
}