namespace Aganox.Lms.Application.Features.Navigation.Dtos;

public sealed record NavigationMenuItemDto(
    Guid Id,
    string Code,
    string LabelKey,
    string? Route,
    string? Icon,
    IReadOnlyList<NavigationMenuItemDto> Children);

public sealed record NavigationMenuItemAdminDto(
    Guid Id,
    string Code,
    Guid? ParentId,
    string LabelKey,
    string? Route,
    string? Icon,
    string? RequiredPermissions,
    int SortOrder,
    bool IsActive);

public sealed record SaveNavigationMenuItemDto(
    Guid? Id,
    string Code,
    Guid? ParentId,
    string LabelKey,
    string? Route,
    string? Icon,
    string? RequiredPermissions,
    int SortOrder,
    bool IsActive);

public sealed record SaveNavigationMenuRequest(IReadOnlyList<SaveNavigationMenuItemDto> Items);
