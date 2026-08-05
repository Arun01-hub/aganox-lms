using Aganox.Lms.Application.Features.LeaveConfiguration.Balances;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Application.Features.LeaveConfiguration.LeavePolicies;
using Aganox.Lms.Application.Features.LeaveRequests;
using Aganox.Lms.Application.Features.LeaveRequests.Dtos;
using Aganox.Lms.Application.Features.LeaveConfiguration.LeaveTypes;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-types")]
[Authorize]
public sealed class LeaveTypesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeaveTypesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetLeaveTypesQuery(activeOnly), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetLeaveTypeByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateLeaveTypeCommand(
            request.Code, request.Name, request.IsPaid, request.RequiresApproval, request.AllowHalfDay,
            request.AllowNegativeBalance, request.MaxDaysPerRequest, request.ColorHex, request.SortOrder, request.IsActive),
            cancellationToken);
        return result.Succeeded ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateLeaveTypeCommand(
            id, request.Code, request.Name, request.IsPaid, request.RequiresApproval, request.AllowHalfDay,
            request.AllowNegativeBalance, request.MaxDaysPerRequest, request.ColorHex, request.SortOrder, request.IsActive),
            cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteLeaveTypeCommand(id), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : NoContent();
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-policies")]
[Authorize]
public sealed class LeavePoliciesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeavePoliciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? leaveTypeId, [FromQuery] int? year, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetLeavePoliciesQuery(leaveTypeId, year), cancellationToken));

    [HttpPut]
    [Authorize(Policy = Permissions.LeaveTypesManage)]
    public async Task<IActionResult> Upsert([FromBody] UpsertLeavePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpsertLeavePolicyCommand(
            request.LeaveTypeId, request.Year, request.SkipWeekends, request.SkipHolidays,
            request.CarryForwardMaxDays, request.YearlyResetEnabled, request.DefaultOpeningBalance, request.AccrualStartDate),
            cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-requests")]
[Authorize]
public sealed class LeavePreviewController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeavePreviewController(IMediator mediator) => _mediator = mediator;

    [HttpPost("preview-days")]
    [Authorize(Policy = Permissions.LeaveApply)]
    public async Task<IActionResult> PreviewDays([FromBody] PreviewLeaveDaysRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PreviewLeaveDaysQuery(
            request.EmployeeId, request.LeaveTypeId, request.FromDate, request.ToDate, request.IsHalfDay),
            cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-balances")]
[Authorize]
public sealed class LeaveBalancesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeaveBalancesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("employee/{employeeId:guid}")]
    [Authorize(Policy = Permissions.BalancesView)]
    public async Task<IActionResult> GetForEmployee(
        Guid employeeId, [FromQuery] int year, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEmployeeLeaveBalancesQuery(employeeId, year), cancellationToken));

    [HttpPut("employee/{employeeId:guid}/opening")]
    [Authorize(Policy = Permissions.BalancesAdjust)]
    public async Task<IActionResult> SetOpening(
        Guid employeeId, [FromBody] SetOpeningBalancesRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetEmployeeOpeningBalancesCommand(
            employeeId, request.Year, request.Balances), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = Permissions.BalancesAdjust)]
    public async Task<IActionResult> Adjust([FromBody] CreateBalanceAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateBalanceAdjustmentCommand(
            request.EmployeeId, request.LeaveTypeId, request.Year, request.DeltaDays, request.Reason),
            cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }
}
