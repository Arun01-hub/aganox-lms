using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Domain.Entities.Attendance;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;
using Aganox.Lms.Domain.Services.Attendance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Attendance;

public sealed record AttendanceDayDto(
    Guid? Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    DateOnly Date,
    string Status,
    string Source,
    string? Remarks,
    string? LeaveTypeCode);

public sealed record AttendanceMonthSummaryDto(
    int Year,
    int Month,
    Guid EmployeeId,
    string EmployeeName,
    decimal WorkingDays,
    decimal PresentDays,
    decimal AbsentDays,
    decimal LeaveDays,
    decimal LopDays,
    decimal PaidDays);

public sealed record GetAttendanceMonthQuery(int Year, int Month, Guid? DepartmentId, Guid? EmployeeId)
    : IRequest<IReadOnlyList<AttendanceDayDto>>;

public sealed record GetAttendanceSummariesQuery(int Year, int Month, Guid? DepartmentId)
    : IRequest<IReadOnlyList<AttendanceMonthSummaryDto>>;

public sealed record UpsertAttendanceDayCommand(
    Guid EmployeeId,
    DateOnly Date,
    AttendanceDayStatus Status,
    string? Remarks) : IRequest<Result<AttendanceDayDto>>;

public sealed record SyncAttendanceFromLeaveCommand(int Year, int Month, Guid? DepartmentId)
    : IRequest<Result<int>>;

public sealed record ApprovedLeaveDayInfo(AttendanceDayStatus Status, string LeaveTypeCode);

