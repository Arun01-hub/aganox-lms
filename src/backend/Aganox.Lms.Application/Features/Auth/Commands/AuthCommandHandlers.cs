using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Auth.Dtos;
using Aganox.Lms.Domain.Entities.System;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Auth.Commands;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public LoginCommandHandler(
        IIdentityService identity,
        IJwtTokenService jwt,
        IAppDbContext db,
        IDateTimeProvider clock)
    {
        _identity = identity;
        _jwt = jwt;
        _db = db;
        _clock = clock;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identity.FindByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null)
        {
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        if (!await _identity.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        return Result<AuthResponseDto>.Success(await IssueTokensAsync(user, request.IpAddress, cancellationToken));
    }

    private async Task<AuthResponseDto> IssueTokensAsync(AuthUserDto user, string? ip, CancellationToken cancellationToken)
    {
        var roles = await _identity.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await _identity.GetPermissionsAsync(user.Id, cancellationToken);
        var accessToken = _jwt.CreateAccessToken(user.Id, user.Email, roles, permissions, out var expiresAt);
        var refreshTokenValue = _jwt.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _clock.UtcNow.AddDays(7),
            CreatedAt = _clock.UtcNow,
            CreatedByIp = ip
        });
        await _db.SaveChangesAsync(cancellationToken);

        var expiresIn = (int)(expiresAt - _clock.UtcNow).TotalSeconds;
        return new AuthResponseDto(
            accessToken,
            expiresIn,
            refreshTokenValue,
            new CurrentUserDto(
                user.Id,
                user.Email,
                user.UserName,
                user.EmployeeId,
                user.MustChangePassword,
                roles,
                permissions));
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public RefreshTokenCommandHandler(
        IIdentityService identity,
        IJwtTokenService jwt,
        IAppDbContext db,
        IDateTimeProvider clock)
    {
        _identity = identity;
        _jwt = jwt;
        _db = db;
        _clock = clock;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.");
        }

        var user = await _identity.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AuthResponseDto>.Failure("User not found.");
        }

        existing.RevokedAt = _clock.UtcNow;
        existing.RevokedByIp = request.IpAddress;

        var roles = await _identity.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await _identity.GetPermissionsAsync(user.Id, cancellationToken);
        var accessToken = _jwt.CreateAccessToken(user.Id, user.Email, roles, permissions, out var expiresAt);
        var newRefresh = _jwt.CreateRefreshToken();
        existing.ReplacedByToken = newRefresh;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefresh,
            ExpiresAt = _clock.UtcNow.AddDays(7),
            CreatedAt = _clock.UtcNow,
            CreatedByIp = request.IpAddress
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            accessToken,
            (int)(expiresAt - _clock.UtcNow).TotalSeconds,
            newRefresh,
            new CurrentUserDto(
                user.Id,
                user.Email,
                user.UserName,
                user.EmployeeId,
                user.MustChangePassword,
                roles,
                permissions)));
    }
}

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public LogoutCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.RevokedAt = _clock.UtcNow;
            token.RevokedByIp = request.IpAddress;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(IIdentityService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure("Not authenticated.");
        }

        var (succeeded, errors) = await _identity.ChangePasswordAsync(
            _currentUser.UserId.Value,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return succeeded ? Result.Success() : Result.Failure(errors);
    }
}

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identity;
    private readonly IEmailSender _email;

    public ForgotPasswordCommandHandler(IIdentityService identity, IEmailSender email)
    {
        _identity = identity;
        _email = email;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, token, _) = await _identity.GeneratePasswordResetTokenAsync(request.Email.Trim(), cancellationToken);

        // Always return success to avoid account enumeration.
        if (succeeded && token is not null)
        {
            await _email.SendAsync(
                request.Email.Trim(),
                "Aganox LMS password reset",
                $"Use this token to reset your password: {token}",
                cancellationToken);
        }

        return Result.Success();
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identity;

    public ResetPasswordCommandHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, errors) = await _identity.ResetPasswordAsync(
            request.Email.Trim(),
            request.Token,
            request.NewPassword,
            cancellationToken);

        return succeeded ? Result.Success() : Result.Failure(errors);
    }
}
