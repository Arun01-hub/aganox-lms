using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Persistence.Context;
using Aganox.Lms.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Lms.Api.Tests;

[Collection("LmsApi")]
public class LeaveRequestsApiTests
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;
    internal static Guid UserId => LeaveTestData.UserId;
    internal static Guid EmployeeId => LeaveTestData.EmployeeId;
    internal static Guid LeaveTypeId => LeaveTestData.LeaveTypeId;

    public LeaveRequestsApiTests(LmsApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
        _services = fixture.Factory.Services;
        LeaveTestData.EnsureSeeded(_services);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken(UserId));
    }

    [Fact]
    public async Task CreateLeaveRequest_as_draft_returns_created()
    {
        var payload = new
        {
            leaveTypeId = LeaveTypeId,
            fromDate = "2026-09-01",
            toDate = "2026-09-01",
            isHalfDay = false,
            remarks = "Personal",
            submit = false
        };

        var response = await _client.PostAsJsonAsync("/api/v1/leave-requests", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Draft", created.GetProperty("status").GetString());
    }

    [Fact]
    public async Task SubmitLeaveRequest_with_overlap_returns_bad_request()
    {
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId = EmployeeId,
                LeaveTypeId = LeaveTypeId,
                FromDate = new DateOnly(2026, 10, 5),
                ToDate = new DateOnly(2026, 10, 7),
                NumberOfDays = 3,
                Status = LeaveRequestStatus.Pending
            });
            await db.SaveChangesAsync();
        }

        var payload = new
        {
            leaveTypeId = LeaveTypeId,
            fromDate = "2026-10-06",
            toDate = "2026-10-08",
            isHalfDay = false,
            submit = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/leave-requests", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

internal static class LeaveTestData
{
    internal static readonly Guid UserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    internal static readonly Guid EmployeeId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    internal static readonly Guid LeaveTypeId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");
    private static readonly Lock SeedLock = new();
    private static bool _seeded;

    internal static void EnsureSeeded(IServiceProvider services)
    {
        lock (SeedLock)
        {
            if (_seeded)
            {
                return;
            }

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            EnsureRolesSeeded(scope.ServiceProvider);

            if (db.Employees.Any(e => e.UserId == UserId))
            {
                _seeded = true;
                return;
            }

            if (!db.Departments.Any())
            {
                db.Departments.Add(new Department { Name = "Engineering", Code = "ENG", IsActive = true });
                db.SaveChanges();
            }

            var departmentId = db.Departments.Select(d => d.Id).First();
            var calendar = new LeaveCalendar { Name = "Default", IsDefault = true };
            db.LeaveCalendars.Add(calendar);
            db.SaveChanges();

            db.LeaveTypes.Add(new LeaveType
            {
                Id = LeaveTypeId,
                Code = "AL",
                Name = "Annual Leave",
                IsPaid = true,
                RequiresApproval = true,
                AllowHalfDay = true,
                AllowNegativeBalance = false,
                SortOrder = 1,
                IsActive = true
            });
            db.LeavePolicies.Add(new LeavePolicy
            {
                LeaveTypeId = LeaveTypeId,
                Year = 2026,
                SkipWeekends = true,
                SkipHolidays = true,
                DefaultOpeningBalance = 12
            });
            db.Employees.Add(new Employee
            {
                Id = EmployeeId,
                UserId = UserId,
                EmployeeCode = "EMP001",
                FullName = "Test Employee",
                DepartmentId = departmentId,
                Designation = "Developer",
                DateOfJoining = new DateOnly(2026, 1, 1),
                Email = "employee@test.local",
                LeaveCalendarId = calendar.Id
            });
            var balance = new LeaveBalance
            {
                EmployeeId = EmployeeId,
                LeaveTypeId = LeaveTypeId,
                Year = 2026,
                OpeningBalance = 12
            };
            balance.RecalculateRemaining();
            db.LeaveBalances.Add(balance);
            db.SaveChanges();
            _seeded = true;
        }
    }

    private static void EnsureRolesSeeded(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var roleName in Roles.All)
        {
            if (roleManager.FindByNameAsync(roleName).GetAwaiter().GetResult() is not null)
            {
                continue;
            }

            roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = $"{roleName} role"
            }).GetAwaiter().GetResult();
        }
    }
}
