using Aganox.Lms.Domain.Entities.System;

namespace Aganox.Lms.Application.Features.Navigation;

public static class NavigationMenuTreeBuilder
{
    public static IReadOnlyList<NavigationMenuItemNode> BuildVisibleTree(
        IReadOnlyList<NavigationMenuItem> items,
        IReadOnlyCollection<string> permissions)
    {
        var active = items.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.LabelKey).ToList();
        var byParent = active.ToLookup(x => x.ParentId);

        NavigationMenuItemNode? BuildNode(NavigationMenuItem item)
        {
            var children = byParent[item.Id]
                .Select(BuildNode)
                .Where(x => x is not null)
                .Cast<NavigationMenuItemNode>()
                .ToList();

            var hasRoute = !string.IsNullOrWhiteSpace(item.Route);
            var allowed = HasPermission(item, permissions);

            if (children.Count > 0)
            {
                return new NavigationMenuItemNode(
                    item.Id,
                    item.Code,
                    item.LabelKey,
                    item.Route,
                    item.Icon,
                    children);
            }

            if (!hasRoute || !allowed)
            {
                return null;
            }

            return new NavigationMenuItemNode(
                item.Id,
                item.Code,
                item.LabelKey,
                item.Route,
                item.Icon,
                []);
        }

        return byParent[null]
            .Select(BuildNode)
            .Where(x => x is not null)
            .Cast<NavigationMenuItemNode>()
            .ToList();
    }

    public static bool HasPermission(NavigationMenuItem item, IReadOnlyCollection<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(item.RequiredPermissions))
        {
            return true;
        }

        var required = item.RequiredPermissions.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return required.Any(p => permissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }
}

public sealed record NavigationMenuItemNode(
    Guid Id,
    string Code,
    string LabelKey,
    string? Route,
    string? Icon,
    IReadOnlyList<NavigationMenuItemNode> Children);
