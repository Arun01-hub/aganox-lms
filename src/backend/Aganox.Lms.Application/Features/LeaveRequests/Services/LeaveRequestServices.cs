using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveRequests.Services;

internal static class LeaveDayCalculationService
{
    public static async Task<(Result<LeaveDayPreviewDto> Result, LeaveType? LeaveType)> CalculateAsync(
        IAppDbContext db,
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly fromDate,
        DateOnly toDate,
        bool isHalfDay,
        CancellationToken cancellationToken)
    {
        var leaveType = await db.LeaveTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == leaveTypeId, cancellationToken);
        if (leaveType is null)
        {
            return (Result<LeaveDayPreviewDto>.Failure("Leave type not found."), null);
        }

        if (isHalfDay && !leaveType.AllowHalfDay)
        {
            return (Result<LeaveDayPreviewDto>.Failure("Half-day is not allowed for this leave type."), leaveType);
        }

        var year = fromDate.Year;
        var policy = await db.LeavePolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LeaveTypeId == leaveTypeId && x.Year == year, cancellationToken);

        var skipWeekends = policy?.SkipWeekends ?? true;
        var skipHolidays = policy?.SkipHolidays ?? true;

        var calendarId = await db.Employees.AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => x.LeaveCalendarId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!calendarId.HasValue)
        {
            calendarId = await db.LeaveCalendars.AsNoTracking()
                .Where(x => x.IsDefault)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var holidayDates = new HashSet<DateOnly>();
        if (calendarId.HasValue && skipHolidays)
        {
            var dates = await db.Holidays.AsNoTracking()
                .Where(x => x.LeaveCalendarId == calendarId.Value &&
                            x.HolidayDate >= fromDate &&
                            x.HolidayDate <= toDate)
                .Select(x => x.HolidayDate)
                .ToListAsync(cancellationToken);
            holidayDates = dates.ToHashSet();
        }

        try
        {
            var calc = LeaveDayCalculator.Calculate(fromDate, toDate, isHalfDay, skipWeekends, skipHolidays, holidayDates);
            return (Result<LeaveDayPreviewDto>.Success(new LeaveDayPreviewDto(
                calc.NumberOfDays,
                calc.SkippedWeekends,
                calc.SkippedHolidays,
                calc.Breakdown.Select(b => new LeaveDayBreakdownDto(b.Date, b.Counted, b.Reason)).ToList())), leaveType);
        }
        catch (ArgumentException ex)
        {
            return (Result<LeaveDayPreviewDto>.Failure(ex.Message), leaveType);
        }
    }
}

internal static class LeaveRequestAccessService
{
    public static async Task<Guid?> GetEmployeeIdForUserAsync(
        IAppDbContext db, Guid userId, CancellationToken cancellationToken) =>
        await db.Employees.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public static bool CanViewAll(ICurrentUserService user) =>
        user.Permissions.Contains(Domain.Constants.Permissions.LeaveViewAll);

    public static bool CanViewTeam(ICurrentUserService user) =>
        user.Permissions.Contains(Domain.Constants.Permissions.LeaveViewTeam);

    public static async Task<IQueryable<LeaveRequest>> ApplyViewScopeAsync(
        IQueryable<LeaveRequest> query,
        IAppDbContext db,
        ICurrentUserService currentUser,
        Guid? filterEmployeeId,
        CancellationToken cancellationToken)
    {
        if (CanViewAll(currentUser))
        {
            return filterEmployeeId.HasValue
                ? query.Where(x => x.EmployeeId == filterEmployeeId.Value)
                : query;
        }

        var ownEmployeeId = currentUser.UserId.HasValue
            ? await GetEmployeeIdForUserAsync(db, currentUser.UserId.Value, cancellationToken)
            : null;

        if (CanViewTeam(currentUser) && ownEmployeeId.HasValue)
        {
            var teamIds = await db.Employees.AsNoTracking()
                .Where(x => x.ManagerId == ownEmployeeId.Value)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            teamIds.Add(ownEmployeeId.Value);

            query = query.Where(x => teamIds.Contains(x.EmployeeId));
            return filterEmployeeId.HasValue
                ? query.Where(x => x.EmployeeId == filterEmployeeId.Value)
                : query;
        }

        if (ownEmployeeId.HasValue)
        {
            return query.Where(x => x.EmployeeId == ownEmployeeId.Value);
        }

        return query.Where(_ => false);
    }
}
