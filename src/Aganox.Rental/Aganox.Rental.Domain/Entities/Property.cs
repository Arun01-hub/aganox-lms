using Aganox.Rental.Domain.Enums;

namespace Aganox.Rental.Domain.Entities;

public sealed class Property : AuditableEntity
{
    public Guid Id { get; set; }

    public required string OwnerId { get; set; }

    public required string OwnerName { get; set; }

    public required string OwnerEmail { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public PropertyType Type { get; set; }

    public PropertyStatus Status { get; set; }

    public required string AddressLine1 { get; set; }

    public required string City { get; set; }

    public required string State { get; set; }

    public required string Zip { get; set; }

    public decimal RentMonthly { get; set; }

    public int Bedrooms { get; set; }

    public decimal Bathrooms { get; set; }

    public int Sqft { get; set; }

    /// <summary>JSON array of amenity labels.</summary>
    public required string AmenitiesJson { get; set; }

    public required string ImageUrl { get; set; }

    public DateOnly AvailableFrom { get; set; }
}
