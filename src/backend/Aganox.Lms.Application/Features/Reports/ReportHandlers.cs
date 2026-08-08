using System.Globalization;
using Aganox.Lms.Application.Common.Export;
using Aganox.Lms.Application.Common.Export.Templates;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Reports.Dtos;
using Aganox.Lms.Application.Features.Reports.Services;
using Aganox.Lms.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Reports;

public sealed record GetEmployeeLeaveReportQuery(Guid? EmployeeId, DateOnly From, DateOnly To)
    : IRequest<ReportResultDto<EmployeeLeaveReportRowDto>?>;
public sealed record GetMonthlyLeaveReportQuery(int Year, int Month, Guid? DepartmentId)
    : IRequest<ReportResultDto<MonthlyLeaveReportRowDto>?>;
public sealed record GetYearlyLeaveReportQuery(int Year, Guid? DepartmentId)
    : IRequest<ReportResultDto<YearlyLeaveReportRowDto>?>;
public sealed record GetDepartmentLeaveReportQuery(Guid DepartmentId, DateOnly From, DateOnly To)
    : IRequest<ReportResultDto<DepartmentLeaveReportRowDto>?>;
public sealed record GetBalanceReportQuery(int Year, Guid? DepartmentId)
    : IRequest<ReportResultDto<BalanceReportRowDto>?>;
public sealed record GetAttendanceSummaryReportQuery(DateOnly From, DateOnly To)
    : IRequest<ReportResultDto<AttendanceSummaryRowDto>?>;
public sealed record GetTrendReportQuery(DateOnly From, DateOnly To)
    : IRequest<ReportResultDto<TrendReportRowDto>?>;

public sealed record ExportReportQuery(
    string ReportName,
    string Format,
    Guid? EmployeeId,
    DateOnly? From,
    DateOnly? To,
    int? Year,
    int? Month,
    Guid? DepartmentId,
    string Language = "en") : IRequest<ReportExportResult?>;

public sealed record ReportExportResult(byte[] Content, string ContentType, string FileName);

public sealed class GetEmployeeLeaveReportQueryHandler
    : IRequestHandler<GetEmployeeLeaveReportQuery, ReportResultDto<EmployeeLeaveReportRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetEmployeeLeaveReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<EmployeeLeaveReportRowDto>?> Handle(
        GetEmployeeLeaveReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var query = await ReportAccessService.ScopeLeaveRequestsAsync(
            _db, _currentUser, request.EmployeeId, cancellationToken);
        var rows = await query
            .Where(x => x.FromDate >= request.From && x.ToDate <= request.To)
            .OrderBy(x => x.FromDate)
            .Select(x => new EmployeeLeaveReportRowDto(
                x.EmployeeId,
                x.Employee!.EmployeeCode,
                x.Employee.FullName,
                x.Employee.Department!.Name,
                x.LeaveType!.Name,
                x.FromDate,
                x.ToDate,
                x.NumberOfDays,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new ReportResultDto<EmployeeLeaveReportRowDto>(rows, "Employee leave report");
    }
}

public sealed class GetMonthlyLeaveReportQueryHandler
    : IRequestHandler<GetMonthlyLeaveReportQuery, ReportResultDto<MonthlyLeaveReportRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMonthlyLeaveReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<MonthlyLeaveReportRowDto>?> Handle(
        GetMonthlyLeaveReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var start = new DateOnly(request.Year, request.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var query = await ReportAccessService.ScopeLeaveRequestsAsync(_db, _currentUser, null, cancellationToken);
        if (request.DepartmentId.HasValue)
        {
            query = query.Where(x => x.Employee!.DepartmentId == request.DepartmentId.Value);
        }

        var items = await query
            .Where(x => x.Status == LeaveRequestStatus.Approved && x.FromDate >= start && x.FromDate <= end)
            .ToListAsync(cancellationToken);

        var rows = items
            .GroupBy(x => new { Dept = x.Employee!.Department!.Name, Type = x.LeaveType!.Name })
            .Select(g => new MonthlyLeaveReportRowDto(g.Key.Dept, g.Key.Type, g.Count(), g.Sum(x => x.NumberOfDays)))
            .OrderBy(x => x.DepartmentName)
            .ThenBy(x => x.LeaveTypeName)
            .ToList();

        return new ReportResultDto<MonthlyLeaveReportRowDto>(rows, $"Monthly leave report {request.Year}-{request.Month:D2}");
    }
}

public sealed class GetYearlyLeaveReportQueryHandler
    : IRequestHandler<GetYearlyLeaveReportQuery, ReportResultDto<YearlyLeaveReportRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetYearlyLeaveReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<YearlyLeaveReportRowDto>?> Handle(
        GetYearlyLeaveReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var start = new DateOnly(request.Year, 1, 1);
        var end = new DateOnly(request.Year, 12, 31);

        var query = await ReportAccessService.ScopeLeaveRequestsAsync(_db, _currentUser, null, cancellationToken);
        if (request.DepartmentId.HasValue)
        {
            query = query.Where(x => x.Employee!.DepartmentId == request.DepartmentId.Value);
        }

        var items = await query
            .Where(x => x.Status == LeaveRequestStatus.Approved && x.FromDate >= start && x.FromDate <= end)
            .ToListAsync(cancellationToken);

        var rows = items
            .GroupBy(x => new { Dept = x.Employee!.Department!.Name, Type = x.LeaveType!.Name })
            .Select(g => new YearlyLeaveReportRowDto(g.Key.Dept, g.Key.Type, g.Count(), g.Sum(x => x.NumberOfDays)))
            .OrderBy(x => x.DepartmentName)
            .ToList();

        return new ReportResultDto<YearlyLeaveReportRowDto>(rows, $"Yearly leave report {request.Year}");
    }
}

