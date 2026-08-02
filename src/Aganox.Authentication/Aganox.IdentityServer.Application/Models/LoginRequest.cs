namespace Aganox.IdentityServer.Application.Models;

public sealed class LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public bool RememberMe { get; init; }

    public string? ReturnUrl { get; init; }
}
