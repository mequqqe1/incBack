using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string SpecialistUserId { get; set; } = null!;
    [Required] public string ParentUserId { get; set; } = null!;

    [Required] public DateTime StartsAtUtc { get; set; }
    [Required] public DateTime EndsAtUtc { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [MaxLength(1000)]
    public string? MessageFromParent { get; set; }

    public Guid? AvailabilitySlotId { get; set; } // если бронь создана на базовый слот
    public AvailabilitySlot? AvailabilitySlot { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}