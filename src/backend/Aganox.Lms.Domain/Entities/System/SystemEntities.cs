using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Entities.System;

public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}

public class Notification : AuditableEntity
{
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string TemplateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public string? Error { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
}

public class NotificationTemplate : SoftDeletableEntity
{
    public string Key { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
}

public class AuditLog
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ActivityLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AppSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class BackupHistory : AuditableEntity
{
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public Guid? InitiatedByUserId { get; set; }
}
