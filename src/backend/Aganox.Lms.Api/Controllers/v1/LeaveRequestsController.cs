using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Application.Features.LeaveRequests;
using Aganox.Lms.Application.Features.LeaveRequests.Dtos;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-requests")]
[Authorize]
public sealed class LeaveRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveRequestsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? employeeId,
        [FromQuery] LeaveRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLeaveRequestsQuery(employeeId, status, page, pageSize), cancellationToken);
        return Ok(new
        {
            items = result.Items,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetLeaveRequestByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.LeaveApply)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        HalfDayPeriod? halfDay = null;
        if (!string.IsNullOrWhiteSpace(request.HalfDayPeriod) &&
            Enum.TryParse<HalfDayPeriod>(request.HalfDayPeriod, true, out var parsed))
        {
            halfDay = parsed;
        }

        var result = await _mediator.Send(new CreateLeaveRequestCommand(
            request.EmployeeId, request.LeaveTypeId, request.FromDate, request.ToDate, request.IsHalfDay,
            halfDay, request.Remarks, request.Submit), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.LeaveApply)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        HalfDayPeriod? halfDay = null;
        if (!string.IsNullOrWhiteSpace(request.HalfDayPeriod) &&
            Enum.TryParse<HalfDayPeriod>(request.HalfDayPeriod, true, out var parsed))
        {
            halfDay = parsed;
        }

        var result = await _mediator.Send(new UpdateLeaveRequestCommand(
            id, request.LeaveTypeId, request.FromDate, request.ToDate, request.IsHalfDay,
            halfDay, request.Remarks, request.Submit), cancellationToken);

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Permissions.LeaveApply)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitLeaveRequestCommand(id), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.LeaveApply)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelLeaveRequestCommand(id), cancellationToken);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
