// Contracts/ParentProfileDtos.cs
namespace SharpAuthDemo.Contracts;

public record ParentProfileResponse(
    Guid Id,
    string UserId,
    string? FirstName,
    string? LastName,
    string? CountryCode,
    string? City,
    string? AddressLine1,
    string? AddressLine2,
    string? Phone
);

public record UpsertParentProfileRequest(
    string? FirstName,
    string? LastName,
    string? CountryCode,
    string? City,
    string? AddressLine1,
    string? AddressLine2,
    string? Phone
);