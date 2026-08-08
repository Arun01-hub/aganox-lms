using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Domain.Entities.Extensibility;
using Aganox.Lms.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Extensibility.CustomFields;

public sealed record CustomFieldDefinitionDto(
    Guid Id,
    string EntityScope,
    string FieldKey,
    string LabelEn,
    string LabelTa,
    string DataType,
    string? OptionsJson,
    bool IsRequired,
    int SortOrder,
    bool IsActive);

public sealed record CustomFieldValueDto(Guid DefinitionId, string FieldKey, string Value);

public sealed record GetCustomFieldDefinitionsQuery(CustomFieldEntityScope? Scope, bool ActiveOnly = true)
    : IRequest<IReadOnlyList<CustomFieldDefinitionDto>>;

public sealed record CreateCustomFieldDefinitionCommand(
    CustomFieldEntityScope EntityScope,
    string FieldKey,
    string LabelEn,
    string LabelTa,
    CustomFieldDataType DataType,
    string? OptionsJson,
    bool IsRequired,
    int SortOrder,
    bool IsActive) : IRequest<Result<CustomFieldDefinitionDto>>;

public sealed record UpdateCustomFieldDefinitionCommand(
    Guid Id,
    string LabelEn,
    string LabelTa,
    CustomFieldDataType DataType,
    string? OptionsJson,
    bool IsRequired,
    int SortOrder,
    bool IsActive) : IRequest<Result<CustomFieldDefinitionDto>>;

public sealed record GetCustomFieldValuesQuery(CustomFieldEntityScope Scope, Guid EntityId)
    : IRequest<IReadOnlyList<CustomFieldValueDto>>;

public sealed record SaveCustomFieldValuesCommand(
    CustomFieldEntityScope Scope,
    Guid EntityId,
    IReadOnlyList<CustomFieldValueDto> Values) : IRequest<Result>;

public sealed class CreateCustomFieldDefinitionCommandValidator : AbstractValidator<CreateCustomFieldDefinitionCommand>
{
    public CreateCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.FieldKey).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LabelEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LabelTa).NotEmpty().MaximumLength(100);
    }
}

public sealed class GetCustomFieldDefinitionsQueryHandler
    : IRequestHandler<GetCustomFieldDefinitionsQuery, IReadOnlyList<CustomFieldDefinitionDto>>
{
    private readonly IAppDbContext _db;
    public GetCustomFieldDefinitionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> Handle(
        GetCustomFieldDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.CustomFieldDefinitions.AsNoTracking();
        if (request.Scope.HasValue)
        {
            query = query.Where(x => x.EntityScope == request.Scope.Value);
        }

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var items = await query.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return items.Select(MapDefinition).ToList();
    }

    internal static CustomFieldDefinitionDto MapDefinition(CustomFieldDefinition x) => new(
        x.Id,
        x.EntityScope.ToString(),
        x.FieldKey,
        x.LabelEn,
        x.LabelTa,
        x.DataType.ToString(),
        x.OptionsJson,
        x.IsRequired,
        x.SortOrder,
        x.IsActive);
}

public sealed class CreateCustomFieldDefinitionCommandHandler
    : IRequestHandler<CreateCustomFieldDefinitionCommand, Result<CustomFieldDefinitionDto>>
{
    private readonly IAppDbContext _db;
    public CreateCustomFieldDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<CustomFieldDefinitionDto>> Handle(
        CreateCustomFieldDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var key = request.FieldKey.Trim();
        if (await _db.CustomFieldDefinitions.AnyAsync(
                x => x.EntityScope == request.EntityScope && x.FieldKey == key,
                cancellationToken))
        {
            return Result<CustomFieldDefinitionDto>.Failure("Field key already exists for this scope.");
        }

        var entity = new CustomFieldDefinition
        {
            EntityScope = request.EntityScope,
            FieldKey = key,
            LabelEn = request.LabelEn.Trim(),
            LabelTa = request.LabelTa.Trim(),
            DataType = request.DataType,
            OptionsJson = request.OptionsJson,
            IsRequired = request.IsRequired,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        _db.CustomFieldDefinitions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CustomFieldDefinitionDto>.Success(GetCustomFieldDefinitionsQueryHandler.MapDefinition(entity));
    }
}

public sealed class UpdateCustomFieldDefinitionCommandHandler
    : IRequestHandler<UpdateCustomFieldDefinitionCommand, Result<CustomFieldDefinitionDto>>
{
    private readonly IAppDbContext _db;
    public UpdateCustomFieldDefinitionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<CustomFieldDefinitionDto>> Handle(
        UpdateCustomFieldDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.CustomFieldDefinitions.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return Result<CustomFieldDefinitionDto>.Failure("Custom field definition not found.");
        }

        entity.LabelEn = request.LabelEn.Trim();
        entity.LabelTa = request.LabelTa.Trim();
        entity.DataType = request.DataType;
        entity.OptionsJson = request.OptionsJson;
        entity.IsRequired = request.IsRequired;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CustomFieldDefinitionDto>.Success(GetCustomFieldDefinitionsQueryHandler.MapDefinition(entity));
    }
}

public sealed class GetCustomFieldValuesQueryHandler
    : IRequestHandler<GetCustomFieldValuesQuery, IReadOnlyList<CustomFieldValueDto>>
{
    private readonly IAppDbContext _db;
    public GetCustomFieldValuesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomFieldValueDto>> Handle(
        GetCustomFieldValuesQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _db.CustomFieldDefinitions.AsNoTracking()
            .Where(x => x.EntityScope == request.Scope && x.IsActive)
            .ToListAsync(cancellationToken);

        var values = await _db.CustomFieldValues.AsNoTracking()
            .Where(x => x.EntityId == request.EntityId && definitions.Select(d => d.Id).Contains(x.DefinitionId))
            .ToListAsync(cancellationToken);

        return definitions.Select(def =>
        {
            var value = values.FirstOrDefault(v => v.DefinitionId == def.Id)?.Value ?? string.Empty;
            return new CustomFieldValueDto(def.Id, def.FieldKey, value);
        }).ToList();
    }
}

public sealed class SaveCustomFieldValuesCommandHandler : IRequestHandler<SaveCustomFieldValuesCommand, Result>
{
    private readonly IAppDbContext _db;
    public SaveCustomFieldValuesCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(SaveCustomFieldValuesCommand request, CancellationToken cancellationToken)
    {
        var definitions = await _db.CustomFieldDefinitions
            .Where(x => x.EntityScope == request.Scope && x.IsActive)
            .ToListAsync(cancellationToken);

        var existing = await _db.CustomFieldValues
            .Where(x => x.EntityId == request.EntityId)
            .ToListAsync(cancellationToken);

        foreach (var incoming in request.Values)
        {
            var def = definitions.FirstOrDefault(x => x.Id == incoming.DefinitionId || x.FieldKey == incoming.FieldKey);
            if (def is null)
            {
                continue;
            }

            var row = existing.FirstOrDefault(x => x.DefinitionId == def.Id);
            if (row is null)
            {
                _db.CustomFieldValues.Add(new CustomFieldValue
                {
                    DefinitionId = def.Id,
                    EntityId = request.EntityId,
                    Value = incoming.Value ?? string.Empty
                });
            }
            else
            {
                row.Value = incoming.Value ?? string.Empty;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