public sealed class UpsertAttendanceDayCommandValidator : AbstractValidator<UpsertAttendanceDayCommand>
{
    public UpsertAttendanceDayCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public sealed class AttendanceService
{
    private readonly IAppDbContext _db;

    public AttendanceService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlySet<DateOnly>> GetHolidayDatesAsync(
        Guid? calendarId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        if (!calendarId.HasValue)
        {
            return new HashSet<DateOnly>();
        }

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var holidays = await _db.Holidays.AsNoTracking()
            .Where(x => x.LeaveCalendarId == calendarId && x.HolidayDate >= start && x.HolidayDate <= end)
            .Select(x => x.HolidayDate)
            .ToListAsync(cancellationToken);
        return holidays.ToHashSet();
    }

    public async Task<Dictionary<DateOnly, AttendanceDayStatus>> BuildDayStatusMapAsync(
        Guid employeeId,
        int year,
        int month,
        IReadOnlySet<DateOnly> holidays,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<DateOnly, AttendanceDayStatus>();
        foreach (var date in AttendanceCalendarService.EnumerateMonthDates(year, month))
        {
            map[date] = AttendanceCalendarService.ResolveDefaultStatus(date, holidays, skipWeekends: true, skipHolidays: true);
        }

        var records = await _db.AttendanceDays.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.Date.Year == year && x.Date.Month == month)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            map[record.Date] = record.Status;
        }

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var leaveRequests = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employeeId &&
                        x.Status == LeaveRequestStatus.Approved &&
                        x.FromDate <= end &&
                        x.ToDate >= start &&
                        x.LeaveType != null &&
                        (x.LeaveType.Code == "CL" || x.LeaveType.Code == "EL"))
            .ToListAsync(cancellationToken);

        if (leaveRequests.Count > 0)
        {
            var policies = await GetLeavePoliciesAsync(leaveRequests, year, cancellationToken);
            var leaveOverlay = AttendanceService.BuildApprovedClElLeaveMap(employeeId, year, month, holidays, leaveRequests, policies);
            ApplyLeaveOverlay(map, records, leaveOverlay);
        }

        return map;
    }

    public async Task<Dictionary<Guid, Dictionary<DateOnly, ApprovedLeaveDayInfo>>> GetApprovedClElLeaveMapsAsync(
        IReadOnlyList<Guid> employeeIds,
        int year,
        int month,
        Func<Guid, Task<IReadOnlySet<DateOnly>>> holidaysResolver,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, Dictionary<DateOnly, ApprovedLeaveDayInfo>>();
        if (employeeIds.Count == 0)
        {
            return result;
        }

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var leaveRequests = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => employeeIds.Contains(x.EmployeeId) &&
                        x.Status == LeaveRequestStatus.Approved &&
                        x.FromDate <= end &&
                        x.ToDate >= start &&
                        x.LeaveType != null &&
                        (x.LeaveType.Code == "CL" || x.LeaveType.Code == "EL"))
            .ToListAsync(cancellationToken);

        if (leaveRequests.Count == 0)
        {
            return result;
        }

        var policies = await GetLeavePoliciesAsync(leaveRequests, year, cancellationToken);
        foreach (var employeeId in employeeIds)
        {
            var holidays = await holidaysResolver(employeeId);
            var map = AttendanceService.BuildApprovedClElLeaveMap(employeeId, year, month, holidays, leaveRequests, policies);
            if (map.Count > 0)
            {
                result[employeeId] = map;
            }
        }

        return result;
    }

    public static Dictionary<DateOnly, ApprovedLeaveDayInfo> BuildApprovedClElLeaveMap(
        Guid employeeId,
        int year,
        int month,
        IReadOnlySet<DateOnly> holidays,
        IEnumerable<LeaveRequest> leaveRequests,
        IReadOnlyDictionary<Guid, LeavePolicy> policiesByLeaveType)
    {
        var result = new Dictionary<DateOnly, ApprovedLeaveDayInfo>();

        foreach (var request in leaveRequests.Where(x => x.EmployeeId == employeeId))
        {
            policiesByLeaveType.TryGetValue(request.LeaveTypeId, out var policy);
            var skipWeekends = policy?.SkipWeekends ?? true;
            var skipHolidays = policy?.SkipHolidays ?? true;

            var calc = LeaveDayCalculator.Calculate(
                request.FromDate,
                request.ToDate,
                request.IsHalfDay,
                skipWeekends,
                skipHolidays,
                holidays);

            foreach (var day in calc.Breakdown.Where(x => x.Counted))
            {
                if (day.Date.Year != year || day.Date.Month != month)
                {
                    continue;
                }

                var status = request.IsHalfDay && request.FromDate == day.Date && request.ToDate == day.Date
                    ? AttendanceDayStatus.HalfDay
                    : AttendanceDayStatus.OnLeave;

                result[day.Date] = new ApprovedLeaveDayInfo(status, request.LeaveType!.Code);
            }
        }

        return result;
    }

    public static void ApplyLeaveOverlay(
        Dictionary<DateOnly, AttendanceDayStatus> dayStatuses,
        IReadOnlyList<AttendanceDay> storedDays,
        IReadOnlyDictionary<DateOnly, ApprovedLeaveDayInfo> leaveOverlay)
    {
        var manualDates = storedDays
            .Where(x => x.Source == AttendanceSource.Manual)
            .Select(x => x.Date)
            .ToHashSet();

        foreach (var (date, info) in leaveOverlay)
        {
            if (!manualDates.Contains(date))
            {
                dayStatuses[date] = info.Status;
            }
        }
    }

    private async Task<Dictionary<Guid, LeavePolicy>> GetLeavePoliciesAsync(
        IReadOnlyList<LeaveRequest> leaveRequests,
        int year,
        CancellationToken cancellationToken)
    {
        var leaveTypeIds = leaveRequests.Select(x => x.LeaveTypeId).Distinct().ToList();
        return await _db.LeavePolicies.AsNoTracking()
            .Where(x => leaveTypeIds.Contains(x.LeaveTypeId) && x.Year == year)
            .ToDictionaryAsync(x => x.LeaveTypeId, cancellationToken);
    }

    public async Task<HashSet<DateOnly>> GetUnpaidLeaveDatesAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var unpaidDates = new HashSet<DateOnly>();

        var requests = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == employeeId &&
                        x.Status == LeaveRequestStatus.Approved &&
                        x.FromDate <= end &&
                        x.ToDate >= start)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var isPaid = request.LeaveType?.IsPaid ?? true;
            for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
            {
                if (date.Year != year || date.Month != month)
                {
                    continue;
                }

                if (!isPaid)
                {
                    unpaidDates.Add(date);
                }
            }
        }

        var absences = await _db.AttendanceDays.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.Date >= start && x.Date <= end &&
                        (x.Status == AttendanceDayStatus.Absent || x.Status == AttendanceDayStatus.HalfDay))
            .ToListAsync(cancellationToken);

        foreach (var absence in absences)
        {
            unpaidDates.Add(absence.Date);
        }

        return unpaidDates;
    }
}

public sealed class GetAttendanceMonthQueryHandler : IRequestHandler<GetAttendanceMonthQuery, IReadOnlyList<AttendanceDayDto>>
{
    private readonly IAppDbContext _db;
    private readonly AttendanceService _attendance;

    public GetAttendanceMonthQueryHandler(IAppDbContext db, AttendanceService attendance)
    {
        _db = db;
        _attendance = attendance;
    }

