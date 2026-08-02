namespace Aganox.Rental.Infrastructure.Seeding;

public interface IRentalDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
