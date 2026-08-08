using Aganox.Lms.Application.Features.Extensibility.CustomFields;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/custom-fields")]
[Authorize(Policy = Permissions.PayrollConfigView)]
public sealed class CustomFieldsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomFieldsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions(
        [FromQuery] CustomFieldEntityScope? scope,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetCustomFieldDefinitionsQuery(scope), cancellationToken));

    [HttpPost("definitions")]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> CreateDefinition(
        [FromBody] CreateCustomFieldDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("definitions/{id:guid}")]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> UpdateDefinition(
        Guid id,
        [FromBody] UpdateCustomFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateCustomFieldDefinitionCommand(
            id, request.LabelEn, request.LabelTa, request.DataType, request.OptionsJson,
            request.IsRequired, request.SortOrder, request.IsActive), cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("values")]
    public async Task<IActionResult> GetValues(
        [FromQuery] CustomFieldEntityScope scope,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetCustomFieldValuesQuery(scope, entityId), cancellationToken));

    [HttpPut("values")]
    [Authorize(Policy = Permissions.PayrollConfigManage)]
    public async Task<IActionResult> SaveValues(
        [FromBody] SaveCustomFieldValuesCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}

public sealed record UpdateCustomFieldDefinitionRequest(
    string LabelEn,
    string LabelTa,
    CustomFieldDataType DataType,
    string? OptionsJson,
    bool IsRequired,
    int SortOrder,
    bool IsActive);
