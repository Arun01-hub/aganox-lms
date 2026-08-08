using Aganox.Lms.Application.Features.Attendance;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attendance")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    public AttendanceController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.AttendanceView)]
    public async Task<IActionResult> GetMonth(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAttendanceMonthQuery(year, month, departmentId, employeeId), cancellationToken));

    [HttpGet("summaries")]
    [Authorize(Policy = Permissions.AttendanceView)]
    public async Task<IActionResult> GetSummaries(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAttendanceSummariesQuery(year, month, departmentId), cancellationToken));

    [HttpPut]
    [Authorize(Policy = Permissions.AttendanceManage)]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertAttendanceDayRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpsertAttendanceDayCommand(
            request.EmployeeId, request.Date, request.Status, request.Remarks), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("sync-from-leave")]
    [Authorize(Policy = Permissions.AttendanceManage)]
    public async Task<IActionResult> SyncFromLeave(
        [FromBody] SyncAttendanceFromLeaveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SyncAttendanceFromLeaveCommand(
            request.Year, request.Month, request.DepartmentId), cancellationToken);
        return result.Succeeded ? Ok(new { synced = result.Data }) : BadRequest(new { errors = result.Errors });
    }
}

public sealed record UpsertAttendanceDayRequest(
    Guid EmployeeId,
    DateOnly Date,
    AttendanceDayStatus Status,
    string? Remarks);

public sealed record SyncAttendanceFromLeaveRequest(int Year, int Month, Guid? DepartmentId);
