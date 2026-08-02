using Aganox.IdentityServer.Application.Models;

namespace Aganox.IdentityServer.Application.Abstractions;

public sealed class LoginResult
{
    public bool Succeeded { get; init; }

    public bool IsLockedOut { get; init; }

    public bool RequiresTwoFactor { get; init; }

    public static LoginResult Success() => new() { Succeeded = true };

    public static LoginResult Failure(bool isLockedOut = false, bool requiresTwoFactor = false) =>
        new() { Succeeded = false, IsLockedOut = isLockedOut, RequiresTwoFactor = requiresTwoFactor };
}

public interface IUserAccountService
{
    Task<LoginResult> SignInAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
