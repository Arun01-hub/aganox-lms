using Aganox.IdentityServer.Application.DependencyInjection;
using Aganox.IdentityServer.Application.Abstractions;
using Aganox.IdentityServer.Infrastructure.DependencyInjection;
using Aganox.IdentityServer.Services;

namespace Aganox.IdentityServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServerHost(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews();
        services.AddRazorPages();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddScoped<IUserAccountService, UserAccountService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Domain.Constants.PolicyNames.RequireAuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());
        });

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
        services.AddCors(options =>
        {
            options.AddPolicy("SpaClients", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
