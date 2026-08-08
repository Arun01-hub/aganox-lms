using Aganox.Lms.Application.Common.Export;
using Aganox.Lms.Application.Common.Export.Templates;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Payroll.Processing;
using Aganox.Lms.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Payroll.Reports;

public sealed record PayrollRegisterRowDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal BasicPay,
    decimal GrossPay,
    decimal Deductions,
    decimal NetPay,
    decimal LopDays);

public sealed record PayrollComponentSummaryRowDto(string ComponentCode, string ComponentName, decimal TotalAmount);

public sealed record PayrollReportResult(string ReportType, IReadOnlyList<IDictionary<string, object>> Rows);

public sealed record GetPayrollReportQuery(Guid PayrollRunId, string ReportType) : IRequest<PayrollReportResult?>;
public sealed record ExportPayrollReportQuery(
    Guid PayrollRunId,
    string ReportType,
    string Format,
    string Language = "en") : IRequest<PayrollReportExportResult?>;
public sealed record PayrollReportExportResult(byte[] Content, string ContentType, string FileName);

public sealed class GetPayrollReportQueryHandler : IRequestHandler<GetPayrollReportQuery, PayrollReportResult?>
{
    private readonly IMediator _mediator;
    public GetPayrollReportQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<PayrollReportResult?> Handle(GetPayrollReportQuery request, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new GetPayrollRunResultsQuery(request.PayrollRunId), cancellationToken);
        if (results.Count == 0) return null;

        return request.ReportType.ToLowerInvariant() switch
        {
            "register" => new PayrollReportResult("register", results.Select(x => (IDictionary<string, object>)new Dictionary<string, object>
            {
                ["employeeCode"] = x.EmployeeCode,
                ["employeeName"] = x.EmployeeName,
                ["basicPay"] = x.BasicPay,
                ["grossPay"] = x.GrossPay,
                ["deductions"] = x.TotalDeductions,
                ["netPay"] = x.NetPay,
                ["lopDays"] = x.LopDays
            }).ToList()),
            "components" => new PayrollReportResult("components", results
                .SelectMany(x => x.LineItems)
                .GroupBy(x => x.ComponentCode)
                .Select(g => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["componentCode"] = g.Key,
                    ["componentName"] = g.First().ComponentName,
                    ["totalAmount"] = g.Sum(x => x.Amount)
                }).ToList()),
            "lop" => new PayrollReportResult("lop", results.Where(x => x.LopDays > 0).Select(x => (IDictionary<string, object>)new Dictionary<string, object>
            {
                ["employeeCode"] = x.EmployeeCode,
                ["employeeName"] = x.EmployeeName,
                ["lopDays"] = x.LopDays,
                ["netPay"] = x.NetPay
            }).ToList()),
            _ => null
        };
    }
}

public sealed class ExportPayrollReportQueryHandler : IRequestHandler<ExportPayrollReportQuery, PayrollReportExportResult?>
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExportPayrollReportQueryHandler(
        IMediator mediator,
        IAppDbContext db,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PayrollReportExportResult?> Handle(
        ExportPayrollReportQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.Permissions.Contains(Permissions.PayrollReportsExport) &&
            !_currentUser.Roles.Contains(Roles.Administrator))
        {
            return null;
        }

        var format = request.Format.ToLowerInvariant();
        if (format is not ("csv" or "xlsx" or "pdf"))
        {
            return null;
        }

        var report = await _mediator.Send(new GetPayrollReportQuery(request.PayrollRunId, request.ReportType), cancellationToken);
        if (report is null) return null;

        var language = ExportLanguageParser.Parse(request.Language);
        var headers = ExportLocalization.GetPayrollReportHeaders(language, report.ReportType);
        var rows = report.Rows
            .Select(row => PayrollReportRowFormatter.FormatRow(report.ReportType, row, language))
            .ToList();
        var title = ExportLocalization.GetPayrollReportTitle(language, report.ReportType);
        var companyName = await ExportCompanyName.ResolveAsync(_db, cancellationToken);

        var run = await _db.PayrollRuns.AsNoTracking()
            .Where(x => x.Id == request.PayrollRunId)
            .Select(x => new { x.Year, x.Month })
            .FirstOrDefaultAsync(cancellationToken);
        var texts = ExportLocalization.Get(language);
        var subtitle = run is null ? null : $"{texts.Period}: {run.Year}-{run.Month:D2}";

        var model = new TabularReportModel(companyName, title, subtitle, headers, rows, language);
        var safeType = report.ReportType.ToLowerInvariant();

        return format switch
        {
            "csv" => new PayrollReportExportResult(
                TabularReportCsvBuilder.Build(headers, rows),
                "text/csv",
                $"payroll-{safeType}.csv"),
            "xlsx" => new PayrollReportExportResult(
                TabularReportExcelTemplate.Generate(model),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"payroll-{safeType}.xlsx"),
            "pdf" => new PayrollReportExportResult(
                TabularReportPdfTemplate.Generate(model),
                "application/pdf",
                $"payroll-{safeType}.pdf"),
            _ => null
        };
    }
}
