namespace Aganox.Lms.Application.Features.Approvals.Dtos;

public sealed record ApprovalStepDto(
    Guid Id,
    int StepOrder,
    string ApproverRole,
    Guid? ApproverEmployeeId,
    string? ApproverName,
    string Status,
    string? Comments,
    DateTime? ActionedAt);

public sealed record LeaveRequestCommentDto(
    Guid Id,
    string AuthorName,
    string Comment,
    DateTime CreatedAt);

public sealed record PendingApprovalDto(
    Guid LeaveRequestId,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal NumberOfDays,
    string Status,
    int CurrentStepOrder,
    string CurrentApproverRole,
    DateTime SubmittedAt);

public sealed record ApprovalHistoryDto(
    Guid LeaveRequestId,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal NumberOfDays,
    string RequestStatus,
    string StepStatus,
    string ApproverRole,
    string? ApproverName,
    string? Comments,
    DateTime? ActionedAt);

public sealed record ApprovalActionRequest(string? Comments);
