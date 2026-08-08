using Aganox.Lms.Application.Features.Payroll.Arrears;
using Aganox.Lms.Application.Features.Payroll.Entry;
using Aganox.Lms.Application.Features.Payroll.Payslips;
using Aganox.Lms.Application.Features.Payroll.Processing;
using Aganox.Lms.Application.Features.Payroll.Reports;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/runs")]
[Authorize(Policy = Permissions.PayrollProcess)]
public sealed class PayrollRunsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollRunsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? year, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPayrollRunsQuery(year), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetPayrollRunByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> Results(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPayrollRunResultsQuery(id), cancellationToken));

    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.PayrollAuditView)]
    public async Task<IActionResult> Audit(Guid id, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPayrollRunAuditQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePayrollRunCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/calculate")]
    public async Task<IActionResult> Calculate(Guid id, [FromQuery] bool recalculate = false, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CalculatePayrollRunCommand(id, recalculate), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitPayrollRunForApprovalCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Permissions.PayrollApprove)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApprovePayrollRunCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/lock")]
    [Authorize(Policy = Permissions.PayrollLock)]
    public async Task<IActionResult> Lock(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LockPayrollRunCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("{id:guid}/salary-entry/context")]
    public async Task<IActionResult> SalaryEntryContext(Guid id, CancellationToken cancellationToken)
    {
        var context = await _mediator.Send(new GetSalaryEntryContextQuery(id), cancellationToken);
        return context is null ? NotFound() : Ok(context);
    }

    [HttpGet("{id:guid}/salary-entry")]
    public async Task<IActionResult> GetSalaryEntry(
        Guid id,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var entry = await _mediator.Send(
            new GetSalaryEntryQuery(id, departmentId, employeeId),
            cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPut("{id:guid}/salary-entry/{employeeId:guid}")]
    public async Task<IActionResult> SaveSalaryEntry(
        Guid id,
        Guid employeeId,
        [FromBody] SaveSalaryEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SaveSalaryEntryCommand(id, employeeId, request.Lines),
            cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/salary-entry/{employeeId:guid}/reset")]
    public async Task<IActionResult> ResetSalaryEntry(Guid id, Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResetSalaryEntryCommand(id, employeeId),
            cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/salary-entry/{employeeId:guid}")]
    public async Task<IActionResult> DeleteSalaryEntry(Guid id, Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteSalaryEntryCommand(id, employeeId),
            cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}

public sealed record SaveSalaryEntryRequest(IReadOnlyList<SaveSalaryEntryLineDto> Lines);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/payslips")]
[Authorize]
public sealed class PayrollPayslipsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollPayslipsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{runId:guid}")]
    public async Task<IActionResult> Get(Guid runId, [FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetPayslipQuery(runId, employeeId), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{runId:guid}/export")]
    public async Task<IActionResult> Export(
        Guid runId,
        [FromQuery] Guid employeeId,
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExportPayslipPdfQuery(runId, employeeId, lang), cancellationToken);
        if (result is null) return NotFound();
        return File(result.Content, result.ContentType, result.FileName);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/reports")]
[Authorize(Policy = Permissions.PayrollReportsView)]
public sealed class PayrollReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{runId:guid}")]
    public async Task<IActionResult> Get(Guid runId, [FromQuery] string reportType, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPayrollReportQuery(runId, reportType), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{runId:guid}/export")]
    [Authorize(Policy = Permissions.PayrollReportsExport)]
    public async Task<IActionResult> Export(
        Guid runId,
        [FromQuery] string reportType,
        [FromQuery] string format = "csv",
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExportPayrollReportQuery(runId, reportType, format, lang), cancellationToken);
        if (result is null) return NotFound();
        return File(result.Content, result.ContentType, result.FileName);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/arrears")]
[Authorize(Policy = Permissions.PayrollArrearsManage)]
public sealed class PayrollArrearsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollArrearsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? employeeId, [FromQuery] PayrollArrearStatus? status, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPayrollArrearsQuery(employeeId, status), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePayrollArrearCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? Created(string.Empty, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelPayrollArrearCommand(id), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
