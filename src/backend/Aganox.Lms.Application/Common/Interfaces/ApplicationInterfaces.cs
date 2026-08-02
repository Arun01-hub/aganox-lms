using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Employee> Employees { get; }
    DbSet<LeaveCalendar> LeaveCalendars { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeavePolicy> LeavePolicies { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveApprovalStep> LeaveApprovalSteps { get; }
    DbSet<LeaveRequestComment> LeaveRequestComments { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<BackupHistory> BackupHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    string CreateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, out DateTime expiresAt);
    string CreateRefreshToken();
}

public interface IIdentityService
{
    Task<AuthUserDto?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string[] Errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Token, string[] Errors)> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string UserName,
    Guid? EmployeeId,
    bool MustChangePassword);