public sealed class GetDepartmentLeaveReportQueryHandler
    : IRequestHandler<GetDepartmentLeaveReportQuery, ReportResultDto<DepartmentLeaveReportRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDepartmentLeaveReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<DepartmentLeaveReportRowDto>?> Handle(
        GetDepartmentLeaveReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var query = await ReportAccessService.ScopeLeaveRequestsAsync(_db, _currentUser, null, cancellationToken);
        var rows = await query
            .Where(x =>
                x.Employee!.DepartmentId == request.DepartmentId &&
                x.FromDate >= request.From &&
                x.ToDate <= request.To)
            .OrderBy(x => x.FromDate)
            .Select(x => new DepartmentLeaveReportRowDto(
                x.EmployeeId,
                x.Employee.FullName,
                x.LeaveType!.Name,
                x.FromDate,
                x.ToDate,
                x.NumberOfDays,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new ReportResultDto<DepartmentLeaveReportRowDto>(rows, "Department leave report");
    }
}

public sealed class GetBalanceReportQueryHandler
    : IRequestHandler<GetBalanceReportQuery, ReportResultDto<BalanceReportRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBalanceReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<BalanceReportRowDto>?> Handle(
        GetBalanceReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var employees = await ReportAccessService.ScopeEmployeesAsync(
            _db, _currentUser, request.DepartmentId, cancellationToken);
        var employeeIds = await employees.Select(x => x.Id).ToListAsync(cancellationToken);

        var rows = await _db.LeaveBalances.AsNoTracking()
            .Include(x => x.Employee)!.ThenInclude(x => x!.Department)
            .Include(x => x.LeaveType)
            .Where(x => x.Year == request.Year && employeeIds.Contains(x.EmployeeId))
            .OrderBy(x => x.Employee!.FullName)
            .Select(x => new BalanceReportRowDto(
                x.EmployeeId,
                x.Employee!.EmployeeCode,
                x.Employee.FullName,
                x.Employee.Department!.Name,
                x.LeaveType!.Code,
                x.OpeningBalance,
                x.Consumed,
                x.Remaining))
            .ToListAsync(cancellationToken);

        return new ReportResultDto<BalanceReportRowDto>(rows, $"Leave balances {request.Year}");
    }
}

