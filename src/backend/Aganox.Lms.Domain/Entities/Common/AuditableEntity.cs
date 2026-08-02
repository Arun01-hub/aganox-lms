namespace Aganox.Lms.Domain.Entities.Common;

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class AuditableEntity : EntityBase
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
