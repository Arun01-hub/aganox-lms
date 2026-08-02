using Microsoft.Extensions.DependencyInjection;

namespace Aganox.IdentityServer.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services;
        return services;
    }
}