    public async Task<IReadOnlyList<AttendanceDayDto>> Handle(
        GetAttendanceMonthQuery request,
        CancellationToken cancellationToken)
    {
        var employeesQuery = _db.Employees.AsNoTracking()
            .Where(x => x.Status == EmployeeStatus.Active);

        if (request.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(x => x.DepartmentId == request.DepartmentId);
        }

        if (request.EmployeeId.HasValue)
        {
            employeesQuery = employeesQuery.Where(x => x.Id == request.EmployeeId);
        }

        var employees = await employeesQuery.OrderBy(x => x.EmployeeCode).ToListAsync(cancellationToken);
        var start = new DateOnly(request.Year, request.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var records = await _db.AttendanceDays.AsNoTracking()
            .Where(x => x.Date >= start && x.Date <= end && employees.Select(e => e.Id).Contains(x.EmployeeId))
            .ToListAsync(cancellationToken);

        var employeeIds = employees.Select(e => e.Id).ToList();
        var holidayCache = new Dictionary<Guid, IReadOnlySet<DateOnly>>();
        async Task<IReadOnlySet<DateOnly>> ResolveHolidays(Guid employeeId)
        {
            if (!holidayCache.TryGetValue(employeeId, out var holidays))
            {
                var employee = employees.First(e => e.Id == employeeId);
                holidays = await _attendance.GetHolidayDatesAsync(employee.LeaveCalendarId, request.Year, request.Month, cancellationToken);
                holidayCache[employeeId] = holidays;
            }

            return holidays;
        }

        var leaveMaps = await _attendance.GetApprovedClElLeaveMapsAsync(
            employeeIds,
            request.Year,
            request.Month,
            ResolveHolidays,
            cancellationToken);

        var list = new List<AttendanceDayDto>();
        foreach (var employee in employees)
        {
            var holidays = await ResolveHolidays(employee.Id);
            leaveMaps.TryGetValue(employee.Id, out var leaveOverlay);
            var employeeRecords = records.Where(x => x.EmployeeId == employee.Id).ToList();

            foreach (var date in AttendanceCalendarService.EnumerateMonthDates(request.Year, request.Month))
            {
                var existing = employeeRecords.FirstOrDefault(x => x.Date == date);
                var dto = BuildAttendanceDayDto(
                    existing,
                    employee,
                    date,
                    holidays,
                    leaveOverlay,
                    request.EmployeeId.HasValue);

                if (dto is not null)
                {
                    list.Add(dto);
                }
            }
        }

        return list;
    }

    private static AttendanceDayDto? BuildAttendanceDayDto(
        AttendanceDay? existing,
        Domain.Entities.Organization.Employee employee,
        DateOnly date,
        IReadOnlySet<DateOnly> holidays,
        Dictionary<DateOnly, ApprovedLeaveDayInfo>? leaveOverlay,
        bool includeNonWorkingDefaults)
    {
        if (existing?.Source == AttendanceSource.Manual)
        {
            return new AttendanceDayDto(
                existing.Id,
                employee.Id,
                employee.FullName,
                employee.EmployeeCode,
                date,
                existing.Status.ToString(),
                existing.Source.ToString(),
                existing.Remarks,
                null);
        }

        if (leaveOverlay is not null && leaveOverlay.TryGetValue(date, out var leaveInfo))
        {
            return new AttendanceDayDto(
                existing?.Id,
                employee.Id,
                employee.FullName,
                employee.EmployeeCode,
                date,
                leaveInfo.Status.ToString(),
                AttendanceSource.LeaveSync.ToString(),
                existing?.Remarks,
                leaveInfo.LeaveTypeCode);
        }

        if (existing is not null)
        {
            return new AttendanceDayDto(
                existing.Id,
                employee.Id,
                employee.FullName,
                employee.EmployeeCode,
                date,
                existing.Status.ToString(),
                existing.Source.ToString(),
                existing.Remarks,
                null);
        }

        var defaultStatus = AttendanceCalendarService.ResolveDefaultStatus(date, holidays, true, true);
        if (!includeNonWorkingDefaults &&
            defaultStatus is AttendanceDayStatus.WeekOff or AttendanceDayStatus.Holiday)
        {
            return null;
        }

        return new AttendanceDayDto(
            null,
            employee.Id,
            employee.FullName,
            employee.EmployeeCode,
            date,
            defaultStatus.ToString(),
            AttendanceSource.Manual.ToString(),
            null,
            null);
    }
}

public sealed class GetAttendanceSummariesQueryHandler
    : IRequestHandler<GetAttendanceSummariesQuery, IReadOnlyList<AttendanceMonthSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly AttendanceService _attendance;

    public GetAttendanceSummariesQueryHandler(IAppDbContext db, AttendanceService attendance)
    {
        _db = db;
        _attendance = attendance;
    }

