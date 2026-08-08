using Aganox.Lms.Application.Common.Export;
using Aganox.Lms.Application.Common.Export.Templates;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Payroll.Processing;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services.Payroll;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Payroll.Payslips;

public sealed record PayslipDto(
    Guid PayrollRunId,
    int Year,
    int Month,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    string DepartmentName,
    decimal BasicPay,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    decimal PaidDays,
    decimal LopDays,
    IReadOnlyList<PayrollLineItemDto> LineItems);

public sealed record GetPayslipQuery(Guid PayrollRunId, Guid? EmployeeId) : IRequest<PayslipDto?>;
public sealed record ExportPayslipPdfQuery(Guid PayrollRunId, Guid EmployeeId, string Language = "en")
    : IRequest<PayslipExportResult?>;
public sealed record PayslipExportResult(byte[] Content, string ContentType, string FileName);

public sealed class GetPayslipQueryHandler : IRequestHandler<GetPayslipQuery, PayslipDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPayslipQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PayslipDto?> Handle(GetPayslipQuery request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId;
        if (!employeeId.HasValue)
        {
            if (_currentUser.Permissions.Contains(Permissions.PayrollPayslipViewAll))
            {
                return null;
            }

            employeeId = _currentUser.UserId.HasValue
                ? await _db.Employees.AsNoTracking()
                    .Where(x => x.UserId == _currentUser.UserId)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
        }
        else if (!_currentUser.Permissions.Contains(Permissions.PayrollPayslipViewAll))
        {
            var ownEmployeeId = await _db.Employees.AsNoTracking()
                .Where(x => x.UserId == _currentUser.UserId)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (ownEmployeeId != employeeId)
            {
                return null;
            }
        }

        if (!employeeId.HasValue) return null;

        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);

        var canViewPreview = _currentUser.Permissions.Contains(Permissions.PayrollPayslipViewAll)
            || _currentUser.Permissions.Contains(Permissions.PayrollProcess);

        if (run is null || !PayrollRunGuard.CanViewPayslip(run.Status, canViewPreview))
        {
            return null;
        }

        var result = await _db.PayrollEmployeeResults.AsNoTracking()
            .Include(x => x.Employee!).ThenInclude(e => e!.Department)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.PayrollRunId == request.PayrollRunId && x.EmployeeId == employeeId, cancellationToken);

        if (result is null) return null;

        return new PayslipDto(
            run.Id,
            run.Year,
            run.Month,
            result.EmployeeId,
            result.Employee!.FullName,
            result.Employee.EmployeeCode,
            result.Employee.Department?.Name ?? string.Empty,
            result.BasicPaySnapshot,
            result.GrossPay,
            result.TotalDeductions,
            result.NetPay,
            result.PaidDays,
            result.LopDays,
            result.LineItems.OrderBy(x => x.SortOrder).Select(x => new PayrollLineItemDto(
                x.ComponentCode,
                x.ComponentName,
                x.Category.ToString(),
                x.Amount,
                x.SortOrder)).ToList());
    }
}

public sealed class ExportPayslipPdfQueryHandler : IRequestHandler<ExportPayslipPdfQuery, PayslipExportResult?>
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExportPayslipPdfQueryHandler(IMediator mediator, IAppDbContext db, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PayslipExportResult?> Handle(ExportPayslipPdfQuery request, CancellationToken cancellationToken)
    {
        var payslip = await _mediator.Send(new GetPayslipQuery(request.PayrollRunId, request.EmployeeId), cancellationToken);
        if (payslip is null) return null;

        _db.PayrollRunAudits.Add(new Domain.Entities.Payroll.PayrollRunAudit
        {
            PayrollRunId = request.PayrollRunId,
            Action = PayrollAuditAction.PayslipGenerated,
            UserId = _currentUser.UserId,
            Details = $"Payslip generated for employee {payslip.EmployeeCode}"
        });
        await _db.SaveChangesAsync(cancellationToken);

        var language = ExportLanguageParser.Parse(request.Language);
        var companyName = await ExportCompanyName.ResolveAsync(_db, cancellationToken);
        var bytes = PayslipPdfTemplate.Generate(new PayslipTemplateModel(companyName, payslip, language));
        return new PayslipExportResult(bytes, "application/pdf", $"payslip-{payslip.EmployeeCode}-{payslip.Year}{payslip.Month:D2}.pdf");
    }
}
