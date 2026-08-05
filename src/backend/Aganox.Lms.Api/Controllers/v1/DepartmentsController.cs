using Aganox.Lms.Application.Features.Organization.Departments.Commands;
using Aganox.Lms.Application.Features.Organization.Departments.Queries;
using Aganox.Lms.Application.Features.Organization.Dtos;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Authorize]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeesView)]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var items = await _mediator.Send(new GetDepartmentsQuery(activeOnly), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetDepartmentByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeesCreate)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateDepartmentCommand(request.Name, request.Code, request.IsActive),
            cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateDepartmentCommand(id, request.Name, request.Code, request.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteDepartmentCommand(id), cancellationToken);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }
}
