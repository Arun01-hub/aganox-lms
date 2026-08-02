using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Auth.Dtos;
using MediatR;

namespace Aganox.Lms.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password, string? IpAddress) : IRequest<Result<AuthResponseDto>>;

public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<Result<AuthResponseDto>>;

public sealed record LogoutCommand(string RefreshToken, string? IpAddress) : IRequest<Result>;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result>;
