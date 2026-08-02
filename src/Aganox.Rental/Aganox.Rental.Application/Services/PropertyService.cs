using Aganox.Rental.Application.Abstractions;
using Aganox.Rental.Application.Mapping;
using Aganox.Rental.Application.Models;

namespace Aganox.Rental.Application.Services;

public sealed class PropertyService(IPropertyRepository repository) : IPropertyService
{
    public async Task<IReadOnlyList<PropertyDto>> SearchAsync(
        PropertySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var criteria = new PropertySearchCriteria(
            request.Query,
            request.City,
            request.Type,
            request.MinRent,
            request.MaxRent,
            request.MinBedrooms,
            request.Status,
            request.AvailableOnly);

        var properties = await repository.SearchAsync(criteria, cancellationToken);
        return properties.Select(PropertyMapper.ToDto).ToList();
    }

    public async Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await repository.GetByIdAsync(id, cancellationToken);
        return property is null ? null : PropertyMapper.ToDto(property);
    }

    public async Task<IReadOnlyList<PropertyDto>> GetMineAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var properties = await repository.GetByOwnerIdAsync(ownerId, cancellationToken);
        return properties.Select(PropertyMapper.ToDto).ToList();
    }

    public Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default) =>
        repository.GetDistinctCitiesAsync(cancellationToken);

    public async Task<PropertyDto> CreateAsync(
        string ownerId,
        string ownerName,
        string ownerEmail,
        CreatePropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = PropertyMapper.ToEntity(request, ownerId, ownerName, ownerEmail);
        await repository.AddAsync(entity, cancellationToken);
        return PropertyMapper.ToDto(entity);
    }

    public async Task<PropertyDto?> UpdateAsync(
        Guid id,
        string ownerId,
        UpdatePropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !string.Equals(entity.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return null;
        }

        PropertyMapper.ApplyUpdate(entity, request);
        await repository.UpdateAsync(entity, cancellationToken);
        return PropertyMapper.ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, string ownerId, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !string.Equals(entity.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return false;
        }

        await repository.DeleteAsync(entity, cancellationToken);
        return true;
    }
}
