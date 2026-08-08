using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Entities.Attendance;
using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Entities.Extensibility;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Entities.System;
using Aganox.Lms.Persistence.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Persistence.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    private readonly ICurrentUserService? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveCalendar> LeaveCalendars => Set<LeaveCalendar>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments => Set<LeaveBalanceAdjustment>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveApprovalStep> LeaveApprovalSteps => Set<LeaveApprovalStep>();
    public DbSet<LeaveRequestComment> LeaveRequestComments => Set<LeaveRequestComment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<BackupHistory> BackupHistories => Set<BackupHistory>();
    public DbSet<NavigationMenuItem> NavigationMenuItems => Set<NavigationMenuItem>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
    public DbSet<AttendanceDay> AttendanceDays => Set<AttendanceDay>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<SalaryRule> SalaryRules => Set<SalaryRule>();
    public DbSet<EmployeeSalaryRevision> EmployeeSalaryRevisions => Set<EmployeeSalaryRevision>();
    public DbSet<EmployeeSalaryComponentOverride> EmployeeSalaryComponentOverrides => Set<EmployeeSalaryComponentOverride>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollEmployeeResult> PayrollEmployeeResults => Set<PayrollEmployeeResult>();
    public DbSet<PayrollLineItem> PayrollLineItems => Set<PayrollLineItem>();
    public DbSet<PayrollArrear> PayrollArrears => Set<PayrollArrear>();
    public DbSet<PayrollRunAudit> PayrollRunAudits => Set<PayrollRunAudit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplySoftDeleteFilter<Department>(builder);
        ApplySoftDeleteFilter<Employee>(builder);
        ApplySoftDeleteFilter<LeaveCalendar>(builder);
        ApplySoftDeleteFilter<Holiday>(builder);
        ApplySoftDeleteFilter<LeaveType>(builder);
        ApplySoftDeleteFilter<LeavePolicy>(builder);
        ApplySoftDeleteFilter<LeaveBalance>(builder);
        ApplySoftDeleteFilter<LeaveRequest>(builder);
        ApplySoftDeleteFilter<NotificationTemplate>(builder);
        ApplySoftDeleteFilter<CustomFieldDefinition>(builder);
        ApplySoftDeleteFilter<SalaryComponent>(builder);
        ApplySoftDeleteFilter<SalaryRule>(builder);
        ApplySoftDeleteFilter<EmployeeSalaryRevision>(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userName = _currentUser?.Email ?? _currentUser?.UserName ?? "system";
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.CreatedBy ??= userName;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = utcNow;
                entry.Entity.ModifiedBy = userName;
            }
        }

        foreach (var entry in ChangeTracker.Entries<SoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = utcNow;
                entry.Entity.DeletedBy = userName;
            }
        }

        AppendAuditLogs(utcNow);

        return base.SaveChangesAsync(cancellationToken);
    }

    private void AppendAuditLogs(DateTime utcNow)
    {
        var userId = _currentUser?.UserId;
        var entries = ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog or ActivityLog or BackupHistory)
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityName = entry.Metadata.ClrType.Name;
            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            if (idProp?.CurrentValue is null)
            {
                continue;
            }

            var entityId = idProp.CurrentValue.ToString() ?? string.Empty;
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                _ when entry.Entity is SoftDeletableEntity soft && soft.IsDeleted => "Deleted",
                _ => "Updated"
            };

            AuditLogs.Add(new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                Timestamp = utcNow,
                NewValues = entry.State == EntityState.Added ? "Added" : null
            });
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : SoftDeletableEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}
