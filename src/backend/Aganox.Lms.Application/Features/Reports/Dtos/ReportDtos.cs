namespace Aganox.Lms.Application.Features.Reports.Dtos;

public sealed record EmployeeLeaveReportRowDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal NumberOfDays,
    string Status);

public sealed record MonthlyLeaveReportRowDto(
    string DepartmentName,
    string LeaveTypeName,
    int RequestCount,
    decimal TotalDays);

public sealed record YearlyLeaveReportRowDto(
    string DepartmentName,
    string LeaveTypeName,
    int RequestCount,
    decimal TotalDays);

public sealed record DepartmentLeaveReportRowDto(
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal NumberOfDays,
    string Status);

public sealed record BalanceReportRowDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string LeaveTypeCode,
    decimal OpeningBalance,
    decimal Consumed,
    decimal Remaining);

public sealed record AttendanceSummaryRowDto(
    DateOnly Date,
    int EmployeesOnLeave,
    decimal TotalLeaveDays);

public sealed record TrendReportRowDto(
    string Period,
    int RequestCount,
    decimal TotalDays);

public sealed record ReportResultDto<T>(IReadOnlyList<T> Rows, string Title);
