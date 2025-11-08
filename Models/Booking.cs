// Models/Booking.cs

using System.ComponentModel.DataAnnotations;
using INCBack.Models;
using SharpAuthDemo.Models;
using INCBack.Models;

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

    public Guid? AvailabilitySlotId { get; set; }
    public AvailabilitySlot? AvailabilitySlot { get; set; }

    // NEW: ссылка на ребёнка
    public Guid? ChildId { get; set; }
    
    public Child? Child { get; set; }
    public BookingOutcome? Outcome { get; set; } // NEW: навигация 1:1

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public SpecialistReview? Review { get; set; } // ← навигация на отзыв (1:1)
    
}