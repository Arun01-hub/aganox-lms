using Aganox.Lms.Application.Features.Reports;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = Permissions.ReportsView)]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("employee-leave")]
    public async Task<IActionResult> EmployeeLeave(
        [FromQuery] Guid? employeeId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEmployeeLeaveReportQuery(employeeId, from, to), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMonthlyLeaveReportQuery(year, month, departmentId), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("yearly")]
    public async Task<IActionResult> Yearly(
        [FromQuery] int year,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetYearlyLeaveReportQuery(year, departmentId), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("department")]
    public async Task<IActionResult> Department(
        [FromQuery] Guid departmentId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDepartmentLeaveReportQuery(departmentId, from, to), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("balances")]
    public async Task<IActionResult> Balances(
        [FromQuery] int year,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBalanceReportQuery(year, departmentId), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("attendance-summary")]
    public async Task<IActionResult> AttendanceSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAttendanceSummaryReportQuery(from, to), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("trends")]
    public async Task<IActionResult> Trends(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTrendReportQuery(from, to), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("{name}/export")]
    [Authorize(Policy = Permissions.ReportsExport)]
    public async Task<IActionResult> Export(
        string name,
        [FromQuery] string format = "csv",
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExportReportQuery(
            name, format, employeeId, from, to, year, month, departmentId, lang), cancellationToken);

        if (result is null)
        {
            return BadRequest(new { errors = new[] { "Export failed or unsupported report/format." } });
        }

        return File(result.Content, result.ContentType, result.FileName);
    }
}
