namespace Aganox.IdentityServer.Infrastructure.Seeding;

public interface IDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
