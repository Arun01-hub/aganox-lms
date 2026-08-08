using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Approvals;
using Aganox.Lms.Application.Features.Dashboards.Dtos;
using Aganox.Lms.Application.Features.LeaveRequests.Services;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Dashboards;

public sealed record GetEmployeeDashboardQuery(int Year) : IRequest<EmployeeDashboardDto?>;
public sealed record GetManagerDashboardQuery(int Year, int Month) : IRequest<ManagerDashboardDto?>;
public sealed record GetAdminDashboardQuery(int Year) : IRequest<AdminDashboardDto?>;

public sealed class GetEmployeeDashboardQueryHandler : IRequestHandler<GetEmployeeDashboardQuery, EmployeeDashboardDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetEmployeeDashboardQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<EmployeeDashboardDto?> Handle(GetEmployeeDashboardQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return null;
        }

        var employeeId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(
            _db, _currentUser.UserId.Value, cancellationToken);
        if (!employeeId.HasValue)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);

        var dashboardLeaveTypes = await _db.LeaveTypes.AsNoTracking()
            .Where(x => x.IsActive && (x.Code == "CL" || x.Code == "EL"))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var existingBalances = await _db.LeaveBalances.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employeeId.Value && x.Year == request.Year)
            .ToListAsync(cancellationToken);

        var leaveTypeIds = dashboardLeaveTypes.Select(x => x.Id).ToList();
        var policies = leaveTypeIds.Count == 0
            ? []
            : await _db.LeavePolicies.AsNoTracking()
                .Where(x => x.Year == request.Year && leaveTypeIds.Contains(x.LeaveTypeId))
                .ToListAsync(cancellationToken);

        var balances = dashboardLeaveTypes
            .Select(leaveType =>
            {
                var balance = existingBalances.FirstOrDefault(x => x.LeaveTypeId == leaveType.Id);
                if (balance is not null)
                {
                    return new LeaveBalanceSummaryDto(leaveType.Code, leaveType.Name, balance.Remaining);
                }

                var policy = policies.FirstOrDefault(x => x.LeaveTypeId == leaveType.Id);
                var remaining = policy?.DefaultOpeningBalance ?? 0;
                return new LeaveBalanceSummaryDto(leaveType.Code, leaveType.Name, remaining);
            })
            .ToList();

        var requests = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employeeId.Value)
            .OrderByDescending(x => x.FromDate)
            .ToListAsync(cancellationToken);

        var upcoming = requests
            .Where(x => x.Status == LeaveRequestStatus.Approved && x.ToDate >= today)
            .OrderBy(x => x.FromDate)
            .Take(10)
            .Select(MapBrief)
            .ToList();

        var pending = requests
            .Where(x => x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Draft)
            .OrderBy(x => x.FromDate)
            .Select(MapBrief)
            .ToList();

        return new EmployeeDashboardDto(balances, upcoming, pending);
    }

    private static LeaveRequestBriefDto MapBrief(Domain.Entities.Leave.LeaveRequest x) => new(
        x.Id, x.LeaveType!.Name, x.FromDate, x.ToDate, x.NumberOfDays, x.Status.ToString());
}

