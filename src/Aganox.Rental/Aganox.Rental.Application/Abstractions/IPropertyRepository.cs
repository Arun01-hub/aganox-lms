using Aganox.Rental.Domain.Entities;
using Aganox.Rental.Domain.Enums;

namespace Aganox.Rental.Application.Abstractions;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> SearchAsync(PropertySearchCriteria criteria, CancellationToken cancellationToken = default);

    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Property>> GetByOwnerIdAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctCitiesAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Property property, CancellationToken cancellationToken = default);

    Task UpdateAsync(Property property, CancellationToken cancellationToken = default);

    Task DeleteAsync(Property property, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record PropertySearchCriteria(
    string? Query,
    string? City,
    PropertyType? Type,
    decimal? MinRent,
    decimal? MaxRent,
    int? MinBedrooms,
    PropertyStatus? Status,
    bool AvailableOnly = true);
