using Aganox.Lms.Application.Features.Approvals;
using Aganox.Lms.Application.Features.Approvals.Dtos;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approvals")]
[Authorize(Policy = Permissions.ApprovalsAct)]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApprovalsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPendingApprovalsQuery(page, pageSize), cancellationToken);
        return Ok(new
        {
            items = result.Items,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetApprovalHistoryQuery(page, pageSize), cancellationToken);
        return Ok(new
        {
            items = result.Items,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages
        });
    }

    [HttpPost("{leaveRequestId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid leaveRequestId,
        [FromBody] ApprovalActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ApproveLeaveRequestCommand(leaveRequestId, request.Comments),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpPost("{leaveRequestId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid leaveRequestId,
        [FromBody] ApprovalActionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comments))
        {
            return BadRequest(new { errors = new[] { "Comments are required when rejecting." } });
        }

        var result = await _mediator.Send(
            new RejectLeaveRequestCommand(leaveRequestId, request.Comments),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
