using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

public record SpecialistProfileUpsertRequest(
    [param: MaxLength(2000)] string? About,
    [param: Required, MaxLength(2)] string CountryCode,
    [param: Required, MaxLength(200)] string City,
    [param: Required, MaxLength(300)] string AddressLine1,
    [param: MaxLength(300)] string? AddressLine2,
    [param: MaxLength(120)] string? Region,
    [param: MaxLength(20)] string? PostalCode,
    int? ExperienceYears,
    decimal? PricePerHour,
    [param: MaxLength(200)] string? Telegram,
    [param: MaxLength(50)] string? Phone,
    bool IsEmailPublic
);

public record SpecialistProfileResponse(
    Guid Id,
    string UserId,
    string? About,
    string CountryCode,
    string City,
    string AddressLine1,
    string? AddressLine2,
    string? Region,
    string? PostalCode,
    int? ExperienceYears,
    decimal? PricePerHour,
    string? Telegram,
    string? Phone,
    bool IsEmailPublic,
    string? AvatarMimeType,
    ModerationStatus Status,
    string? ModerationComment,
    DateTime? ModeratedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int[] SpecializationIds,
    int[] SkillIds
);

// Если эти рекорды используешь — тоже ставим [param:]
public record AvatarUpdateRequest(
    [param: Required, MaxLength(200)] string MimeType,
    [param: Required] string Base64
);

public record DiplomaUploadRequest(
    [param: Required, MaxLength(200)] string? Title,
    [param: Required, MaxLength(260)] string? FileName,
    [param: Required, MaxLength(100)] string MimeType,
    [param: Required] string Base64
);

public record DiplomaResponse(
    Guid Id,
    string? Title,
    string? FileName,
    string? MimeType,
    DateTime UploadedAtUtc
);

public record ModerationUpdateRequest(
    [param: Required] ModerationStatus Status,
    [param: MaxLength(500)] string? Comment
);

// И здесь
public record SetSpecializationsRequest([param: Required] int[] SpecializationIds);
public record SetSkillsRequest([param: Required] int[] SkillIds);

public record LookupItem(int Id, string Name, string? Description, int SortOrder, bool IsActive);

public record ReviewCreateDto(
    Guid? BookingId,
    int Rating,
    string? Comment,
    bool IsAnonymous = false
);

public record ReviewVm(
    Guid Id,
    int Rating,
    string? Comment,
    string AuthorName,     // «Аноним» если IsAnonymous
    DateTime CreatedAtUtc
);

public record ReviewsSummaryVm(double average, int count, IReadOnlyList<ReviewVm> items);

