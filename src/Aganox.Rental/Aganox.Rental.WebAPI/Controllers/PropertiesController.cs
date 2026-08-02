using Aganox.Rental.Application.Abstractions;
using Aganox.Rental.Application.Models;
using Aganox.Rental.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Rental.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> Search(
        [FromQuery] PropertySearchRequest request,
        CancellationToken cancellationToken)
    {
        var results = await propertyService.SearchAsync(request, cancellationToken);
        return Ok(results);
    }

    [HttpGet("cities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCities(CancellationToken cancellationToken)
    {
        var cities = await propertyService.GetCitiesAsync(cancellationToken);
        return Ok(cities);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetMine(CancellationToken cancellationToken)
    {
        var ownerId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Unauthorized();
        }

        var results = await propertyService.GetMineAsync(ownerId, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetByIdAsync(id, cancellationToken);
        return property is null ? NotFound() : Ok(property);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PropertyDto>> Create(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var ownerId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Unauthorized();
        }

        var created = await propertyService.CreateAsync(
            ownerId,
            User.GetUserName() ?? ownerId,
            User.GetUserEmail() ?? ownerId,
            request,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PropertyDto>> Update(
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var ownerId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Unauthorized();
        }

        var updated = await propertyService.UpdateAsync(id, ownerId, request, cancellationToken);
        if (updated is null)
        {
            var existing = await propertyService.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            return Forbid();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ownerId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Unauthorized();
        }

        var deleted = await propertyService.DeleteAsync(id, ownerId, cancellationToken);
        if (!deleted)
        {
            var existing = await propertyService.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            return Forbid();
        }

        return NoContent();
    }
}
