using Aganox.Lms.Domain.Entities.Attendance;
using Aganox.Lms.Domain.Entities.Extensibility;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aganox.Lms.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Designation).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.EmployeeCode).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.DepartmentId);
        builder.HasIndex(x => x.ManagerId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Manager)
            .WithMany(x => x.DirectReports)
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaveCalendar)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.LeaveCalendarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveCalendarConfiguration : IEntityTypeConfiguration<LeaveCalendar>
{
    public void Configure(EntityTypeBuilder<LeaveCalendar> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.LeaveCalendarId, x.HolidayDate }).IsUnique();
        builder.HasOne(x => x.LeaveCalendar)
            .WithMany(x => x.Holidays)
            .HasForeignKey(x => x.LeaveCalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ColorHex).HasMaxLength(7);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class LeavePolicyConfiguration : IEntityTypeConfiguration<LeavePolicy>
{
    public void Configure(EntityTypeBuilder<LeavePolicy> builder)
    {
        builder.Property(x => x.CarryForwardMaxDays).HasPrecision(9, 2);
        builder.Property(x => x.DefaultOpeningBalance).HasPrecision(9, 2);
        builder.HasIndex(x => new { x.LeaveTypeId, x.Year }).IsUnique();
        builder.HasOne(x => x.LeaveType)
            .WithMany(x => x.Policies)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.Property(x => x.OpeningBalance).HasPrecision(9, 2);
        builder.Property(x => x.Credited).HasPrecision(9, 2);
        builder.Property(x => x.Consumed).HasPrecision(9, 2);
        builder.Property(x => x.Adjusted).HasPrecision(9, 2);
        builder.Property(x => x.Remaining).HasPrecision(9, 2);
        builder.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LeaveType).WithMany(x => x.Balances).HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveBalanceAdjustmentConfiguration : IEntityTypeConfiguration<LeaveBalanceAdjustment>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceAdjustment> builder)
    {
        builder.Property(x => x.DeltaDays).HasPrecision(9, 2);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.LeaveBalance)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.LeaveBalanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.Property(x => x.NumberOfDays).HasPrecision(9, 2);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.HalfDayPeriod).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.EmployeeId, x.Status });
        builder.HasIndex(x => new { x.FromDate, x.ToDate });
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LeaveType).WithMany(x => x.Requests).HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveApprovalStepConfiguration : IEntityTypeConfiguration<LeaveApprovalStep>
{
    public void Configure(EntityTypeBuilder<LeaveApprovalStep> builder)
    {
        builder.Property(x => x.ApproverRole).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Comments).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.LeaveRequest)
            .WithMany(x => x.ApprovalSteps)
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ApproverEmployee)
            .WithMany()
            .HasForeignKey(x => x.ApproverEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class LeaveRequestCommentConfiguration : IEntityTypeConfiguration<LeaveRequestComment>
{
    public void Configure(EntityTypeBuilder<LeaveRequestComment> builder)
    {
        builder.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
        builder.HasOne(x => x.LeaveRequest)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(x => x.Token).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.TemplateKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.UserId, x.IsRead });
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SubjectTemplate).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(x => x.Action).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Timestamp);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public class BackupHistoryConfiguration : IEntityTypeConfiguration<BackupHistory>
{
    public void Configure(EntityTypeBuilder<BackupHistory> builder)
    {
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
    }
}

public class NavigationMenuItemConfiguration : IEntityTypeConfiguration<NavigationMenuItem>
{
    public void Configure(EntityTypeBuilder<NavigationMenuItem> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LabelKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Route).HasMaxLength(200);
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.RequiredPermissions).HasMaxLength(500);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.ParentId);
        builder.HasIndex(x => x.SortOrder);
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.Property(x => x.FieldKey).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LabelEn).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LabelTa).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityScope).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.DataType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OptionsJson).HasMaxLength(2000);
        builder.HasIndex(x => new { x.EntityScope, x.FieldKey }).IsUnique();
    }
}

public class CustomFieldValueConfiguration : IEntityTypeConfiguration<CustomFieldValue>
{
    public void Configure(EntityTypeBuilder<CustomFieldValue> builder)
    {
        builder.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.DefinitionId, x.EntityId }).IsUnique();
        builder.HasOne(x => x.Definition)
            .WithMany(x => x.Values)
            .HasForeignKey(x => x.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttendanceDayConfiguration : IEntityTypeConfiguration<AttendanceDay>
{
    public void Configure(EntityTypeBuilder<AttendanceDay> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameTa).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CalculationType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.BaseComponentCode).HasMaxLength(30);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class SalaryRuleConfiguration : IEntityTypeConfiguration<SalaryRule>
{
    public void Configure(EntityTypeBuilder<SalaryRule> builder)
    {
        builder.Property(x => x.ParametersJson).HasMaxLength(4000).IsRequired();
        builder.HasOne(x => x.SalaryComponent)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.SalaryComponentId, x.EffectiveFrom });
    }
}

public class EmployeeSalaryRevisionConfiguration : IEntityTypeConfiguration<EmployeeSalaryRevision>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryRevision> builder)
    {
        builder.Property(x => x.BasicPay).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
    }
}

public class EmployeeSalaryComponentOverrideConfiguration : IEntityTypeConfiguration<EmployeeSalaryComponentOverride>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryComponentOverride> builder)
    {
        builder.Property(x => x.FixedAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.EmployeeSalaryRevision)
            .WithMany(x => x.ComponentOverrides)
            .HasForeignKey(x => x.EmployeeSalaryRevisionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalaryComponent)
            .WithMany()
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EmployeeSalaryRevisionId, x.SalaryComponentId }).IsUnique();
    }
}

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Year, x.Month }).IsUnique();
    }
}

public class PayrollEmployeeResultConfiguration : IEntityTypeConfiguration<PayrollEmployeeResult>
{
    public void Configure(EntityTypeBuilder<PayrollEmployeeResult> builder)
    {
        builder.Property(x => x.BasicPaySnapshot).HasPrecision(18, 2);
        builder.Property(x => x.GrossPay).HasPrecision(18, 2);
        builder.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        builder.Property(x => x.NetPay).HasPrecision(18, 2);
        builder.Property(x => x.PaidDays).HasPrecision(9, 2);
        builder.Property(x => x.LopDays).HasPrecision(9, 2);
        builder.Property(x => x.InputSnapshotJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.OutputSnapshotJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.CalculatedSnapshotJson).HasMaxLength(16000);
        builder.HasOne(x => x.PayrollRun).WithMany(x => x.EmployeeResults).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId }).IsUnique();
    }
}

public class PayrollLineItemConfiguration : IEntityTypeConfiguration<PayrollLineItem>
{
    public void Configure(EntityTypeBuilder<PayrollLineItem> builder)
    {
        builder.Property(x => x.ComponentCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ComponentName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne(x => x.PayrollEmployeeResult)
            .WithMany(x => x.LineItems)
            .HasForeignKey(x => x.PayrollEmployeeResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PayrollArrearConfiguration : IEntityTypeConfiguration<PayrollArrear>
{
    public void Configure(EntityTypeBuilder<PayrollArrear> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TargetPayrollRun).WithMany().HasForeignKey(x => x.TargetPayrollRunId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PayrollRunAuditConfiguration : IEntityTypeConfiguration<PayrollRunAudit>
{
    public void Configure(EntityTypeBuilder<PayrollRunAudit> builder)
    {
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.HasOne(x => x.PayrollRun).WithMany(x => x.AuditEntries).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Timestamp);
    }
}
