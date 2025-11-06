// Models/BookingOutcome.cs
using System.ComponentModel.DataAnnotations;

public class BookingOutcome
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    [Required] public string SpecialistUserId { get; set; } = null!;
    [Required] public string ParentUserId { get; set; } = null!;

    // Публичная часть (видна родителю)
    [MaxLength(4000)] public string? Summary { get; set; }             // итоги встречи
    [MaxLength(4000)] public string? Recommendations { get; set; }      // рекомендации
    [MaxLength(1000)] public string? NextSteps { get; set; }            // «что дальше/домашка»

    // Необязательная приватная часть (только для специалиста/админов)
    [MaxLength(4000)] public string? SpecialistPrivateNotes { get; set; }

    // Признак/время, что родитель прочитал
    public DateTime? ParentAcknowledgedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}