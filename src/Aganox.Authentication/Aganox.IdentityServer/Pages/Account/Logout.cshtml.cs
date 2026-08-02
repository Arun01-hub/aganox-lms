using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aganox.IdentityServer.Pages.Account;

public sealed class LogoutModel(IConfiguration configuration) : PageModel
{
    public string? PostLogoutRedirectUri { get; private set; }

    public string ReturnToApplicationUrl { get; private set; } = "/";

    public bool RequiresOidcSignOut { get; private set; }

    public void OnGet()
    {
        PostLogoutRedirectUri = Request.Query["post_logout_redirect_uri"].FirstOrDefault();
        RequiresOidcSignOut = PostLogoutRedirectUri is not null ||
                              Request.Query.ContainsKey("id_token_hint") ||
                              Request.Query.ContainsKey("client_id");

        ReturnToApplicationUrl =
            PostLogoutRedirectUri ??
            configuration["Identity:DefaultClientUrl"] ??
            "/";
    }
}
