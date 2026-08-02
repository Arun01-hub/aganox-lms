using Aganox.IdentityServer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Aganox.IdentityServer.Infrastructure.OpenIddict;

public static class OpenIddictServiceExtensions
{
    public static IServiceCollection AddOpenIddictServices(
        this IServiceCollection services,
        string? issuer)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AuthDbContext>();
            })
            .AddServer(options =>
            {
                if (!string.IsNullOrWhiteSpace(issuer))
                {
                    options.SetIssuer(new Uri(issuer));
                }

                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetEndSessionEndpointUris("/connect/logout");

                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .AllowClientCredentialsFlow()
                       .RequireProofKeyForCodeExchange();

                // Rental API validates tokens with JwtBearer, which expects signed JWTs (JWS), not encrypted JWE tokens.
                options.DisableAccessTokenEncryption();

                options.RegisterScopes(
                    Scopes.OpenId,
                    Scopes.Profile,
                    Scopes.Email,
                    Scopes.OfflineAccess,
                    "aganox.api");

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();
            });

        return services;
    }
}
