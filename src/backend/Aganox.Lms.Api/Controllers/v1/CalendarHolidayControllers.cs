using Aganox.Lms.Application.Features.LeaveConfiguration.Calendars;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Application.Features.LeaveConfiguration.Holidays;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-calendars")]
[Authorize]
public sealed class LeaveCalendarsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeaveCalendarsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetLeaveCalendarsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetLeaveCalendarByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveCalendarRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateLeaveCalendarCommand(request.Name, request.IsDefault), cancellationToken);
        return result.Succeeded ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveCalendarRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateLeaveCalendarCommand(id, request.Name, request.IsDefault), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteLeaveCalendarCommand(id), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : NoContent();
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/holidays")]
[Authorize]
public sealed class HolidaysController : ControllerBase
{
    private readonly IMediator _mediator;
    public HolidaysController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? calendarId, [FromQuery] int? year, CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetHolidaysQuery(calendarId, year), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetHolidayByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateHolidayCommand(
            request.LeaveCalendarId, request.HolidayDate, request.Name, request.IsOptional), cancellationToken);
        return result.Succeeded ? CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHolidayRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateHolidayCommand(id, request.HolidayDate, request.Name, request.IsOptional), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.HolidaysManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteHolidayCommand(id), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : NoContent();
    }
}
