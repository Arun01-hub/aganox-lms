using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aganox.Lms.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace Aganox.Lms.Infrastructure.Auth;

/// <summary>
/// Maps external OIDC identities to LMS users and adds role/permission claims from the local store.
/// </summary>
public sealed class LmsUserClaimsTransformation(IIdentityService identityService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        if (principal.HasClaim(c => c.Type == "permission"))
        {
            return principal;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email))
        {
            return principal;
        }

        var user = await identityService.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return principal;
        }

        var roles = await identityService.GetRolesAsync(user.Id);
        var permissions = await identityService.GetPermissionsAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("email", user.Email)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        return new ClaimsPrincipal(new ClaimsIdentity(
            principal.Claims.Concat(claims),
            principal.Identity.AuthenticationType));
    }
}
