using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Persistence.Context;
using Aganox.Lms.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aganox.Lms.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        await db.Database.MigrateAsync(cancellationToken);

        await SeedRolesAndPermissionsAsync(roleManager, cancellationToken);
        var calendar = await SeedCalendarAsync(db, cancellationToken);
        await SeedDepartmentsAndLeaveTypesAsync(db, calendar.Id, cancellationToken);
        await SeedAdminAsync(userManager, db, calendar.Id, cancellationToken);

        logger.LogInformation("Database seed completed.");
    }

    private static async Task SeedRolesAndPermissionsAsync(RoleManager<ApplicationRole> roleManager, CancellationToken cancellationToken)
    {
        var rolePermissions = new Dictionary<string, string[]>
        {
            [Roles.Administrator] = Permissions.All,
            [Roles.Hr] =
            [
                Permissions.EmployeesView, Permissions.EmployeesCreate, Permissions.EmployeesUpdate, Permissions.EmployeesDelete,
                Permissions.LeaveApply, Permissions.LeaveViewOwn, Permissions.LeaveViewTeam, Permissions.LeaveViewAll,
                Permissions.ApprovalsAct,
                Permissions.BalancesView, Permissions.BalancesAdjust, Permissions.BalancesAdmin,
                Permissions.LeaveTypesManage, Permissions.HolidaysManage,
                Permissions.ReportsView, Permissions.ReportsExport,
                Permissions.AdminAudit, Permissions.UsersManage
            ],
            [Roles.Manager] =
            [
                Permissions.EmployeesView,
                Permissions.LeaveApply, Permissions.LeaveViewOwn, Permissions.LeaveViewTeam,
                Permissions.ApprovalsAct,
                Permissions.BalancesView,
                Permissions.ReportsView
            ],
            [Roles.Employee] =
            [
                Permissions.LeaveApply, Permissions.LeaveViewOwn, Permissions.BalancesView
            ]
        };

        foreach (var roleName in Roles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = $"{roleName} role"
                };
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var permission in rolePermissions[roleName])
            {
                if (existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
                {
                    continue;
                }

                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task<LeaveCalendar> SeedCalendarAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var calendar = await db.LeaveCalendars.FirstOrDefaultAsync(x => x.IsDefault, cancellationToken);
        if (calendar is not null)
        {
            return calendar;
        }

        calendar = new LeaveCalendar
        {
            Name = "Default Calendar",
            IsDefault = true
        };
        db.LeaveCalendars.Add(calendar);

        var year = DateTime.UtcNow.Year;
        db.Holidays.AddRange(
            new Holiday { LeaveCalendarId = calendar.Id, HolidayDate = new DateOnly(year, 1, 1), Name = "New Year's Day" },
            new Holiday { LeaveCalendarId = calendar.Id, HolidayDate = new DateOnly(year, 12, 25), Name = "Christmas Day" });

        await db.SaveChangesAsync(cancellationToken);
        return calendar;
    }

    private static async Task SeedDepartmentsAndLeaveTypesAsync(AppDbContext db, Guid calendarId, CancellationToken cancellationToken)
    {
        if (!await db.Departments.AnyAsync(cancellationToken))
        {
            db.Departments.Add(new Department { Name = "General", Code = "GEN", IsActive = true });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.LeaveTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        var year = DateTime.UtcNow.Year;
        var types = new[]
        {
            new LeaveType { Code = "EL", Name = "Earned Leave", SortOrder = 1, ColorHex = "#2F6FED" },
            new LeaveType { Code = "SL", Name = "Sick Leave", SortOrder = 2, ColorHex = "#C44B4B" },
            new LeaveType { Code = "COMP", Name = "Compensatory Leave", SortOrder = 3, ColorHex = "#2F7D4A" }
        };

        foreach (var type in types)
        {
            db.LeaveTypes.Add(type);
            db.LeavePolicies.Add(new LeavePolicy
            {
                LeaveTypeId = type.Id,
                Year = year,
                SkipWeekends = true,
                SkipHolidays = true,
                CarryForwardMaxDays = type.Code == "EL" ? 5 : 0,
                YearlyResetEnabled = true,
                DefaultOpeningBalance = type.Code switch
                {
                    "EL" => 12,
                    "SL" => 6,
                    _ => 0
                }
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        _ = calendarId;
    }

    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        Guid calendarId,
        CancellationToken cancellationToken)
    {
        const string adminEmail = "admin@aganox.local";
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
        {
            return;
        }

        var department = await db.Departments.FirstAsync(cancellationToken);
        var employee = new Employee
        {
            EmployeeCode = "ADMIN001",
            FullName = "System Administrator",
            DepartmentId = department.Id,
            Designation = "Administrator",
            DateOfJoining = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Email = adminEmail,
            LeaveCalendarId = calendarId
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            EmployeeId = employee.Id,
            MustChangePassword = true,
            IsActive = true
        };

        var create = await userManager.CreateAsync(user, "ChangeMe!123");
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", create.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Roles.Administrator);
        employee.UserId = user.Id;
        await db.SaveChangesAsync(cancellationToken);
    }
}
