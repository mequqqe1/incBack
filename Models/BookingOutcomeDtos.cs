// Models/Dto/BookingOutcomeDtos.cs
using System.ComponentModel.DataAnnotations;
using SharpAuthDemo.Models;

public record CloseBookingRequest(
    [property:Required] string Summary,
    string? Recommendations,
    string? NextSteps,
    string? SpecialistPrivateNotes
);

public record BookingOutcomeResponse(
    Guid BookingId,
    string Summary,
    string? Recommendations,
    string? NextSteps,
    DateTime CreatedAtUtc,
    DateTime? ParentAcknowledgedAtUtc
);

// для удобства деталки брони со вложенным Outcome
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
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    BookingOutcomeResponse? Outcome // NEW
);