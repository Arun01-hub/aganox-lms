using Aganox.IdentityServer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace Aganox.IdentityServer.Pages.Consent;

[Authorize]
public sealed class IndexModel(IOpenIddictApplicationManager applicationManager) : PageModel
{
    public string ApplicationName { get; private set; } = "Application";

    public IReadOnlyList<string> Scopes { get; private set; } = [];

    public async Task OnGetAsync(string? scope = null)
    {
        var clientId = Request.Query["client_id"].ToString();
        if (!string.IsNullOrEmpty(clientId))
        {
            var application = await applicationManager.FindByClientIdAsync(clientId);
            if (application is not null)
            {
                ApplicationName = (await applicationManager.GetDisplayNameAsync(application)) ?? clientId;
            }
            else
            {
                ApplicationName = clientId;
            }
        }

        Scopes = ConsentService.GetDisplayScopes((scope ?? Request.Query["scope"].ToString())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
