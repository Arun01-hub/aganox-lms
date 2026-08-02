namespace Aganox.IdentityServer.Application.Models;

public sealed class ConsentRequest
{
    public required string ApplicationName { get; init; }

    public required IReadOnlyList<string> RequestedScopes { get; init; }

    public string? ReturnUrl { get; init; }
}
