using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Lms.Api.Tests;

[Collection("LmsApi")]
public class ApprovalsApiTests
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public ApprovalsApiTests(LmsApiFixture fixture)
    {
        _services = fixture.Factory.Services;
        LeaveTestData.EnsureSeeded(_services);
        _client = fixture.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                TestJwtFactory.CreateToken(LeaveRequestsApiTests.UserId));
    }

    [Fact]
    public async Task ApproveLeaveRequest_as_hr_finalizes_request()
    {
        Guid requestId;
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            var request = new LeaveRequest
            {
                EmployeeId = LeaveRequestsApiTests.EmployeeId,
                LeaveTypeId = LeaveRequestsApiTests.LeaveTypeId,
                FromDate = new DateOnly(2026, 11, 10),
                ToDate = new DateOnly(2026, 11, 10),
                NumberOfDays = 1,
                Status = LeaveRequestStatus.Pending
            };
            db.LeaveRequests.Add(request);
            await db.SaveChangesAsync();

            db.LeaveApprovalSteps.Add(new LeaveApprovalStep
            {
                LeaveRequestId = request.Id,
                StepOrder = 1,
                ApproverRole = "HR",
                Status = ApprovalStepStatus.Pending
            });
            await db.SaveChangesAsync();
            requestId = request.Id;
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/approvals/{requestId}/approve",
            new { comments = "Approved by HR" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approved", body.GetProperty("status").GetString());
    }
}
