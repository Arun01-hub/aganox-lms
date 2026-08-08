using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Lms.Api.Tests;

[Collection("LmsApi")]
public class PayrollApiTests
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public PayrollApiTests(LmsApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
        _services = fixture.Factory.Services;
        PayrollTestData.EnsureSeeded(_services);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken(PayrollTestData.UserId));
    }

    [Fact]
    public async Task ExportPayslip_works_before_lock_for_payroll_processors()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 12, notes = "Dec" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var run = await create.Content.ReadFromJsonAsync<JsonElement>();
        var runId = run.GetProperty("id").GetGuid();

        var calc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate", null);
        Assert.Equal(HttpStatusCode.OK, calc.StatusCode);

        var export = await _client.GetAsync(
            $"/api/v1/payroll/payslips/{runId}/export?employeeId={LeaveTestData.EmployeeId}");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("application/pdf", export.Content.Headers.ContentType?.MediaType);
        var pdfBytes = await export.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 4)));
        Assert.Contains("%%EOF", System.Text.Encoding.ASCII.GetString(pdfBytes));
    }

    [Fact]
    public async Task CreatePayrollRun_reuses_existing_month_and_clears_results()
    {
        var first = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 10, notes = "Oct" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstRun = await first.Content.ReadFromJsonAsync<JsonElement>();
        var runId = firstRun.GetProperty("id").GetGuid();

        var calc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate", null);
        Assert.Equal(HttpStatusCode.OK, calc.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 10, notes = "Oct rerun" });
        Assert.Equal(HttpStatusCode.Created, duplicate.StatusCode);
        var reused = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(runId, reused.GetProperty("id").GetGuid());
        Assert.Equal("Draft", reused.GetProperty("status").GetString());

        var results = await _client.GetAsync($"/api/v1/payroll/runs/{runId}/results");
        Assert.Equal(HttpStatusCode.OK, results.StatusCode);
        var resultItems = await results.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, resultItems.GetArrayLength());
    }

    [Fact]
    public async Task Recalculate_allowed_before_lock()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 11, notes = "Nov" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var run = await create.Content.ReadFromJsonAsync<JsonElement>();
        var runId = run.GetProperty("id").GetGuid();

        var calc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate", null);
        Assert.Equal(HttpStatusCode.OK, calc.StatusCode);

        var approve = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var recalc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate?recalculate=true", null);
        Assert.Equal(HttpStatusCode.OK, recalc.StatusCode);

        var updated = await recalc.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Calculated", updated.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PayrollWorkflow_calculate_approve_lock_and_block_recalculate()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 8, notes = "Aug" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var run = await create.Content.ReadFromJsonAsync<JsonElement>();
        var runId = run.GetProperty("id").GetGuid();

        var calc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate", null);
        Assert.Equal(HttpStatusCode.OK, calc.StatusCode);

        var approve = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var lockResp = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/lock", null);
        Assert.Equal(HttpStatusCode.OK, lockResp.StatusCode);

        var recalc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate?recalculate=true", null);
        Assert.Equal(HttpStatusCode.BadRequest, recalc.StatusCode);
    }

    [Fact]
    public async Task SyncAttendanceFromLeave_does_not_modify_leave_requests()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leaveCountBefore = db.LeaveRequests.Count();

        var response = await _client.PostAsJsonAsync("/api/v1/attendance/sync-from-leave",
            new { year = 2026, month = 8, departmentId = (Guid?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var leaveCountAfter = db.LeaveRequests.Count();
        Assert.Equal(leaveCountBefore, leaveCountAfter);
    }

    [Fact]
    public async Task CreateSalaryComponent_accepts_string_enums()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var payload = new
        {
            code = $"T{suffix}",
            nameEn = "Vehicle Allowance",
            nameTa = "வாகன உதவி",
            category = "Earning",
            calculationType = "Fixed",
            baseComponentCode = (string?)null,
            sortOrder = 8,
            isTaxable = false,
            includeInGross = true,
            isActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payroll/components", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SalaryEntry_save_can_be_repeated_after_adjustments()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 7, notes = "Jul" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var run = await create.Content.ReadFromJsonAsync<JsonElement>();
        var runId = run.GetProperty("id").GetGuid();

        var calc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate", null);
        Assert.Equal(HttpStatusCode.OK, calc.StatusCode);

        var entryResp = await _client.GetAsync($"/api/v1/payroll/runs/{runId}/salary-entry?employeeId={LeaveTestData.EmployeeId}");
        var entry = await entryResp.Content.ReadFromJsonAsync<JsonElement>();
        var lines = entry.GetProperty("lines").EnumerateArray()
            .Select(x => new { componentCode = x.GetProperty("componentCode").GetString(), amount = x.GetProperty("amount").GetDecimal() })
            .ToList();

        var firstSaveLines = lines
            .Select(x => new { componentCode = x.componentCode, amount = x.componentCode == "VEHICLE" ? 1500m : x.amount })
            .ToList();
        var firstSave = await _client.PutAsJsonAsync(
            $"/api/v1/payroll/runs/{runId}/salary-entry/{LeaveTestData.EmployeeId}",
            new { lines = firstSaveLines });
        Assert.True(firstSave.IsSuccessStatusCode, await firstSave.Content.ReadAsStringAsync());

        var secondSaveLines = firstSaveLines
            .Select(x => new { componentCode = x.componentCode, amount = x.componentCode == "VEHICLE" ? 0m : x.amount })
            .ToList();
        var secondSave = await _client.PutAsJsonAsync(
            $"/api/v1/payroll/runs/{runId}/salary-entry/{LeaveTestData.EmployeeId}",
            new { lines = secondSaveLines });
        Assert.True(secondSave.IsSuccessStatusCode, await secondSave.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SalaryEntry_save_updates_net_pay_and_blocks_when_locked()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/payroll/runs", new { year = 2026, month = 9, notes = "Sep" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var run = await create.Content.ReadFromJsonAsync<JsonElement>();
        var runId = run.GetProperty("id").GetGuid();

        var calc = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/calculate", null);
        Assert.Equal(HttpStatusCode.OK, calc.StatusCode);

        var entryResp = await _client.GetAsync($"/api/v1/payroll/runs/{runId}/salary-entry?employeeId={LeaveTestData.EmployeeId}");
        Assert.Equal(HttpStatusCode.OK, entryResp.StatusCode);
        var entry = await entryResp.Content.ReadFromJsonAsync<JsonElement>();
        var lines = entry.GetProperty("lines").EnumerateArray()
            .Select(x => new { componentCode = x.GetProperty("componentCode").GetString(), amount = x.GetProperty("amount").GetDecimal() })
            .ToList();

        var saveLines = lines.Select(x => new { componentCode = x.componentCode, amount = x.componentCode == "BASIC" ? x.amount + 500 : x.amount }).ToList();
        var save = await _client.PutAsJsonAsync(
            $"/api/v1/payroll/runs/{runId}/salary-entry/{LeaveTestData.EmployeeId}",
            new { lines = saveLines });
        var saveBody = await save.Content.ReadAsStringAsync();
        Assert.True(save.IsSuccessStatusCode, saveBody);
        var saved = await save.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(saved.GetProperty("isManuallyAdjusted").GetBoolean());
        Assert.True(saved.GetProperty("netPay").GetDecimal() > 0);

        var approve = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var lockResp = await _client.PostAsync($"/api/v1/payroll/runs/{runId}/lock", null);
        Assert.Equal(HttpStatusCode.OK, lockResp.StatusCode);

        var blockedSave = await _client.PutAsJsonAsync(
            $"/api/v1/payroll/runs/{runId}/salary-entry/{LeaveTestData.EmployeeId}",
            new { lines = saveLines });
        Assert.Equal(HttpStatusCode.BadRequest, blockedSave.StatusCode);
    }
}

internal static class PayrollTestData
{
    internal static readonly Guid UserId = Guid.Parse("dddddddd-eeee-ffff-0000-111111111111");

    internal static void EnsureSeeded(IServiceProvider services)
    {
        LeaveTestData.EnsureSeeded(services);
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.EmployeeSalaryRevisions.Any(x => x.EmployeeId == LeaveTestData.EmployeeId))
        {
            return;
        }

        if (!db.SalaryComponents.Any())
        {
            var da = new SalaryComponent
            {
                Code = "DA",
                NameEn = "DA",
                NameTa = "DA",
                Category = SalaryComponentCategory.Earning,
                CalculationType = SalaryCalculationType.PercentOfBasic,
                SortOrder = 2,
                IncludeInGross = true
            };
            db.SalaryComponents.Add(da);
            db.SalaryRules.Add(new SalaryRule
            {
                SalaryComponentId = da.Id,
                EffectiveFrom = new DateOnly(2020, 1, 1),
                Priority = 1,
                ParametersJson = """{"rate":0.1}"""
            });
            db.SaveChanges();
        }

        db.EmployeeSalaryRevisions.Add(new EmployeeSalaryRevision
        {
            EmployeeId = LeaveTestData.EmployeeId,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            BasicPay = 30000
        });
        db.SaveChanges();
    }
}
