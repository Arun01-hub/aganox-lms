namespace Aganox.IdentityServer.Application.Models;

public sealed class ClientRegistrationDto
{
    public required string ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public required string DisplayName { get; init; }

    public required IReadOnlyList<string> RedirectUris { get; init; }

    public IReadOnlyList<string> PostLogoutRedirectUris { get; init; } = [];

    public required IReadOnlyList<string> Permissions { get; init; }
}
