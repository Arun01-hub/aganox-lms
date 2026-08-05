namespace Aganox.Lms.Application.Features.LeaveRequests.Dtos;

using Aganox.Lms.Application.Features.Approvals.Dtos;

public sealed record LeaveRequestSummaryDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeCode,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay,
    string? HalfDayPeriod,
    decimal NumberOfDays,
    string Status,
    string? Remarks,
    DateTime CreatedAt);

public sealed record LeaveRequestDetailDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeCode,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay,
    string? HalfDayPeriod,
    decimal NumberOfDays,
    string Status,
    string? Remarks,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    IReadOnlyList<ApprovalStepDto> ApprovalSteps,
    IReadOnlyList<LeaveRequestCommentDto> Comments);

public sealed record CreateLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay,
    string? HalfDayPeriod,
    string? Remarks,
    bool Submit);

public sealed record UpdateLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay,
    string? HalfDayPeriod,
    string? Remarks,
    bool Submit);

public sealed record CreateBalanceAdjustmentRequest(
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    decimal DeltaDays,
    string Reason);
