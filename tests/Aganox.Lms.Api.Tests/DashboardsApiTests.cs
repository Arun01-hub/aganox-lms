using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Lms.Api.Tests;

[Collection("LmsApi")]
public class DashboardsApiTests
{
    private readonly HttpClient _client;

    public DashboardsApiTests(LmsApiFixture fixture)
    {
        LeaveTestData.EnsureSeeded(fixture.Factory.Services);
        _client = fixture.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                TestJwtFactory.CreateToken(LeaveTestData.UserId));
    }

    [Fact]
    public async Task GetEmployeeDashboard_returns_balances()
    {
        var response = await _client.GetAsync("/api/v1/dashboards/employee?year=2026");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("balances", out _));
    }
}
