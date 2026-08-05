using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.LeaveRequests.Services;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Reports.Services;

internal static class ReportAccessService
{
    public static async Task<IQueryable<Domain.Entities.Leave.LeaveRequest>> ScopeLeaveRequestsAsync(
        IAppDbContext db,
        ICurrentUserService user,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var query = db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)!.ThenInclude(x => x!.Department)
            .Include(x => x.LeaveType)
            .AsQueryable();

        if (LeaveRequestAccessService.CanViewAll(user))
        {
            return employeeId.HasValue
                ? query.Where(x => x.EmployeeId == employeeId.Value)
                : query;
        }

        var ownId = user.UserId.HasValue
            ? await LeaveRequestAccessService.GetEmployeeIdForUserAsync(db, user.UserId.Value, cancellationToken)
            : null;

        if (LeaveRequestAccessService.CanViewTeam(user) && ownId.HasValue)
        {
            var teamIds = await db.Employees.AsNoTracking()
                .Where(x => x.ManagerId == ownId.Value)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            teamIds.Add(ownId.Value);

            query = query.Where(x => teamIds.Contains(x.EmployeeId));
            return employeeId.HasValue
                ? query.Where(x => x.EmployeeId == employeeId.Value)
                : query;
        }

        if (ownId.HasValue)
        {
            return query.Where(x => x.EmployeeId == ownId.Value);
        }

        return query.Where(_ => false);
    }

    public static async Task<IQueryable<Domain.Entities.Organization.Employee>> ScopeEmployeesAsync(
        IAppDbContext db,
        ICurrentUserService user,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking()
            .Include(x => x.Department)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == departmentId.Value);
        }

        if (LeaveRequestAccessService.CanViewAll(user))
        {
            return query;
        }

        var ownId = user.UserId.HasValue
            ? await LeaveRequestAccessService.GetEmployeeIdForUserAsync(db, user.UserId.Value, cancellationToken)
            : null;

        if (LeaveRequestAccessService.CanViewTeam(user) && ownId.HasValue)
        {
            return query.Where(x => x.Id == ownId.Value || x.ManagerId == ownId.Value);
        }

        if (ownId.HasValue)
        {
            return query.Where(x => x.Id == ownId.Value);
        }

        return query.Where(_ => false);
    }

    public static bool CanRunReports(ICurrentUserService user) =>
        user.Permissions.Contains(Permissions.ReportsView) ||
        user.Roles.Contains(Roles.Administrator) ||
        user.Roles.Contains(Roles.Hr);

    public static bool CanExport(ICurrentUserService user) =>
        user.Permissions.Contains(Permissions.ReportsExport) ||
        user.Roles.Contains(Roles.Administrator) ||
        user.Roles.Contains(Roles.Hr);
}
