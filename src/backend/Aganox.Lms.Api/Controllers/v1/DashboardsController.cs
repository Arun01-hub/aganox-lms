using Aganox.Lms.Application.Features.Dashboards;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboards")]
[Authorize]
public sealed class DashboardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("employee")]
    public async Task<IActionResult> Employee([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var result = await _mediator.Send(new GetEmployeeDashboardQuery(y), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("manager")]
    public async Task<IActionResult> Manager(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var result = await _mediator.Send(
            new GetManagerDashboardQuery(year ?? now.Year, month ?? now.Month),
            cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Policy = Permissions.LeaveViewAll)]
    public async Task<IActionResult> Admin([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery(year ?? DateTime.UtcNow.Year), cancellationToken);
        return result is null ? Forbid() : Ok(result);
    }
}
