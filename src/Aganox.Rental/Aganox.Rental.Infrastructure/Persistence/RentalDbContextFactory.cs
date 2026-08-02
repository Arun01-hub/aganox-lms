using Aganox.Rental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aganox.Rental.Infrastructure.Persistence;

public sealed class RentalDbContextFactory : IDesignTimeDbContextFactory<RentalDbContext>
{
    public RentalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RentalDbContext>();
        optionsBuilder.UseSqlite(
            "Data Source=aganox-rental.db",
            builder => builder.MigrationsAssembly(typeof(RentalDbContext).Assembly.FullName));

        return new RentalDbContext(optionsBuilder.Options);
    }
}
