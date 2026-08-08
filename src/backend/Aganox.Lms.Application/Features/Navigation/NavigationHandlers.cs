using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Navigation.Dtos;
using Aganox.Lms.Domain.Entities.System;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Navigation;

public sealed record GetNavigationMenuQuery : IRequest<IReadOnlyList<NavigationMenuItemDto>>;

public sealed record GetAdminNavigationMenuQuery : IRequest<IReadOnlyList<NavigationMenuItemAdminDto>>;

public sealed record SaveNavigationMenuCommand(IReadOnlyList<SaveNavigationMenuItemDto> Items)
    : IRequest<Result<IReadOnlyList<NavigationMenuItemAdminDto>>>;

public sealed record ResetNavigationMenuCommand : IRequest<Result<IReadOnlyList<NavigationMenuItemAdminDto>>>;

public sealed class SaveNavigationMenuCommandValidator : AbstractValidator<SaveNavigationMenuCommand>
{
    public SaveNavigationMenuCommandValidator()
    {
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            item.RuleFor(x => x.LabelKey).NotEmpty().MaximumLength(100);
            item.RuleFor(x => x.Route).MaximumLength(200);
            item.RuleFor(x => x.Icon).MaximumLength(50);
            item.RuleFor(x => x.RequiredPermissions).MaximumLength(500);
        });
    }
}

public sealed class GetNavigationMenuQueryHandler : IRequestHandler<GetNavigationMenuQuery, IReadOnlyList<NavigationMenuItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNavigationMenuQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NavigationMenuItemDto>> Handle(
        GetNavigationMenuQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _db.NavigationMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        var tree = NavigationMenuTreeBuilder.BuildVisibleTree(items, _currentUser.Permissions);
        return tree.Select(Map).ToList();
    }

    private static NavigationMenuItemDto Map(NavigationMenuItemNode node) => new(
        node.Id,
        node.Code,
        node.LabelKey,
        node.Route,
        node.Icon,
        node.Children.Select(Map).ToList());
}

public sealed class GetAdminNavigationMenuQueryHandler
    : IRequestHandler<GetAdminNavigationMenuQuery, IReadOnlyList<NavigationMenuItemAdminDto>>
{
    private readonly IAppDbContext _db;

    public GetAdminNavigationMenuQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<NavigationMenuItemAdminDto>> Handle(
        GetAdminNavigationMenuQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _db.NavigationMenuItems.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.LabelKey)
            .ToListAsync(cancellationToken);

        return items.Select(NavigationMenuMapper.ToAdminDto).ToList();
    }
}

public sealed class SaveNavigationMenuCommandHandler
    : IRequestHandler<SaveNavigationMenuCommand, Result<IReadOnlyList<NavigationMenuItemAdminDto>>>
{
    private readonly IAppDbContext _db;

    public SaveNavigationMenuCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<NavigationMenuItemAdminDto>>> Handle(
        SaveNavigationMenuCommand request,
        CancellationToken cancellationToken)
    {
        var items = request.Items.ToList();
        var codes = items.Select(x => x.Code.Trim()).ToList();
        if (codes.Count != codes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return Result<IReadOnlyList<NavigationMenuItemAdminDto>>.Failure("Menu item codes must be unique.");
        }

        var existing = await _db.NavigationMenuItems.ToListAsync(cancellationToken);
        var byId = existing.ToDictionary(x => x.Id);
        var byCode = existing.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var mapped = new List<(SaveNavigationMenuItemDto Dto, NavigationMenuItem Entity)>();

        foreach (var dto in items)
        {
            NavigationMenuItem entity;
            if (dto.Id.HasValue && byId.TryGetValue(dto.Id.Value, out entity!))
            {
                // matched by id
            }
            else if (byCode.TryGetValue(dto.Code.Trim(), out entity!))
            {
                // matched by code
            }
            else
            {
                entity = new NavigationMenuItem { Id = dto.Id ?? Guid.NewGuid() };
                _db.NavigationMenuItems.Add(entity);
                byId[entity.Id] = entity;
                byCode[dto.Code.Trim()] = entity;
            }

            mapped.Add((dto, entity));
        }

        var clientIdToEntityId = new Dictionary<Guid, Guid>();
        foreach (var (dto, entity) in mapped)
        {
            if (dto.Id.HasValue)
            {
                clientIdToEntityId[dto.Id.Value] = entity.Id;
            }

            clientIdToEntityId[entity.Id] = entity.Id;
        }

        foreach (var (dto, entity) in mapped)
        {
            entity.Code = dto.Code.Trim();
            entity.LabelKey = dto.LabelKey.Trim();
            entity.Route = string.IsNullOrWhiteSpace(dto.Route) ? null : dto.Route.Trim();
            entity.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon.Trim();
            entity.RequiredPermissions = string.IsNullOrWhiteSpace(dto.RequiredPermissions)
                ? null
                : dto.RequiredPermissions.Trim();
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            if (dto.ParentId.HasValue)
            {
                Guid parentId;
                if (clientIdToEntityId.TryGetValue(dto.ParentId.Value, out parentId))
                {
                    // mapped from request payload
                }
                else if (byId.ContainsKey(dto.ParentId.Value))
                {
                    parentId = dto.ParentId.Value;
                }
                else
                {
                    return Result<IReadOnlyList<NavigationMenuItemAdminDto>>.Failure(
                        $"Parent menu item for '{dto.Code}' was not found.");
                }

                if (parentId == entity.Id)
                {
                    return Result<IReadOnlyList<NavigationMenuItemAdminDto>>.Failure(
                        $"Menu item '{dto.Code}' cannot be its own parent.");
                }

                entity.ParentId = parentId;
            }
            else
            {
                entity.ParentId = null;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var all = await _db.NavigationMenuItems.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.LabelKey)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<NavigationMenuItemAdminDto>>.Success(
            all.Select(NavigationMenuMapper.ToAdminDto).ToList());
    }
}

public sealed class ResetNavigationMenuCommandHandler
    : IRequestHandler<ResetNavigationMenuCommand, Result<IReadOnlyList<NavigationMenuItemAdminDto>>>
{
    private readonly IAppDbContext _db;

    public ResetNavigationMenuCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<NavigationMenuItemAdminDto>>> Handle(
        ResetNavigationMenuCommand request,
        CancellationToken cancellationToken)
    {
        await NavigationMenuSeeder.SeedAsync(_db, cancellationToken);

        var all = await _db.NavigationMenuItems.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.LabelKey)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<NavigationMenuItemAdminDto>>.Success(
            all.Select(NavigationMenuMapper.ToAdminDto).ToList());
    }
}

internal static class NavigationMenuMapper
{
    public static NavigationMenuItemAdminDto ToAdminDto(NavigationMenuItem item) => new(
        item.Id,
        item.Code,
        item.ParentId,
        item.LabelKey,
        item.Route,
        item.Icon,
        item.RequiredPermissions,
        item.SortOrder,
        item.IsActive);
}
