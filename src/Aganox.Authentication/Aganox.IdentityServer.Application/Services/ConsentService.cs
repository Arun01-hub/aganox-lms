namespace Aganox.IdentityServer.Application.Services;

public static class ConsentService
{
    public static IReadOnlyList<string> GetDisplayScopes(IEnumerable<string> scopes) =>
        scopes.Where(scope => !string.Equals(scope, "openid", StringComparison.OrdinalIgnoreCase))
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .OrderBy(scope => scope, StringComparer.OrdinalIgnoreCase)
              .ToList();

    public static bool IsValidScope(string scope) =>
        Domain.Enums.ScopeNames.All.Contains(scope, StringComparer.OrdinalIgnoreCase);
}
