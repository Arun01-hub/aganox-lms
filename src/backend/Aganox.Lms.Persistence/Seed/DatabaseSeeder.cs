using Aganox.Lms.Application.Features.Navigation;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;
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
        await SeedPayrollConfigurationAsync(db, cancellationToken);
        await NavigationMenuSeeder.SeedAsync(db, cancellationToken);
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
                Permissions.AdminAudit, Permissions.UsersManage,
                Permissions.AttendanceView, Permissions.AttendanceManage, Permissions.AttendanceImport,
                Permissions.PayrollConfigView, Permissions.PayrollConfigManage,
                Permissions.PayrollSalaryView, Permissions.PayrollSalaryManage,
                Permissions.PayrollProcess, Permissions.PayrollApprove, Permissions.PayrollLock,
                Permissions.PayrollPayslipViewAll, Permissions.PayrollReportsView, Permissions.PayrollReportsExport,
                Permissions.PayrollArrearsManage, Permissions.PayrollAuditView
            ],
            [Roles.Manager] =
            [
                Permissions.EmployeesView,
                Permissions.LeaveApply, Permissions.LeaveViewOwn, Permissions.LeaveViewTeam,
                Permissions.ApprovalsAct,
                Permissions.BalancesView,
                Permissions.ReportsView,
                Permissions.AttendanceView,
                Permissions.PayrollPayslipViewOwn
            ],
            [Roles.Employee] =
            [
                Permissions.LeaveApply, Permissions.LeaveViewOwn, Permissions.BalancesView,
                Permissions.AttendanceView,
                Permissions.PayrollPayslipViewOwn
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

        var year = DateTime.UtcNow.Year;
        var seeds = new (string Code, string Name, int SortOrder, string ColorHex, decimal OpeningBalance, decimal CarryForward, bool IsPaid)[]
        {
            ("CL", "Casual Leave", 1, "#2F6FED", 12, 0, true),
            ("EL", "Earned Leave", 2, "#2F7D4A", 12, 5, true),
            ("LOP", "Loss of Pay", 3, "#C44B4B", 0, 0, false)
        };

        foreach (var seed in seeds)
        {
            var existing = await db.LeaveTypes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == seed.Code, cancellationToken);

            LeaveType leaveType;
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                    existing.IsActive = true;
                }

                existing.Name = seed.Name;
                existing.SortOrder = seed.SortOrder;
                existing.ColorHex = seed.ColorHex;
                existing.IsPaid = seed.IsPaid;
                leaveType = existing;
            }
            else
            {
                leaveType = new LeaveType
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    SortOrder = seed.SortOrder,
                    ColorHex = seed.ColorHex,
                    IsPaid = seed.IsPaid
                };
                db.LeaveTypes.Add(leaveType);
            }

            var hasPolicy = await db.LeavePolicies
                .IgnoreQueryFilters()
                .AnyAsync(x => x.LeaveTypeId == leaveType.Id && x.Year == year && !x.IsDeleted, cancellationToken);

            if (!hasPolicy)
            {
                db.LeavePolicies.Add(new LeavePolicy
                {
                    LeaveTypeId = leaveType.Id,
                    Year = year,
                    SkipWeekends = true,
                    SkipHolidays = true,
                    CarryForwardMaxDays = seed.CarryForward,
                    YearlyResetEnabled = true,
                    DefaultOpeningBalance = seed.OpeningBalance
                });
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        _ = calendarId;
    }

    private static async Task SeedPayrollConfigurationAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var effectiveFrom = new DateOnly(2020, 1, 1);
        var components = new List<(SalaryComponent Component, string ParametersJson)>
        {
            (new SalaryComponent
            {
                Code = "BASIC",
                NameEn = "Basic Pay",
                NameTa = "அடிப்படை ஊதியம்",
                Category = SalaryComponentCategory.Earning,
                CalculationType = SalaryCalculationType.Fixed,
                SortOrder = 1,
                IncludeInGross = true
            }, """{"fixedAmount":0}"""),
            (new SalaryComponent
            {
                Code = "DA",
                NameEn = "Dearness Allowance",
                NameTa = "அதிவேக உதவித்தொகை",
                Category = SalaryComponentCategory.Earning,
                CalculationType = SalaryCalculationType.PercentOfBasic,
                SortOrder = 2,
                IncludeInGross = true
            }, """{"rate":0.12}"""),
            (new SalaryComponent
            {
                Code = "HRA",
                NameEn = "House Rent Allowance",
                NameTa = "வீட்டு வாடகை உதவித்தொகை",
                Category = SalaryComponentCategory.Earning,
                CalculationType = SalaryCalculationType.PercentOfBasic,
                SortOrder = 3,
                IncludeInGross = true
            }, """{"rate":0.10}"""),
            (new SalaryComponent
            {
                Code = "PF",
                NameEn = "Provident Fund",
                NameTa = "நிதிய உதவித்தொகை",
                Category = SalaryComponentCategory.Deduction,
                CalculationType = SalaryCalculationType.PercentOfBasic,
                SortOrder = 10,
                IncludeInGross = false
            }, """{"rate":0.12}"""),
            (new SalaryComponent
            {
                Code = "PT",
                NameEn = "Professional Tax",
                NameTa = "தொழில்முறை வரி",
                Category = SalaryComponentCategory.Deduction,
                CalculationType = SalaryCalculationType.Fixed,
                SortOrder = 11,
                IncludeInGross = false
            }, """{"fixedAmount":200}"""),
            (new SalaryComponent { Code = "CASH1", NameEn = "Cash I", NameTa = "படி I", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 4, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "WASH", NameEn = "Wash Allowance", NameTa = "சலவை படி", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 5, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "CASH2", NameEn = "Cash II", NameTa = "படி II", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 6, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "MEDICAL", NameEn = "Medical Allowance", NameTa = "மருத்துவ உதவி", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 7, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "VEHICLE", NameEn = "Vehicle Allowance", NameTa = "வாகன உதவி", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 8, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "NAT_HOL", NameEn = "National Holiday", NameTa = "விடுமுறை நாட்கள்", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 9, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "OTHERS_E", NameEn = "Others (Earning)", NameTa = "மற்றவை (வருவாய்)", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 12, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "RISK1", NameEn = "Risk Allowance I", NameTa = "ஆபத்து உதவி I", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 13, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "RISK2", NameEn = "Risk Allowance II", NameTa = "ஆபத்து உதவி II", Category = SalaryComponentCategory.Earning, CalculationType = SalaryCalculationType.Fixed, SortOrder = 14, IncludeInGross = true }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "FEST_ADV", NameEn = "Festival Advance", NameTa = "திருவிழா முன்பணம்", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 12, IncludeInGross = false }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "EMP_SOC", NameEn = "Employee Society", NameTa = "ஊழியர் சங்கம்", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 13, IncludeInGross = false }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "LIC", NameEn = "LIC", NameTa = "LIC", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 14, IncludeInGross = false }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "CONS_LOAN", NameEn = "Consumer Loan", NameTa = "நுகர்வோர் கடன்", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 15, IncludeInGross = false }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "OTHERS_D1", NameEn = "Others 1", NameTa = "மற்றவை 1", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 16, IncludeInGross = false }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "OTHERS_D2", NameEn = "Others 2", NameTa = "மற்றவை 2", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 17, IncludeInGross = false }, """{"fixedAmount":0}"""),
            (new SalaryComponent { Code = "OTHERS_D3", NameEn = "Others 3", NameTa = "மற்றவை 3", Category = SalaryComponentCategory.Deduction, CalculationType = SalaryCalculationType.Fixed, SortOrder = 18, IncludeInGross = false }, """{"fixedAmount":0}""")
        };

        foreach (var (component, parameters) in components)
        {
            var existing = await db.SalaryComponents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == component.Code, cancellationToken);

            SalaryComponent salaryComponent;
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                    existing.Category = component.Category;
                    existing.CalculationType = component.CalculationType;
                    existing.SortOrder = component.SortOrder;
                    existing.IncludeInGross = component.IncludeInGross;
                    existing.IsActive = true;
                }

                existing.NameEn = component.NameEn;
                existing.NameTa = component.NameTa;
                salaryComponent = existing;
            }
            else
            {
                salaryComponent = component;
                db.SalaryComponents.Add(salaryComponent);
            }

            var hasRule = await db.SalaryRules
                .IgnoreQueryFilters()
                .AnyAsync(
                    x => x.SalaryComponentId == salaryComponent.Id
                         && x.EffectiveFrom == effectiveFrom
                         && !x.IsDeleted,
                    cancellationToken);

            if (!hasRule)
            {
                db.SalaryRules.Add(new SalaryRule
                {
                    SalaryComponentId = salaryComponent.Id,
                    EffectiveFrom = effectiveFrom,
                    Priority = 1,
                    ParametersJson = parameters
                });
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
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
