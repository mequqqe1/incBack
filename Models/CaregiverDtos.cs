// Contracts/CaregiverDtos.cs
using INCBack.Models;

namespace SharpAuthDemo.Contracts;

public record CaregiverResponse(
    Guid Id, string Email, string? UserId, string? Relation,
    bool IsAdmin, CaregiverStatus Status, DateTime InvitedAtUtc, DateTime? AcceptedAtUtc
);

public record InviteCaregiverRequest(string Email, string? Relation, bool IsAdmin);
public record UpdateCaregiverRequest(string? Relation, bool? IsAdmin, CaregiverStatus? Status);