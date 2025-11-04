// Contracts/ChildDtos.cs
using INCBack.Models;

namespace SharpAuthDemo.Contracts;

public record ChildResponse(
    Guid Id,
    string FirstName,
    string? LastName,
    DateTime? BirthDate,
    Sex Sex,
    SupportLevel SupportLevel,
    string? PrimaryDiagnosis,
    bool NonVerbal,
    string? CommunicationMethod,
    string? Allergies,
    string? Medications,
    string? Triggers,
    string? CalmingStrategies,
    string? SchoolOrCenter,
    string? CurrentGoals
);

public record UpsertChildRequest(
    string FirstName,
    string? LastName,
    DateTime? BirthDate,
    Sex Sex,
    SupportLevel SupportLevel,
    string? PrimaryDiagnosis,
    bool NonVerbal,
    string? CommunicationMethod,
    string? Allergies,
    string? Medications,
    string? Triggers,
    string? CalmingStrategies,
    string? SchoolOrCenter,
    string? CurrentGoals
);

public record ChildNoteRequest(string Text);
public record ChildNoteResponse(Guid Id, string Text, DateTime CreatedAtUtc);