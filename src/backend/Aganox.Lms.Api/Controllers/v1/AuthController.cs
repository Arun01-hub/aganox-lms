using Aganox.Lms.Application.Features.Auth.Commands;
using Aganox.Lms.Application.Features.Auth.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aganox.Lms.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LoginCommand(request.Email, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return result.Succeeded ? Ok(result.Data) : Unauthorized(new { errors = result.Errors });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RefreshTokenCommand(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return result.Succeeded ? Ok(result.Data) : Unauthorized(new { errors = result.Errors });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new LogoutCommand(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword),
            cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
