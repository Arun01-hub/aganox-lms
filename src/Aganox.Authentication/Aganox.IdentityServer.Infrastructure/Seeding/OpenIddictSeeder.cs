using Aganox.IdentityServer.Application.Abstractions;
using Aganox.IdentityServer.Application.Models;
using Aganox.IdentityServer.Domain.Entities;
using Aganox.IdentityServer.Domain.Enums;
using Aganox.IdentityServer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Aganox.IdentityServer.Infrastructure.Seeding;

public sealed class OpenIddictSeeder(
    IServiceProvider serviceProvider,
    ILogger<OpenIddictSeeder> logger) : IDbSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        await SeedRolesAsync(scope.ServiceProvider, cancellationToken);
        await SeedUsersAsync(scope.ServiceProvider, cancellationToken);
        await SeedScopesAsync(scope.ServiceProvider, cancellationToken);
        await SeedClientsAsync(scope.ServiceProvider, cancellationToken);

        logger.LogInformation("Identity database seeding completed.");
    }

    private static async Task SeedRolesAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }
    }

    private static async Task SeedUsersAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        const string email = "admin@aganox.local";
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator"
        };

        var result = await userManager.CreateAsync(user, "ChangeMe123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create seed user: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }

        await userManager.AddToRoleAsync(user, "User");
    }

    private static async Task SeedScopesAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var scopeManager = provider.GetRequiredService<IOpenIddictScopeManager>();

        if (await scopeManager.FindByNameAsync("aganox.api") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "aganox.api",
                DisplayName = "Aganox API",
                Resources = { "aganox.api" }
            }, cancellationToken);
        }
    }

    private static async Task SeedClientsAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var registrationService = provider.GetRequiredService<IClientRegistrationService>();
        await registrationService.EnsureDevelopmentClientsAsync(cancellationToken);
    }
}

public sealed class ClientRegistrationService(
    IOpenIddictApplicationManager applicationManager) : IClientRegistrationService
{
    public async Task EnsureDevelopmentClientsAsync(CancellationToken cancellationToken = default)
    {
        await RegisterClientAsync(new ClientRegistrationDto
        {
            ClientId = "aganox-web",
            DisplayName = "Aganox Web",
            RedirectUris = ["https://localhost:7036/signin-oidc", "http://localhost:5110/signin-oidc"],
            Permissions =
            [
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + ScopeNames.OfflineAccess,
                Permissions.Prefixes.Scope + ScopeNames.AganoxApi
            ]
        }, cancellationToken);

        await RegisterClientAsync(new ClientRegistrationDto
        {
            ClientId = "aganox-service",
            ClientSecret = "dev-service-secret-change-in-production",
            DisplayName = "Aganox Service",
            RedirectUris = [],
            Permissions =
            [
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + ScopeNames.AganoxApi
            ]
        }, cancellationToken);

        await RegisterClientAsync(new ClientRegistrationDto
        {
            ClientId = "aganox-rental",
            DisplayName = "Aganox Rental",
            RedirectUris =
            [
                "http://localhost:4200/",
                "http://localhost:4200",
                "http://rental.aganox.local:4200/",
                "http://rental.aganox.local:4200"
            ],
            PostLogoutRedirectUris =
            [
                "http://localhost:4200/",
                "http://localhost:4200",
                "http://rental.aganox.local:4200/",
                "http://rental.aganox.local:4200"
            ],
            Permissions =
            [
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + ScopeNames.OfflineAccess,
                Permissions.Prefixes.Scope + ScopeNames.AganoxApi
            ]
        }, cancellationToken);

        await RegisterClientAsync(new ClientRegistrationDto
        {
            ClientId = "aganox-rental-service",
            ClientSecret = "dev-rental-service-secret-change-in-production",
            DisplayName = "Aganox Rental Service",
            RedirectUris = [],
            Permissions =
            [
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + ScopeNames.AganoxApi
            ]
        }, cancellationToken);
    }

    public async Task RegisterClientAsync(ClientRegistrationDto client, CancellationToken cancellationToken = default)
    {
        if (await applicationManager.FindByClientIdAsync(client.ClientId, cancellationToken) is { } existing)
        {
            var existingDescriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(existingDescriptor, existing, cancellationToken);

            var updated = MergeUris(existingDescriptor.RedirectUris, client.RedirectUris)
                | MergeUris(existingDescriptor.PostLogoutRedirectUris, client.PostLogoutRedirectUris);

            if (updated)
            {
                await applicationManager.UpdateAsync(existing, existingDescriptor, cancellationToken);
            }

            return;
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            ClientType = string.IsNullOrEmpty(client.ClientSecret)
                ? ClientTypes.Public
                : ClientTypes.Confidential,
            ConsentType = ConsentTypes.Explicit
        };

        if (!string.IsNullOrEmpty(client.ClientSecret))
        {
            descriptor.ClientSecret = client.ClientSecret;
        }

        foreach (var redirectUri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        foreach (var postLogoutRedirectUri in client.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(postLogoutRedirectUri));
        }

        foreach (var permission in client.Permissions)
        {
            descriptor.Permissions.Add(permission);
        }

        await applicationManager.CreateAsync(descriptor, cancellationToken);
    }

    private static bool MergeUris(ICollection<Uri> target, IReadOnlyList<string> source)
    {
        var updated = false;

        foreach (var uri in source.Select(static value => new Uri(value)))
        {
            if (target.Contains(uri))
            {
                continue;
            }

            target.Add(uri);
            updated = true;
        }

        return updated;
    }
}
