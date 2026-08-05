using System.Globalization;
using System.Text;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Reports.Dtos;
using Aganox.Lms.Application.Features.Reports.Services;
using Aganox.Lms.Domain.Enums;
using ClosedXML.Excel;
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
    Guid? DepartmentId) : IRequest<ReportExportResult?>;

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
    private readonly ICurrentUserService _currentUser;

    public ExportReportQueryHandler(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
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

        var (headers, rows, title) = await LoadReportDataAsync(request, cancellationToken);
        if (headers is null)
        {
            return null;
        }

        var safeName = request.ReportName.ToLowerInvariant().Replace(' ', '-');
        return format switch
        {
            "csv" => new ReportExportResult(
                Encoding.UTF8.GetBytes(BuildCsv(headers, rows)),
                "text/csv",
                $"{safeName}.csv"),
            "xlsx" => new ReportExportResult(
                BuildXlsx(title, headers, rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{safeName}.xlsx"),
            "pdf" => new ReportExportResult(
                BuildSimplePdf(title, headers, rows),
                "application/pdf",
                $"{safeName}.pdf"),
            _ => null
        };
    }

    private async Task<(string[]? Headers, IReadOnlyList<string[]> Rows, string Title)> LoadReportDataAsync(
        ExportReportQuery request,
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
                    ["Employee Code", "Employee", "Department", "Leave Type", "From", "To", "Days", "Status"],
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
                    ["Department", "Leave Type", "Requests", "Total Days"],
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
                    ["Department", "Leave Type", "Requests", "Total Days"],
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
                    ["Code", "Employee", "Department", "Leave Type", "Opening", "Consumed", "Remaining"],
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
                    ["Period", "Requests", "Total Days"],
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

    private static string BuildCsv(string[] headers, IReadOnlyList<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(EscapeCsv)));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static byte[] BuildXlsx(string title, string[] headers, IReadOnlyList<string[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");
        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;

        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(3, c + 1).Value = headers[c];
            sheet.Cell(3, c + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < headers.Length; c++)
            {
                sheet.Cell(r + 4, c + 1).Value = rows[r][c];
            }
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildSimplePdf(string title, string[] headers, IReadOnlyList<string[]> rows)
    {
        var lines = new List<string> { title, string.Empty, string.Join(" | ", headers) };
        lines.AddRange(rows.Select(r => string.Join(" | ", r)));
        var text = string.Join("\n", lines);
        return SimplePdfWriter.Create(text);
    }
}

internal static class SimplePdfWriter
{
    public static byte[] Create(string text)
    {
        var escaped = text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", string.Empty);
        var lines = escaped.Split('\n');
        var content = new StringBuilder();
        content.AppendLine("BT /F1 10 Tf 50 750 Td 14 TL");
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                content.AppendLine("T*");
            }

            content.Append('(').Append(lines[i]).AppendLine(") Tj");
        }

        content.AppendLine("ET");
        var stream = content.ToString();
        var streamBytes = Encoding.UTF8.GetBytes(stream);

        using var ms = new MemoryStream();
        void W(string s) => ms.Write(Encoding.ASCII.GetBytes(s));
        W("%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        void Object(int id, Action write)
        {
            offsets.Add(ms.Position);
            W($"{id} 0 obj\n");
            write();
            W("endobj\n");
        }

        Object(1, () => W("<< /Type /Catalog /Pages 2 0 R >>"));
        Object(2, () => W("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));
        Object(3, () => W("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>"));
        Object(4, () =>
        {
            W($"<< /Length {streamBytes.Length} >>\nstream\n");
            ms.Write(streamBytes);
            W("\nendstream");
        });
        Object(5, () => W("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        var xref = ms.Position;
        W($"xref\n0 {offsets.Count}\n");
        W("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            W($"{offsets[i]:D10} 00000 n \n");
        }

        W($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return ms.ToArray();
    }
}
