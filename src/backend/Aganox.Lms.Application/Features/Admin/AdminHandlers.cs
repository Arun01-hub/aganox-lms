using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Admin.Dtos;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.System;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Admin;

public sealed record GetSettingsQuery : IRequest<IReadOnlyList<AppSettingDto>>;
public sealed record UpdateSettingsCommand(IReadOnlyList<AppSettingDto> Settings) : IRequest<Result<IReadOnlyList<AppSettingDto>>>;
public sealed record GetPermissionsMatrixQuery : IRequest<IReadOnlyList<RolePermissionsDto>>;
public sealed record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyList<string> Permissions) : IRequest<Result<RolePermissionsDto>>;
public sealed record GetAuditLogsQuery(string? EntityName, Guid? UserId, DateTime? From, DateTime? To, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<AuditLogDto>>;
public sealed record GetActivityLogsQuery(Guid? UserId, DateTime? From, DateTime? To, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ActivityLogDto>>;
public sealed record GetBackupHistoryQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<BackupHistoryDto>>;
public sealed record CreateBackupCommand : IRequest<Result<BackupResultDto>>;
public sealed record RestoreBackupCommand(string BackupFilePath) : IRequest<Result>;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.Settings).NotEmpty();
        RuleForEach(x => x.Settings).ChildRules(s =>
        {
            s.RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
            s.RuleFor(x => x.Value).MaximumLength(2000);
        });
    }
}

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
    }
}

public sealed class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, IReadOnlyList<AppSettingDto>>
{
    private readonly IAppDbContext _db;

    public GetSettingsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AppSettingDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        await AdminSettingsInitializer.EnsureDefaultsAsync(_db, cancellationToken);
        return await _db.AppSettings.AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new AppSettingDto(x.Key, x.Value))
            .ToListAsync(cancellationToken);
    }
}

public sealed class UpdateSettingsCommandHandler : IRequestHandler<UpdateSettingsCommand, Result<IReadOnlyList<AppSettingDto>>>
{
    private readonly IAppDbContext _db;
    private readonly IActivityLogger _activity;

    public UpdateSettingsCommandHandler(IAppDbContext db, IActivityLogger activity)
    {
        _db = db;
        _activity = activity;
    }

    public async Task<Result<IReadOnlyList<AppSettingDto>>> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        await AdminSettingsInitializer.EnsureDefaultsAsync(_db, cancellationToken);

        foreach (var item in request.Settings)
        {
            var setting = await _db.AppSettings.FirstOrDefaultAsync(x => x.Key == item.Key, cancellationToken);
            if (setting is null)
            {
                _db.AppSettings.Add(new AppSetting { Key = item.Key, Value = item.Value });
            }
            else
            {
                setting.Value = item.Value;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _activity.LogAsync("Settings.Updated", $"Updated {request.Settings.Count} setting(s).", cancellationToken);

        var all = await _db.AppSettings.AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new AppSettingDto(x.Key, x.Value))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<AppSettingDto>>.Success(all);
    }
}

public sealed class GetPermissionsMatrixQueryHandler : IRequestHandler<GetPermissionsMatrixQuery, IReadOnlyList<RolePermissionsDto>>
{
    private readonly IIdentityService _identity;

    public GetPermissionsMatrixQueryHandler(IIdentityService identity) => _identity = identity;

    public Task<IReadOnlyList<RolePermissionsDto>> Handle(
        GetPermissionsMatrixQuery request,
        CancellationToken cancellationToken) =>
        _identity.GetRolesWithPermissionsAsync(cancellationToken);
}

public sealed class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result<RolePermissionsDto>>
{
    private readonly IIdentityService _identity;
    private readonly IActivityLogger _activity;

    public UpdateRolePermissionsCommandHandler(IIdentityService identity, IActivityLogger activity)
    {
        _identity = identity;
        _activity = activity;
    }

