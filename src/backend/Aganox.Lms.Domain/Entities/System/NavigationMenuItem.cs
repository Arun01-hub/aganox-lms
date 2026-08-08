using Aganox.Lms.Domain.Entities.Common;

namespace Aganox.Lms.Domain.Entities.System;

public class NavigationMenuItem : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public NavigationMenuItem? Parent { get; set; }
    public ICollection<NavigationMenuItem> Children { get; set; } = new List<NavigationMenuItem>();
    public string LabelKey { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Icon { get; set; }
    /// <summary>Semicolon-separated permission codes; user needs any one (OR).</summary>
    public string? RequiredPermissions { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