public sealed class GetAttendanceSummaryReportQueryHandler
    : IRequestHandler<GetAttendanceSummaryReportQuery, ReportResultDto<AttendanceSummaryRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAttendanceSummaryReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<AttendanceSummaryRowDto>?> Handle(
        GetAttendanceSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var query = await ReportAccessService.ScopeLeaveRequestsAsync(_db, _currentUser, null, cancellationToken);
        var approved = await query
            .Where(x =>
                x.Status == LeaveRequestStatus.Approved &&
                x.FromDate <= request.To &&
                x.ToDate >= request.From)
            .ToListAsync(cancellationToken);

        var rows = new List<AttendanceSummaryRowDto>();
        for (var d = request.From; d <= request.To; d = d.AddDays(1))
        {
            var onLeave = approved.Where(x => x.FromDate <= d && x.ToDate >= d).ToList();
            if (onLeave.Count == 0)
            {
                continue;
            }

            rows.Add(new AttendanceSummaryRowDto(
                d,
                onLeave.Select(x => x.EmployeeId).Distinct().Count(),
                onLeave.Sum(x => x.IsHalfDay && x.FromDate == d && x.ToDate == d ? 0.5m : 1m)));
        }

        return new ReportResultDto<AttendanceSummaryRowDto>(rows, "Attendance summary");
    }
}

public sealed class GetTrendReportQueryHandler
    : IRequestHandler<GetTrendReportQuery, ReportResultDto<TrendReportRowDto>?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTrendReportQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportResultDto<TrendReportRowDto>?> Handle(
        GetTrendReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanRunReports(_currentUser))
        {
            return null;
        }

        var query = await ReportAccessService.ScopeLeaveRequestsAsync(_db, _currentUser, null, cancellationToken);
        var approved = await query
            .Where(x =>
                x.Status == LeaveRequestStatus.Approved &&
                x.FromDate >= request.From &&
                x.FromDate <= request.To)
            .ToListAsync(cancellationToken);

        var rows = approved
            .GroupBy(x => x.FromDate.ToString("yyyy-MM"))
            .Select(g => new TrendReportRowDto(g.Key, g.Count(), g.Sum(x => x.NumberOfDays)))
            .OrderBy(x => x.Period)
            .ToList();

        return new ReportResultDto<TrendReportRowDto>(rows, "Leave trends");
    }
}