    public async Task<IReadOnlyList<AttendanceMonthSummaryDto>> Handle(
        GetAttendanceSummariesQuery request,
        CancellationToken cancellationToken)
    {
        var employeesQuery = _db.Employees.AsNoTracking().Where(x => x.Status == EmployeeStatus.Active);
        if (request.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(x => x.DepartmentId == request.DepartmentId);
        }

        var employees = await employeesQuery.OrderBy(x => x.EmployeeCode).ToListAsync(cancellationToken);
        var summaries = new List<AttendanceMonthSummaryDto>();

        foreach (var employee in employees)
        {
            var holidays = await _attendance.GetHolidayDatesAsync(employee.LeaveCalendarId, request.Year, request.Month, cancellationToken);
            var dayMap = await _attendance.BuildDayStatusMapAsync(employee.Id, request.Year, request.Month, holidays, cancellationToken);
            var unpaid = await _attendance.GetUnpaidLeaveDatesAsync(employee.Id, request.Year, request.Month, cancellationToken);
            var summary = AttendanceCalendarService.Summarize(request.Year, request.Month, dayMap, unpaid);
            summaries.Add(new AttendanceMonthSummaryDto(
                summary.Year,
                summary.Month,
                employee.Id,
                employee.FullName,
                summary.WorkingDays,
                summary.PresentDays,
                summary.AbsentDays,
                summary.LeaveDays,
                summary.LopDays,
                summary.PaidDays));
        }

        return summaries;
    }
}

public sealed class UpsertAttendanceDayCommandHandler : IRequestHandler<UpsertAttendanceDayCommand, Result<AttendanceDayDto>>
{
    private readonly IAppDbContext _db;

    public UpsertAttendanceDayCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<AttendanceDayDto>> Handle(
        UpsertAttendanceDayCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result<AttendanceDayDto>.Failure("Employee not found.");
        }

        var row = await _db.AttendanceDays
            .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.Date == request.Date, cancellationToken);

        if (row is null)
        {
            row = new AttendanceDay
            {
                EmployeeId = request.EmployeeId,
                Date = request.Date,
                Status = request.Status,
                Source = AttendanceSource.Manual,
                Remarks = request.Remarks
            };
            _db.AttendanceDays.Add(row);
        }
        else
        {
            row.Status = request.Status;
            row.Source = AttendanceSource.Manual;
            row.Remarks = request.Remarks;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<AttendanceDayDto>.Success(new AttendanceDayDto(
            row.Id,
            employee.Id,
            employee.FullName,
            employee.EmployeeCode,
            row.Date,
            row.Status.ToString(),
            row.Source.ToString(),
            row.Remarks,
            null));
    }
}

public sealed class SyncAttendanceFromLeaveCommandHandler : IRequestHandler<SyncAttendanceFromLeaveCommand, Result<int>>
{
    private readonly IAppDbContext _db;

    public SyncAttendanceFromLeaveCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<int>> Handle(SyncAttendanceFromLeaveCommand request, CancellationToken cancellationToken)
    {
        var start = new DateOnly(request.Year, request.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var employeesQuery = _db.Employees.Where(x => x.Status == EmployeeStatus.Active);
        if (request.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(x => x.DepartmentId == request.DepartmentId);
        }

        var employeeIds = await employeesQuery.Select(x => x.Id).ToListAsync(cancellationToken);
        var leaveRequests = await _db.LeaveRequests.AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId) &&
                        x.Status == LeaveRequestStatus.Approved &&
                        x.FromDate <= end &&
                        x.ToDate >= start)
            .ToListAsync(cancellationToken);

        var upsertCount = 0;
        foreach (var leave in leaveRequests)
        {
            for (var date = leave.FromDate; date <= leave.ToDate; date = date.AddDays(1))
            {
                if (date < start || date > end)
                {
                    continue;
                }

                var existing = await _db.AttendanceDays
                    .FirstOrDefaultAsync(x => x.EmployeeId == leave.EmployeeId && x.Date == date, cancellationToken);

                if (existing is null)
                {
                    _db.AttendanceDays.Add(new AttendanceDay
                    {
                        EmployeeId = leave.EmployeeId,
                        Date = date,
                        Status = leave.IsHalfDay && leave.FromDate == date && leave.ToDate == date
                            ? AttendanceDayStatus.HalfDay
                            : AttendanceDayStatus.OnLeave,
                        Source = AttendanceSource.LeaveSync,
                        Remarks = "Synced from approved leave"
                    });
                    upsertCount++;
                }
                else if (existing.Source == AttendanceSource.LeaveSync)
                {
                    existing.Status = leave.IsHalfDay && leave.FromDate == date && leave.ToDate == date
                        ? AttendanceDayStatus.HalfDay
                        : AttendanceDayStatus.OnLeave;
                    upsertCount++;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(upsertCount);
    }
}
