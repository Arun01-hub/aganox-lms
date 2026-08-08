using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Lms.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=AganoxLms;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("AganoxLmsTests_v3");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
