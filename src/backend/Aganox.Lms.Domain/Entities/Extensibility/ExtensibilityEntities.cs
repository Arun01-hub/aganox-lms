using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Entities.Extensibility;

public class CustomFieldDefinition : SoftDeletableEntity
{
    public CustomFieldEntityScope EntityScope { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string LabelTa { get; set; } = string.Empty;
    public CustomFieldDataType DataType { get; set; } = CustomFieldDataType.Text;
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CustomFieldValue> Values { get; set; } = new List<CustomFieldValue>();
}

public class CustomFieldValue : AuditableEntity
{
    public Guid DefinitionId { get; set; }
    public CustomFieldDefinition? Definition { get; set; }
    public Guid EntityId { get; set; }
    public string Value { get; set; } = string.Empty;
}
