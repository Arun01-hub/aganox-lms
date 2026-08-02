using Aganox.Rental.Application.Mapping;
using Aganox.Rental.Domain.Entities;
using Aganox.Rental.Domain.Enums;
using Aganox.Rental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aganox.Rental.Infrastructure.Seeding;

public sealed class RentalDbSeeder(
    RentalDbContext dbContext,
    ILogger<RentalDbSeeder> logger) : IRentalDbSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Properties.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var properties = CreateSeedProperties(now);
        dbContext.Properties.AddRange(properties);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} rental properties.", properties.Count);
    }

    private static List<Property> CreateSeedProperties(DateTimeOffset timestamp)
    {
        Property Create(
            Guid id,
            string ownerId,
            string ownerName,
            string ownerEmail,
            string title,
            string description,
            PropertyType type,
            PropertyStatus status,
            string line1,
            string city,
            string state,
            string zip,
            decimal rent,
            int beds,
            decimal baths,
            int sqft,
            string[] amenities,
            string imageUrl,
            DateOnly availableFrom) =>
            new()
            {
                Id = id,
                OwnerId = ownerId,
                OwnerName = ownerName,
                OwnerEmail = ownerEmail,
                Title = title,
                Description = description,
                Type = type,
                Status = status,
                AddressLine1 = line1,
                City = city,
                State = state,
                Zip = zip,
                RentMonthly = rent,
                Bedrooms = beds,
                Bathrooms = baths,
                Sqft = sqft,
                AmenitiesJson = PropertyMapper.SerializeAmenities(amenities),
                ImageUrl = imageUrl,
                AvailableFrom = availableFrom,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

        return
        [
            Create(
                Guid.Parse("11111111-1111-1111-1111-111111111101"),
                "seed-owner-1", "Sarah Chen", "sarah@example.com",
                "Sunlit Downtown Loft",
                "Bright open-plan loft with floor-to-ceiling windows, polished concrete floors, and a chef's kitchen.",
                PropertyType.Apartment, PropertyStatus.Available,
                "412 Market Street, Unit 8B", "Portland", "OR", "97204",
                2450, 2, 2, 1180,
                ["In-unit laundry", "Air conditioning", "Elevator", "Parking"],
                "https://images.unsplash.com/photo-1502672260266-1c1ef2cd9361?w=800&q=80",
                new DateOnly(2026, 8, 1)),
            Create(
                Guid.Parse("11111111-1111-1111-1111-111111111102"),
                "seed-owner-2", "Marcus Rivera", "marcus@example.com",
                "Family Home with Garden",
                "Quiet cul-de-sac home with fenced backyard, updated appliances, and a finished basement.",
                PropertyType.House, PropertyStatus.Available,
                "88 Cedar Lane", "Austin", "TX", "78745",
                3200, 4, 3, 2200,
                ["Parking", "Pet friendly", "Air conditioning", "Balcony"],
                "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=800&q=80",
                new DateOnly(2026, 7, 15)),
            Create(
                Guid.Parse("11111111-1111-1111-1111-111111111103"),
                "seed-owner-1", "Sarah Chen", "sarah@example.com",
                "Compact Studio near Campus",
                "Efficient studio layout with built-in storage, high-speed fiber ready, and bike storage.",
                PropertyType.Studio, PropertyStatus.Available,
                "19 University Ave", "Portland", "OR", "97201",
                1350, 0, 1, 480,
                ["Furnished", "Utilities included", "Elevator"],
                "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&q=80",
                new DateOnly(2026, 7, 1)),
            Create(
                Guid.Parse("11111111-1111-1111-1111-111111111104"),
                "seed-owner-3", "Aisha Patel", "aisha@example.com",
                "Waterfront Condo with Pool",
                "Corner unit with bay views, resort-style amenities, and concierge service.",
                PropertyType.Condo, PropertyStatus.Available,
                "500 Harbor Drive, #1204", "Miami", "FL", "33131",
                4100, 2, 2, 1350,
                ["Pool", "Gym", "Parking", "Air conditioning", "Elevator"],
                "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=800&q=80",
                new DateOnly(2026, 9, 1)),
            Create(
                Guid.Parse("11111111-1111-1111-1111-111111111105"),
                "seed-owner-2", "Marcus Rivera", "marcus@example.com",
                "Modern Townhouse in East Austin",
                "Three-story townhouse with rooftop deck, smart thermostat, and community dog park.",
                PropertyType.Townhouse, PropertyStatus.Rented,
                "2200 E 6th Street", "Austin", "TX", "78702",
                2800, 3, 2.5m, 1750,
                ["Pet friendly", "Parking", "Balcony", "In-unit laundry"],
                "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800&q=80",
                new DateOnly(2026, 11, 1)),
            Create(
                Guid.Parse("11111111-1111-1111-1111-111111111106"),
                "seed-owner-3", "Aisha Patel", "aisha@example.com",
                "Charming Bungalow",
                "Classic bungalow with front porch, hardwood floors, and a detached garage.",
                PropertyType.House, PropertyStatus.Available,
                "742 Maple Street", "Denver", "CO", "80205",
                2650, 3, 2, 1600,
                ["Parking", "Pet friendly", "Air conditioning"],
                "https://images.unsplash.com/photo-1605276374101-de9d8785336f?w=800&q=80",
                new DateOnly(2026, 8, 15))
        ];
    }
}