public sealed class GetManagerDashboardQueryHandler : IRequestHandler<GetManagerDashboardQuery, ManagerDashboardDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetManagerDashboardQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ManagerDashboardDto?> Handle(GetManagerDashboardQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null ||
            (!_currentUser.Permissions.Contains(Permissions.LeaveViewTeam) &&
             !_currentUser.Roles.Contains(Roles.Manager)))
        {
            return null;
        }

        var managerEmployeeId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(
            _db, _currentUser.UserId.Value, cancellationToken);
        if (!managerEmployeeId.HasValue)
        {
            return null;
        }

        var teamIds = await _db.Employees.AsNoTracking()
            .Where(x => x.ManagerId == managerEmployeeId.Value && x.Status == EmployeeStatus.Active)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var pendingPage = await new GetPendingApprovalsQueryHandler(_db, _currentUser)
            .Handle(new GetPendingApprovalsQuery(1, 1000), cancellationToken);

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var approvedTeamLeaves = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .Where(x =>
                teamIds.Contains(x.EmployeeId) &&
                x.Status == LeaveRequestStatus.Approved &&
                x.FromDate <= monthEnd &&
                x.ToDate >= monthStart)
            .ToListAsync(cancellationToken);

        var onLeaveToday = approvedTeamLeaves
            .Where(x => x.FromDate <= today && x.ToDate >= today)
            .Select(x => new TeamLeaveTodayDto(
                x.EmployeeId,
                x.Employee!.FullName,
                x.LeaveType!.Name,
                x.FromDate,
                x.ToDate))
            .ToList();

        var calendar = new List<TeamCalendarEntryDto>();
        foreach (var leave in approvedTeamLeaves)
        {
            for (var d = leave.FromDate; d <= leave.ToDate; d = d.AddDays(1))
            {
                if (d >= monthStart && d <= monthEnd)
                {
                    calendar.Add(new TeamCalendarEntryDto(
                        d, leave.EmployeeId, leave.Employee!.FullName, leave.LeaveType!.Code));
                }
            }
        }

        var pendingTeam = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => teamIds.Contains(x.EmployeeId) && x.Status == LeaveRequestStatus.Pending)
            .OrderBy(x => x.FromDate)
            .Select(x => new LeaveRequestBriefDto(
                x.Id, x.LeaveType!.Name, x.FromDate, x.ToDate, x.NumberOfDays, x.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new ManagerDashboardDto(
            pendingPage.TotalCount,
            onLeaveToday,
            calendar.OrderBy(x => x.Date).ThenBy(x => x.EmployeeName).ToList(),
            pendingTeam);
    }
}

public sealed class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetAdminDashboardQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AdminDashboardDto?> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.Roles.Contains(Roles.Administrator) &&
            !_currentUser.Roles.Contains(Roles.Hr) &&
            !_currentUser.Permissions.Contains(Permissions.LeaveViewAll))
        {
            return null;
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var totalEmployees = await _db.Employees.AsNoTracking().CountAsync(cancellationToken);
        var activeEmployees = await _db.Employees.AsNoTracking()
            .CountAsync(x => x.Status == EmployeeStatus.Active, cancellationToken);

        var onLeaveToday = await _db.LeaveRequests.AsNoTracking()
            .CountAsync(x =>
                x.Status == LeaveRequestStatus.Approved &&
                x.FromDate <= today &&
                x.ToDate >= today, cancellationToken);

        var pendingApprovals = await _db.LeaveRequests.AsNoTracking()
            .CountAsync(x => x.Status == LeaveRequestStatus.Pending, cancellationToken);

        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        var approvedInYear = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)!.ThenInclude(x => x!.Department)
            .Where(x =>
                x.Status == LeaveRequestStatus.Approved &&
                x.FromDate >= yearStart &&
                x.FromDate <= yearEnd)
            .ToListAsync(cancellationToken);

        var byDepartment = approvedInYear
            .GroupBy(x => x.Employee!.Department!.Name)
            .Select(g => new DepartmentStatDto(g.Key, g.Sum(x => x.NumberOfDays), g.Count()))
            .OrderByDescending(x => x.TotalDays)
            .ToList();

        var monthlyTrend = approvedInYear
            .GroupBy(x => x.FromDate.ToString("yyyy-MM"))
            .Select(g => new MonthlyTrendDto(g.Key, g.Sum(x => x.NumberOfDays), g.Count()))
            .OrderBy(x => x.Month)
            .ToList();

        return new AdminDashboardDto(
            totalEmployees,
            activeEmployees,
            onLeaveToday,
            pendingApprovals,
            byDepartment,
            monthlyTrend);
    }
}
