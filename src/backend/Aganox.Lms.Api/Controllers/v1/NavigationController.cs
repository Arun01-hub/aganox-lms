using Aganox.Lms.Application.Features.Navigation;
using Aganox.Lms.Application.Features.Navigation.Dtos;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/navigation")]
[Authorize]
public sealed class NavigationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NavigationController(IMediator mediator) => _mediator = mediator;

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetNavigationMenuQuery(), cancellationToken));
}
