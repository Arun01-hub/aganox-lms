using Aganox.Lms.Application.Features.Admin;
using Aganox.Lms.Application.Features.Admin.Dtos;
using Aganox.Lms.Application.Features.Navigation;
using Aganox.Lms.Application.Features.Navigation.Dtos;
using Aganox.Lms.Domain.Constants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("settings")]
    [Authorize(Policy = Permissions.AdminSettings)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetSettingsQuery(), cancellationToken));

    [HttpPut("settings")]
    [Authorize(Policy = Permissions.AdminSettings)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateSettingsCommand(request.Settings), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetPermissionsMatrixQuery(), cancellationToken));

    [HttpPut("roles/{roleId:guid}/permissions")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateRolePermissionsCommand(roleId, request.Permissions),
            cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = Permissions.AdminAudit)]
    public async Task<IActionResult> AuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(entityName, userId, from, to, page, pageSize),
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

    [HttpGet("activity-logs")]
    [Authorize(Policy = Permissions.AdminAudit)]
    public async Task<IActionResult> ActivityLogs(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetActivityLogsQuery(userId, from, to, page, pageSize),
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

    [HttpGet("backups")]
    [Authorize(Policy = Permissions.AdminBackup)]
    public async Task<IActionResult> BackupHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBackupHistoryQuery(page, pageSize), cancellationToken);
        return Ok(new
        {
            items = result.Items,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages
        });
    }

    [HttpPost("backup")]
    [Authorize(Policy = Permissions.AdminBackup)]
    public async Task<IActionResult> CreateBackup(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateBackupCommand(), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpPost("restore")]
    [Authorize(Policy = Permissions.AdminBackup)]
    public async Task<IActionResult> Restore([FromBody] RestoreBackupRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RestoreBackupCommand(request.BackupFilePath), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(new { message = "Restore completed." });
    }

    [HttpGet("navigation/menu")]
    [Authorize(Policy = Permissions.AdminSettings)]
    public async Task<IActionResult> GetNavigationMenu(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetAdminNavigationMenuQuery(), cancellationToken));

    [HttpPut("navigation/menu")]
    [Authorize(Policy = Permissions.AdminSettings)]
    public async Task<IActionResult> SaveNavigationMenu(
        [FromBody] SaveNavigationMenuRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SaveNavigationMenuCommand(request.Items), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }

    [HttpPost("navigation/menu/reset")]
    [Authorize(Policy = Permissions.AdminSettings)]
    public async Task<IActionResult> ResetNavigationMenu(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetNavigationMenuCommand(), cancellationToken);
        return !result.Succeeded ? BadRequest(new { errors = result.Errors }) : Ok(result.Data);
    }
}
