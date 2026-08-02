using Aganox.Rental.Application.Models;

namespace Aganox.Rental.Application.Abstractions;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertyDto>> SearchAsync(PropertySearchRequest request, CancellationToken cancellationToken = default);

    Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyDto>> GetMineAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default);

    Task<PropertyDto> CreateAsync(string ownerId, string ownerName, string ownerEmail, CreatePropertyRequest request, CancellationToken cancellationToken = default);

    Task<PropertyDto?> UpdateAsync(Guid id, string ownerId, UpdatePropertyRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, string ownerId, CancellationToken cancellationToken = default);
}