public sealed class ExportReportQueryHandler : IRequestHandler<ExportReportQuery, ReportExportResult?>
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExportReportQueryHandler(
        IMediator mediator,
        IAppDbContext db,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReportExportResult?> Handle(ExportReportQuery request, CancellationToken cancellationToken)
    {
        if (!ReportAccessService.CanExport(_currentUser))
        {
            return null;
        }

        var format = request.Format.ToLowerInvariant();
        if (format is not ("csv" or "xlsx" or "pdf"))
        {
            return null;
        }

        var language = ExportLanguageParser.Parse(request.Language);
        var (headers, rows, title) = await LoadReportDataAsync(request, language, cancellationToken);
        if (headers is null)
        {
            return null;
        }

        var companyName = await ExportCompanyName.ResolveAsync(_db, cancellationToken);
        var model = new TabularReportModel(companyName, title, null, headers, rows, language);
        var safeName = request.ReportName.ToLowerInvariant().Replace(' ', '-');
        return format switch
        {
            "csv" => new ReportExportResult(
                TabularReportCsvBuilder.Build(headers, rows),
                "text/csv",
                $"{safeName}.csv"),
            "xlsx" => new ReportExportResult(
                TabularReportExcelTemplate.Generate(model),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{safeName}.xlsx"),
            "pdf" => new ReportExportResult(
                TabularReportPdfTemplate.Generate(model),
                "application/pdf",
                $"{safeName}.pdf"),
            _ => null
        };
    }

    private async Task<(string[]? Headers, IReadOnlyList<string[]> Rows, string Title)> LoadReportDataAsync(
        ExportReportQuery request,
        ExportLanguage language,
        CancellationToken cancellationToken)
    {
        switch (request.ReportName.ToLowerInvariant())
        {
            case "employee-leave":
            {
                if (!request.From.HasValue || !request.To.HasValue)
                {
                    return (null, [], string.Empty);
                }

                var report = await _mediator.Send(new GetEmployeeLeaveReportQuery(
                    request.EmployeeId, request.From.Value, request.To.Value), cancellationToken);
                if (report is null)
                {
                    return (null, [], string.Empty);
                }

                return (
                    ExportLocalization.GetLeaveReportHeaders(language, "employee-leave"),
                    report.Rows.Select(x => new[]
                    {
                        x.EmployeeCode, x.EmployeeName, x.DepartmentName, x.LeaveTypeName,
                        x.FromDate.ToString("yyyy-MM-dd"), x.ToDate.ToString("yyyy-MM-dd"),
                        x.NumberOfDays.ToString(CultureInfo.InvariantCulture), x.Status
                    }).ToList(),
                    report.Title);
            }
            case "monthly":
            {
                if (!request.Year.HasValue || !request.Month.HasValue)
                {
                    return (null, [], string.Empty);
                }

                var report = await _mediator.Send(new GetMonthlyLeaveReportQuery(
                    request.Year.Value, request.Month.Value, request.DepartmentId), cancellationToken);
                if (report is null)
                {
                    return (null, [], string.Empty);
                }

                return (
                    ExportLocalization.GetLeaveReportHeaders(language, "monthly"),
                    report.Rows.Select(x => new[]
                    {
                        x.DepartmentName, x.LeaveTypeName,
                        x.RequestCount.ToString(), x.TotalDays.ToString(CultureInfo.InvariantCulture)
                    }).ToList(),
                    report.Title);
            }
            case "yearly":
            {
                if (!request.Year.HasValue)
                {
                    return (null, [], string.Empty);
                }

                var report = await _mediator.Send(new GetYearlyLeaveReportQuery(
                    request.Year.Value, request.DepartmentId), cancellationToken);
                if (report is null)
                {
                    return (null, [], string.Empty);
                }

                return (
                    ExportLocalization.GetLeaveReportHeaders(language, "yearly"),
                    report.Rows.Select(x => new[]
                    {
                        x.DepartmentName, x.LeaveTypeName,
                        x.RequestCount.ToString(), x.TotalDays.ToString(CultureInfo.InvariantCulture)
                    }).ToList(),
                    report.Title);
            }
            case "balances":
            {
                if (!request.Year.HasValue)
                {
                    return (null, [], string.Empty);
                }

                var report = await _mediator.Send(new GetBalanceReportQuery(
                    request.Year.Value, request.DepartmentId), cancellationToken);
                if (report is null)
                {
                    return (null, [], string.Empty);
                }

                return (
                    ExportLocalization.GetLeaveReportHeaders(language, "balances"),
                    report.Rows.Select(x => new[]
                    {
                        x.EmployeeCode, x.EmployeeName, x.DepartmentName, x.LeaveTypeCode,
                        x.OpeningBalance.ToString(CultureInfo.InvariantCulture),
                        x.Consumed.ToString(CultureInfo.InvariantCulture),
                        x.Remaining.ToString(CultureInfo.InvariantCulture)
                    }).ToList(),
                    report.Title);
            }
            case "trends":
            {
                if (!request.From.HasValue || !request.To.HasValue)
                {
                    return (null, [], string.Empty);
                }

                var report = await _mediator.Send(new GetTrendReportQuery(
                    request.From.Value, request.To.Value), cancellationToken);
                if (report is null)
                {
                    return (null, [], string.Empty);
                }

                return (
                    ExportLocalization.GetLeaveReportHeaders(language, "trends"),
                    report.Rows.Select(x => new[]
                    {
                        x.Period, x.RequestCount.ToString(), x.TotalDays.ToString(CultureInfo.InvariantCulture)
                    }).ToList(),
                    report.Title);
            }
            default:
                return (null, [], string.Empty);
        }
    }
}
