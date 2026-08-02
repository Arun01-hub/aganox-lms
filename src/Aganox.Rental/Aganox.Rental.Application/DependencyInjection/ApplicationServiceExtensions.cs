using Aganox.Rental.Application.Abstractions;
using Aganox.Rental.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Rental.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        return services;
    }
}
