using Aganox.Lms.Application.Features.Organization.Dtos;
using Aganox.Lms.Application.Features.Organization.Employees.Commands;
using Aganox.Lms.Application.Features.Organization.Employees.Queries;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public sealed class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeesView)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeStatus? status,
        [FromQuery] Guid? managerId,
        [FromQuery] string sortBy = "fullName",
        [FromQuery] string sortDir = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEmployeesQuery(search, departmentId, status, managerId, sortBy, sortDir, page, pageSize),
            cancellationToken);

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
    [Authorize(Policy = Permissions.EmployeesView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetEmployeeByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/profile")]
    [Authorize(Policy = Permissions.EmployeesView)]
    public async Task<IActionResult> Profile(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetEmployeeProfileQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeesCreate)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EmployeeStatus>(request.Status, true, out var status))
        {
            return BadRequest(new { errors = new[] { "Invalid employee status." } });
        }

        var result = await _mediator.Send(
            new CreateEmployeeCommand(
                request.EmployeeCode,
                request.FullName,
                request.DepartmentId,
                request.Designation,
                request.DateOfJoining,
                status,
                request.Email,
                request.Phone,
                request.ManagerId,
                request.LeaveCalendarId,
                request.CreateUserAccount,
                request.UserPassword,
                request.UserRole),
            cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EmployeesUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EmployeeStatus>(request.Status, true, out var status))
        {
            return BadRequest(new { errors = new[] { "Invalid employee status." } });
        }

        var result = await _mediator.Send(
            new UpdateEmployeeCommand(
                id,
                request.EmployeeCode,
                request.FullName,
                request.DepartmentId,
                request.Designation,
                request.DateOfJoining,
                status,
                request.Email,
                request.Phone,
                request.ManagerId,
                request.LeaveCalendarId),
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
        var result = await _mediator.Send(new DeleteEmployeeCommand(id), cancellationToken);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/link-user")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> LinkUser(Guid id, [FromBody] LinkEmployeeUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LinkEmployeeUserCommand(id, request.UserId, request.Email, request.Password, request.Role),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
