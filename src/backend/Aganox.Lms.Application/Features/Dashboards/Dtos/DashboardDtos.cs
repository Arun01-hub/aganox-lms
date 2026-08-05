namespace Aganox.Lms.Application.Features.Dashboards.Dtos;

public sealed record LeaveBalanceSummaryDto(
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal Remaining);

public sealed record LeaveRequestBriefDto(
    Guid Id,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal NumberOfDays,
    string Status);

public sealed record EmployeeDashboardDto(
    IReadOnlyList<LeaveBalanceSummaryDto> Balances,
    IReadOnlyList<LeaveRequestBriefDto> UpcomingLeaves,
    IReadOnlyList<LeaveRequestBriefDto> PendingRequests);

public sealed record TeamLeaveTodayDto(
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeName,
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record TeamCalendarEntryDto(
    DateOnly Date,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveTypeCode);

public sealed record ManagerDashboardDto(
    int PendingApprovalsCount,
    IReadOnlyList<TeamLeaveTodayDto> TeamOnLeaveToday,
    IReadOnlyList<TeamCalendarEntryDto> TeamCalendar,
    IReadOnlyList<LeaveRequestBriefDto> PendingTeamRequests);

public sealed record DepartmentStatDto(string DepartmentName, decimal TotalDays, int RequestCount);

public sealed record MonthlyTrendDto(string Month, decimal TotalDays, int RequestCount);

public sealed record AdminDashboardDto(
    int TotalEmployees,
    int ActiveEmployees,
    int OnLeaveToday,
    int PendingApprovals,
    IReadOnlyList<DepartmentStatDto> LeavesByDepartment,
    IReadOnlyList<MonthlyTrendDto> MonthlyTrend);
