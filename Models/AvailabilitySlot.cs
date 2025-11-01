using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

public class AvailabilitySlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string SpecialistUserId { get; set; } = null!; // FK -> users

    [Required] public DateTime StartsAtUtc { get; set; }
    [Required] public DateTime EndsAtUtc { get; set; }

    // Быстрый флаг, чтобы не вычислять каждый раз
    public bool IsBooked { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; } // "онлайн", "офлайн", адрес и т.п.

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}