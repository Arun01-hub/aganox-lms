namespace Aganox.Lms.Application.Features.Admin.Dtos;

public sealed record AppSettingDto(string Key, string Value);

public sealed record UpdateSettingsRequest(IReadOnlyList<AppSettingDto> Settings);

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);

public sealed record AuditLogDto(
    long Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    Guid? UserId,
    DateTime Timestamp);

public sealed record ActivityLogDto(
    long Id,
    Guid? UserId,
    string Action,
    string? Details,
    DateTime Timestamp);

public sealed record BackupHistoryDto(
    Guid Id,
    string FilePath,
    string Status,
    string? Message,
    DateTime CreatedAt);

public sealed record BackupResultDto(Guid BackupId, string FilePath, string Status);

public sealed record RestoreBackupRequest(string BackupFilePath);
