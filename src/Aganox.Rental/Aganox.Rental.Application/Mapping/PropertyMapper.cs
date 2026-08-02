using System.Text.Json;
using Aganox.Rental.Application.Models;
using Aganox.Rental.Domain.Entities;

namespace Aganox.Rental.Application.Mapping;

public static class PropertyMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static PropertyDto ToDto(Property entity) =>
        new(
            entity.Id,
            entity.OwnerId,
            entity.OwnerName,
            entity.OwnerEmail,
            entity.Title,
            entity.Description,
            entity.Type,
            entity.Status,
            new PropertyAddressDto(entity.AddressLine1, entity.City, entity.State, entity.Zip),
            entity.RentMonthly,
            entity.Bedrooms,
            entity.Bathrooms,
            entity.Sqft,
            DeserializeAmenities(entity.AmenitiesJson),
            entity.ImageUrl,
            entity.AvailableFrom,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static Property ToEntity(CreatePropertyRequest request, string ownerId, string ownerName, string ownerEmail)
    {
        var now = DateTimeOffset.UtcNow;
        return new Property
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            OwnerName = ownerName,
            OwnerEmail = ownerEmail,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Type = request.Type,
            Status = request.Status,
            AddressLine1 = request.Address.Line1.Trim(),
            City = request.Address.City.Trim(),
            State = request.Address.State.Trim().ToUpperInvariant(),
            Zip = request.Address.Zip.Trim(),
            RentMonthly = request.RentMonthly,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            Sqft = request.Sqft,
            AmenitiesJson = SerializeAmenities(request.Amenities),
            ImageUrl = request.ImageUrl.Trim(),
            AvailableFrom = request.AvailableFrom,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static void ApplyUpdate(Property entity, UpdatePropertyRequest request)
    {
        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.Type = request.Type;
        entity.Status = request.Status;
        entity.AddressLine1 = request.Address.Line1.Trim();
        entity.City = request.Address.City.Trim();
        entity.State = request.Address.State.Trim().ToUpperInvariant();
        entity.Zip = request.Address.Zip.Trim();
        entity.RentMonthly = request.RentMonthly;
        entity.Bedrooms = request.Bedrooms;
        entity.Bathrooms = request.Bathrooms;
        entity.Sqft = request.Sqft;
        entity.AmenitiesJson = SerializeAmenities(request.Amenities);
        entity.ImageUrl = request.ImageUrl.Trim();
        entity.AvailableFrom = request.AvailableFrom;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static string SerializeAmenities(IReadOnlyList<string> amenities) =>
        JsonSerializer.Serialize(amenities, JsonOptions);

    public static IReadOnlyList<string> DeserializeAmenities(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }
}
