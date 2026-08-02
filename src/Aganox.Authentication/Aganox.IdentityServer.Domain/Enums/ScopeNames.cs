namespace Aganox.IdentityServer.Domain.Enums;

public static class ScopeNames
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";
    public const string AganoxApi = "aganox.api";

    public static readonly IReadOnlyList<string> All =
    [
        OpenId,
        Profile,
        Email,
        OfflineAccess,
        AganoxApi
    ];
}
