using Aganox.Rental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aganox.Rental.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    private static readonly ValueConverter<DateTimeOffset, DateTime> DateTimeOffsetConverter = new(
        offset => offset.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(property => property.Id);

        builder.Property(property => property.OwnerId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(property => property.OwnerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(property => property.OwnerEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(property => property.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(property => property.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(property => property.AddressLine1)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(property => property.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(property => property.State)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(property => property.Zip)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(property => property.RentMonthly)
            .HasPrecision(18, 2);

        builder.Property(property => property.Bathrooms)
            .HasPrecision(4, 1);

        builder.Property(property => property.AmenitiesJson)
            .HasColumnName("Amenities")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(property => property.ImageUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(property => property.CreatedAt)
            .HasConversion(DateTimeOffsetConverter);

        builder.Property(property => property.UpdatedAt)
            .HasConversion(DateTimeOffsetConverter);

        builder.HasIndex(property => property.OwnerId);
        builder.HasIndex(property => property.City);
        builder.HasIndex(property => property.Status);
        builder.HasIndex(property => property.UpdatedAt);
        builder.HasIndex(property => new { property.Status, property.City, property.RentMonthly });
    }
}
