// Models/Dto/BookingOutcomeDtos.cs
using System.ComponentModel.DataAnnotations;
using SharpAuthDemo.Models;

public record CloseBookingRequest
{
    [Required]
    public string Summary { get; init; } = default!;
    public string? Recommendations { get; init; }
    public string? NextSteps { get; init; }
    public string? SpecialistPrivateNotes { get; init; }
}

public record BookingOutcomeResponse(
    Guid BookingId,
    string Summary,
    string? Recommendations,
    string? NextSteps,
    DateTime CreatedAtUtc,
    DateTime? ParentAcknowledgedAtUtc
);

public record BookingDetailsResponse(
    Guid Id,
    string SpecialistUserId,
    string ParentUserId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    BookingStatus Status,
    string? MessageFromParent,
    Guid? AvailabilitySlotId,
    Guid? ChildId,
    Guid? AssignedCaregiverMemberId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    BookingOutcomeResponse? Outcome
);