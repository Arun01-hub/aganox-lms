using System.ComponentModel.DataAnnotations;
using Aganox.Rental.Domain.Enums;

namespace Aganox.Rental.Application.Models;

public sealed record PropertyAddressDto(
    string Line1,
    string City,
    string State,
    string Zip);

public sealed record PropertyDto(
    Guid Id,
    string OwnerId,
    string OwnerName,
    string OwnerEmail,
    string Title,
    string Description,
    PropertyType Type,
    PropertyStatus Status,
    PropertyAddressDto Address,
    decimal RentMonthly,
    int Bedrooms,
    decimal Bathrooms,
    int Sqft,
    IReadOnlyList<string> Amenities,
    string ImageUrl,
    DateOnly AvailableFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class PropertySearchRequest
{
    public string? Query { get; init; }

    public string? City { get; init; }

    public PropertyType? Type { get; init; }

    public decimal? MinRent { get; init; }

    public decimal? MaxRent { get; init; }

    public int? MinBedrooms { get; init; }

    public PropertyStatus? Status { get; init; }

    public bool AvailableOnly { get; init; } = true;
}

public sealed class CreatePropertyRequest
{
    [Required, MinLength(5)]
    public string Title { get; init; } = string.Empty;

    [Required, MinLength(20)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public PropertyType Type { get; init; }

    [Required]
    public PropertyStatus Status { get; init; }

    [Required]
    public PropertyAddressDto Address { get; init; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    [Range(0, double.MaxValue)]
    public decimal RentMonthly { get; init; }

    [Range(0, 20)]
    public int Bedrooms { get; init; }

    [Range(0, 20)]
    public decimal Bathrooms { get; init; }

    [Range(1, 100000)]
    public int Sqft { get; init; }

    public IReadOnlyList<string> Amenities { get; init; } = [];

    [Required, Url]
    public string ImageUrl { get; init; } = string.Empty;

    [Required]
    public DateOnly AvailableFrom { get; init; }
}

public sealed class UpdatePropertyRequest
{
    [Required, MinLength(5)]
    public string Title { get; init; } = string.Empty;

    [Required, MinLength(20)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public PropertyType Type { get; init; }

    [Required]
    public PropertyStatus Status { get; init; }

    [Required]
    public PropertyAddressDto Address { get; init; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    [Range(0, double.MaxValue)]
    public decimal RentMonthly { get; init; }

    [Range(0, 20)]
    public int Bedrooms { get; init; }

    [Range(0, 20)]
    public decimal Bathrooms { get; init; }

    [Range(1, 100000)]
    public int Sqft { get; init; }

    public IReadOnlyList<string> Amenities { get; init; } = [];

    [Required, Url]
    public string ImageUrl { get; init; } = string.Empty;

    [Required]
    public DateOnly AvailableFrom { get; init; }
}
