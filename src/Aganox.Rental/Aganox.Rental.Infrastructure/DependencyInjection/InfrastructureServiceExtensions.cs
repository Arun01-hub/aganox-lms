using Aganox.Rental.Application.Abstractions;
using Aganox.Rental.Infrastructure.Persistence;
using Aganox.Rental.Infrastructure.Repositories;
using Aganox.Rental.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Rental.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RentalDb")
            ?? throw new InvalidOperationException("Connection string 'RentalDb' is not configured.");

        services.AddDbContext<RentalDbContext>(options =>
        {
            if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(RentalDbContext).Assembly.FullName));
            }
            else
            {
                options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(RentalDbContext).Assembly.FullName));
            }
        });

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IRentalDbSeeder, RentalDbSeeder>();

        return services;
    }
}
