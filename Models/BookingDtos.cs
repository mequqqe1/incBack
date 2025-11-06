using System.ComponentModel.DataAnnotations;

namespace SharpAuthDemo.Models;

// Запрос на создание пачки слотов
public record AvailabilitySlotCreateItem(
    [param: Required] DateTime StartsAtUtc,
    [param: Required] DateTime EndsAtUtc,
    string? Note
);

public record CreateAvailabilityRequest(
    [param: Required] AvailabilitySlotCreateItem[] Slots
);

// Ответ для слота
public record AvailabilitySlotResponse(
    Guid Id,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsBooked,
    string? Note
);

public record CreateBookingRequest(
    Guid AvailabilitySlotId,
    Guid ChildId,                 // NEW: требуем ребёнка
    string? MessageFromParent
);


public record BookingResponse(
    Guid Id,
    string SpecialistUserId,
    string ParentUserId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    BookingStatus Status,
    string? MessageFromParent,
    Guid? AvailabilitySlotId,
    Guid? ChildId,               // NEW: отдаём тоже
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record WeeklyTemplateSlotDto(
    [param: Range(0, 6)] int DayOfWeek,   // 0—6 как System.DayOfWeek
    [param: Required] TimeOnly StartLocalTime,
    [param: Required] TimeOnly EndLocalTime,
    string? Note
);

public record UpsertWeeklyTemplateRequest(
    [param: Required] WeeklyTemplateSlotDto[] Slots,
    bool IsActive
);

public record WeeklyTemplateResponse(
    Guid Id,
    bool IsActive,
    WeeklyTemplateSlotDto[] Slots
);

public record MaterializeTemplateRequest(
    [param: Required] DateTime FromDateUtc, // дата-время, используем только дату (UTC)
    [param: Required] DateTime ToDateUtc,   // не включительно; максимум, скажем, +90 дней
    bool SkipPast = true
);

public record SchedulePresetInfo(string Code, string Name, int SlotMinutes);

public record BreakDto(
    [param: Required] TimeOnly From,
    [param: Required] TimeOnly To
);

public record GenerateFromPresetRequest(
    [param: Required] string PresetCode,
    int[]? DaysOfWeek,                 // если null — дефолт от пресета
    [param: Required] TimeOnly StartLocalTime,
    [param: Required] TimeOnly EndLocalTime,
    int SlotMinutes,                   // 30 по умолчанию
    BreakDto[]? Breaks,
    string? Note,
    bool IsActive = true
);