    public async Task<Result<RolePermissionsDto>> Handle(
        UpdateRolePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var invalid = request.Permissions
            .Where(p => !Permissions.All.Contains(p, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (invalid.Count > 0)
        {
            return Result<RolePermissionsDto>.Failure($"Unknown permissions: {string.Join(", ", invalid)}");
        }

        var (succeeded, errors) = await _identity.SetRolePermissionsAsync(
            request.RoleId, request.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), cancellationToken);
        if (!succeeded)
        {
            return Result<RolePermissionsDto>.Failure(errors);
        }

        var roles = await _identity.GetRolesWithPermissionsAsync(cancellationToken);
        var updated = roles.FirstOrDefault(x => x.RoleId == request.RoleId);
        if (updated is null)
        {
            return Result<RolePermissionsDto>.Failure("Role not found.");
        }

        await _activity.LogAsync("Permissions.Updated", $"Role {updated.RoleName} permissions updated.", cancellationToken);
        return Result<RolePermissionsDto>.Success(updated);
    }
}

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAppDbContext _db;

    public GetAuditLogsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            query = query.Where(x => x.EntityName == request.EntityName);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == request.UserId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.Timestamp >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.Timestamp <= request.To.Value);
        }

        query = query.OrderByDescending(x => x.Timestamp);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogDto(
                x.Id, x.EntityName, x.EntityId, x.Action, x.OldValues, x.NewValues, x.UserId, x.Timestamp))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}

public sealed class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, PagedResult<ActivityLogDto>>
{
    private readonly IAppDbContext _db;

    public GetActivityLogsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<ActivityLogDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);
        var query = _db.ActivityLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == request.UserId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.Timestamp >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.Timestamp <= request.To.Value);
        }

        query = query.OrderByDescending(x => x.Timestamp);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ActivityLogDto(x.Id, x.UserId, x.Action, x.Details, x.Timestamp))
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityLogDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}

public sealed class GetBackupHistoryQueryHandler : IRequestHandler<GetBackupHistoryQuery, PagedResult<BackupHistoryDto>>
{
    private readonly IAppDbContext _db;

    public GetBackupHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<BackupHistoryDto>> Handle(GetBackupHistoryQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);
        var query = _db.BackupHistories.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new BackupHistoryDto(x.Id, x.FilePath, x.Status, x.Message, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<BackupHistoryDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }
}

public sealed class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, Result<BackupResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly IDatabaseBackupService _backup;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogger _activity;

    public CreateBackupCommandHandler(
        IAppDbContext db,
        IDatabaseBackupService backup,
        ICurrentUserService currentUser,
        IActivityLogger activity)
    {
        _db = db;
        _backup = backup;
        _currentUser = currentUser;
        _activity = activity;
    }

    public async Task<Result<BackupResultDto>> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        if (!_backup.IsSupported)
        {
            return Result<BackupResultDto>.Failure("Database backup is not supported for the current database provider.");
        }

        var (succeeded, filePath, error) = await _backup.CreateBackupAsync(cancellationToken);
        var history = new BackupHistory
        {
            FilePath = filePath ?? string.Empty,
            Status = succeeded ? "Completed" : "Failed",
            Message = error,
            InitiatedByUserId = _currentUser.UserId
        };
        _db.BackupHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);

        if (!succeeded)
        {
            return Result<BackupResultDto>.Failure(error ?? "Backup failed.");
        }

        await _activity.LogAsync("Backup.Created", filePath, cancellationToken);
        return Result<BackupResultDto>.Success(new BackupResultDto(history.Id, filePath!, history.Status));
    }
}

public sealed class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, Result>
{
    private readonly IDatabaseBackupService _backup;
    private readonly IActivityLogger _activity;

    public RestoreBackupCommandHandler(IDatabaseBackupService backup, IActivityLogger activity)
    {
        _backup = backup;
        _activity = activity;
    }

    public async Task<Result> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        if (!_backup.IsSupported)
        {
            return Result.Failure("Database restore is not supported for the current database provider.");
        }

        if (string.IsNullOrWhiteSpace(request.BackupFilePath) || !File.Exists(request.BackupFilePath))
        {
            return Result.Failure("Backup file not found.");
        }

        var (succeeded, error) = await _backup.RestoreBackupAsync(request.BackupFilePath, cancellationToken);
        if (!succeeded)
        {
            return Result.Failure(error ?? "Restore failed.");
        }

        await _activity.LogAsync("Backup.Restored", request.BackupFilePath, cancellationToken);
        return Result.Success();
    }
}
