using Aganox.Rental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Rental.Infrastructure.Persistence;

public sealed class RentalDbContext(DbContextOptions<RentalDbContext> options) : DbContext(options)
{
    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RentalDbContext).Assembly);
    }
}
