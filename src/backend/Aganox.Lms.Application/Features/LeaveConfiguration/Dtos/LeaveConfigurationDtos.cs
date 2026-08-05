namespace Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;

public sealed record LeaveTypeDto(
    Guid Id,
    string Code,
    string Name,
    bool IsPaid,
    bool RequiresApproval,
    bool AllowHalfDay,
    bool AllowNegativeBalance,
    int? MaxDaysPerRequest,
    string? ColorHex,
    int SortOrder,
    bool IsActive);

public sealed record LeavePolicyDto(
    Guid Id,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    bool SkipWeekends,
    bool SkipHolidays,
    decimal CarryForwardMaxDays,
    bool YearlyResetEnabled,
    decimal DefaultOpeningBalance,
    DateOnly? AccrualStartDate);

public sealed record LeaveCalendarDto(
    Guid Id,
    string Name,
    bool IsDefault,
    int HolidayCount);

public sealed record HolidayDto(
    Guid Id,
    Guid LeaveCalendarId,
    string CalendarName,
    DateOnly HolidayDate,
    string Name,
    bool IsOptional);

public sealed record LeaveBalanceDto(
    Guid Id,
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    int Year,
    decimal OpeningBalance,
    decimal Credited,
    decimal Consumed,
    decimal Adjusted,
    decimal Remaining);

public sealed record LeaveDayPreviewDto(
    decimal NumberOfDays,
    int SkippedWeekends,
    int SkippedHolidays,
    IReadOnlyList<LeaveDayBreakdownDto> Breakdown);

public sealed record LeaveDayBreakdownDto(DateOnly Date, bool Counted, string? Reason);

public sealed record CreateLeaveTypeRequest(
    string Code,
    string Name,
    bool IsPaid,
    bool RequiresApproval,
    bool AllowHalfDay,
    bool AllowNegativeBalance,
    int? MaxDaysPerRequest,
    string? ColorHex,
    int SortOrder,
    bool IsActive);

public sealed record UpdateLeaveTypeRequest(
    string Code,
    string Name,
    bool IsPaid,
    bool RequiresApproval,
    bool AllowHalfDay,
    bool AllowNegativeBalance,
    int? MaxDaysPerRequest,
    string? ColorHex,
    int SortOrder,
    bool IsActive);

public sealed record UpsertLeavePolicyRequest(
    Guid LeaveTypeId,
    int Year,
    bool SkipWeekends,
    bool SkipHolidays,
    decimal CarryForwardMaxDays,
    bool YearlyResetEnabled,
    decimal DefaultOpeningBalance,
    DateOnly? AccrualStartDate);

public sealed record CreateLeaveCalendarRequest(string Name, bool IsDefault);
public sealed record UpdateLeaveCalendarRequest(string Name, bool IsDefault);

public sealed record CreateHolidayRequest(
    Guid LeaveCalendarId,
    DateOnly HolidayDate,
    string Name,
    bool IsOptional);

public sealed record UpdateHolidayRequest(
    DateOnly HolidayDate,
    string Name,
    bool IsOptional);

public sealed record SetOpeningBalancesRequest(
    int Year,
    IReadOnlyList<OpeningBalanceItemRequest> Balances);

public sealed record OpeningBalanceItemRequest(Guid LeaveTypeId, decimal OpeningBalance);

public sealed record PreviewLeaveDaysRequest(
    Guid? EmployeeId,
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay);
