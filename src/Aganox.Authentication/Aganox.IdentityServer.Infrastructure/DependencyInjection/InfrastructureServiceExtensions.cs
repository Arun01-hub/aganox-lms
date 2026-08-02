using Aganox.IdentityServer.Application.Abstractions;
using Aganox.IdentityServer.Infrastructure.Identity;
using Aganox.IdentityServer.Infrastructure.OpenIddict;
using Aganox.IdentityServer.Infrastructure.Persistence;
using Aganox.IdentityServer.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.IdentityServer.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");

        services.AddDbContext<AuthDbContext>(options =>
        {
            if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));
            }
            else
            {
                options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));
            }

            options.UseOpenIddict();
        });

        services.AddIdentityServices();
        services.AddOpenIddictServices(configuration["Identity:Issuer"]);

        services.AddScoped<IClientRegistrationService, ClientRegistrationService>();
        services.AddScoped<IDbSeeder, OpenIddictSeeder>();

        return services;
    }
}
