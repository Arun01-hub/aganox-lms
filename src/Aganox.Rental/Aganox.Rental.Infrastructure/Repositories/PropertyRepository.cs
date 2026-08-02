using Aganox.Rental.Application.Abstractions;
using Aganox.Rental.Domain.Entities;
using Aganox.Rental.Domain.Enums;
using Aganox.Rental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Rental.Infrastructure.Repositories;

public sealed class PropertyRepository(RentalDbContext dbContext) : IPropertyRepository
{
    public async Task<IReadOnlyList<Property>> SearchAsync(
        PropertySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Properties.AsNoTracking();

        if (criteria.AvailableOnly && criteria.Status is null)
        {
            query = query.Where(property => property.Status == PropertyStatus.Available);
        }
        else if (criteria.Status is not null)
        {
            query = query.Where(property => property.Status == criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            var city = criteria.City.Trim();
            query = query.Where(property => EF.Functions.Like(property.City, $"%{city}%"));
        }

        if (criteria.Type is not null)
        {
            query = query.Where(property => property.Type == criteria.Type);
        }

        if (criteria.MinBedrooms is not null)
        {
            query = query.Where(property => property.Bedrooms >= criteria.MinBedrooms);
        }

        if (criteria.MinRent is not null)
        {
            query = query.Where(property => property.RentMonthly >= criteria.MinRent);
        }

        if (criteria.MaxRent is not null)
        {
            query = query.Where(property => property.RentMonthly <= criteria.MaxRent);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var term = $"%{criteria.Query.Trim()}%";
            query = query.Where(property =>
                EF.Functions.Like(property.Title, term) ||
                EF.Functions.Like(property.Description, term) ||
                EF.Functions.Like(property.AddressLine1, term) ||
                EF.Functions.Like(property.City, term) ||
                EF.Functions.Like(property.AmenitiesJson, term));
        }

        return await query
            .OrderByDescending(property => property.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Properties.FirstOrDefaultAsync(property => property.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Property>> GetByOwnerIdAsync(
        string ownerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Properties
            .AsNoTracking()
            .Where(property => property.OwnerId == ownerId)
            .OrderByDescending(property => property.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctCitiesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Properties
            .AsNoTracking()
            .Select(property => property.City)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Property property, CancellationToken cancellationToken = default)
    {
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Property property, CancellationToken cancellationToken = default)
    {
        dbContext.Properties.Update(property);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Property property, CancellationToken cancellationToken = default)
    {
        dbContext.Properties.Remove(property);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Properties.AnyAsync(property => property.Id == id, cancellationToken);
}
