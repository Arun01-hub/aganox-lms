using Aganox.IdentityServer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aganox.IdentityServer.Infrastructure.Persistence;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlite(
            "Data Source=aganox-identity.db",
            builder => builder.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));

        return new AuthDbContext(optionsBuilder.Options);
    }
}
