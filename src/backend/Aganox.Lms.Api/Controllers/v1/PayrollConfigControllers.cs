using Aganox.Lms.Application.Features.Payroll.Configuration;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/components")]
[Authorize(Policy = Permissions.PayrollConfigView)]
public sealed class PayrollComponentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollComponentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetSalaryComponentsQuery(activeOnly), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> Create([FromBody] CreateSalaryComponentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? Created(string.Empty, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateSalaryComponentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateSalaryComponentCommand(
            id, command.Code, command.NameEn, command.NameTa, command.Category, command.CalculationType,
            command.BaseComponentCode, command.SortOrder, command.IsTaxable, command.IncludeInGross, command.IsActive), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/rules")]
[Authorize(Policy = Permissions.PayrollConfigView)]
public sealed class PayrollRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? componentId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSalaryRulesQuery(componentId), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> Create([FromBody] CreateSalaryRuleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? Created(string.Empty, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateSalaryRuleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateSalaryRuleCommand(
            id, command.EffectiveFrom, command.EffectiveTo, command.Priority, command.ParametersJson, command.IsActive), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll/salaries")]
[Authorize(Policy = Permissions.PayrollSalaryView)]
public sealed class EmployeeSalariesController : ControllerBase
{
    private readonly IMediator _mediator;
    public EmployeeSalariesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{employeeId:guid}")]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetEmployeeSalaryRevisionsQuery(employeeId), cancellationToken));

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewEmployeeSalaryQuery query, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(query, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.PayrollSalaryManage)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeSalaryRevisionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? Created(string.Empty, result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PayrollSalaryManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateEmployeeSalaryRevisionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateEmployeeSalaryRevisionCommand(
            id, command.EffectiveFrom, command.EffectiveTo, command.BasicPay, command.Remarks, command.Overrides), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }
}
