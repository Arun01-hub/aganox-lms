using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Navigation;

public static class NavigationMenuSeeder
{
    private sealed record SeedItem(
        string Code,
        string? ParentCode,
        string LabelKey,
        string? Route,
        string? Icon,
        string? RequiredPermissions,
        int SortOrder);

    private static readonly SeedItem[] Defaults =
    [
        new("home", null, "dashboard.title", "/", "bi-house-door", null, 1),
        new("profile", null, "nav.profile", "/profile", "bi-person-gear", null, 99),
        new("organization", null, "nav.group.organization", null, "bi-building", null, 10),
        new("organization.employees", "organization", "nav.employees", "/employees", "bi-people", "Employees.View", 11),
        new("organization.departments", "organization", "nav.departments", "/departments", "bi-diagram-3", "Employees.View", 12),
        new("leave", null, "nav.group.leave", null, "bi-calendar2-week", null, 20),
        new("leave.config", "leave", "nav.leaveConfig", "/leave/types", "bi-sliders", "LeaveTypes.Manage", 21),
        new("leave.requests", "leave", "nav.myLeave", "/leave/requests", "bi-calendar2-check", "Leave.Apply", 22),
        new("leave.preview", "leave", "nav.dayPreview", "/leave/preview", "bi-calculator", "Leave.Apply", 23),
        new("leave.approvals", "leave", "nav.approvals", "/approvals", "bi-check2-square", "Approvals.Act", 24),
        new("time", null, "nav.group.time", null, "bi-clock-history", null, 30),
        new("time.attendance", "time", "nav.attendance", "/attendance", "bi-calendar-check", "Attendance.View", 31),
        new("payroll", null, "nav.group.payroll", null, "bi-cash-stack", null, 40),
        new("payroll.runs", "payroll", "nav.payroll", "/payroll/runs", "bi-cash-stack", "Payroll.Config.View;Payroll.Process", 41),
        new("payroll.salaryEntry", "payroll", "nav.salaryEntry", "/payroll/salary-entry", "bi-pencil-square", "Payroll.Process", 42),
        new("payroll.salaries", "payroll", "nav.salaries", "/payroll/salaries", "bi-wallet2", "Payroll.Salary.View", 43),
        new("payroll.config", "payroll", "nav.payrollConfig", "/payroll/components", "bi-gear", "Payroll.Config.View", 44),
        new("payroll.reports", "payroll", "nav.payrollReports", "/payroll/reports", "bi-table", "Payroll.Reports.View", 45),
        new("payroll.arrears", "payroll", "nav.arrears", "/payroll/arrears", "bi-clock-history", "Payroll.Arrears.Manage", 46),
        new("reports", null, "nav.reports", "/reports", "bi-file-earmark-bar-graph", "Reports.View", 50),
        new("admin", null, "nav.admin", "/admin", "bi-shield-lock", "Admin.Settings;Admin.Audit;Admin.Backup;Users.Manage", 60)
    ];

    public static async Task SeedAsync(IAppDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.NavigationMenuItems.ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var idByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in Defaults)
        {
            if (!byCode.TryGetValue(seed.Code, out var entity))
            {
                entity = new NavigationMenuItem { Code = seed.Code };
                db.NavigationMenuItems.Add(entity);
                byCode[seed.Code] = entity;
            }

            entity.LabelKey = seed.LabelKey;
            entity.Route = seed.Route;
            entity.Icon = seed.Icon;
            entity.RequiredPermissions = seed.RequiredPermissions;
            entity.SortOrder = seed.SortOrder;
            entity.IsActive = true;
            idByCode[seed.Code] = entity.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var seed in Defaults)
        {
            var entity = byCode[seed.Code];
            entity.ParentId = seed.ParentCode is null
                ? null
                : idByCode.GetValueOrDefault(seed.ParentCode);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
