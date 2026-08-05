using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Aganox.Lms.Api.Tests;

[Collection("LmsApi")]
public class AdminApiTests
{
    private readonly HttpClient _client;

    public AdminApiTests(LmsApiFixture fixture)
    {
        LeaveTestData.EnsureSeeded(fixture.Factory.Services);
        _client = fixture.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                TestJwtFactory.CreateToken(LeaveTestData.UserId));
    }

    [Fact]
    public async Task GetSettings_returns_defaults()
    {
        var response = await _client.GetAsync("/api/v1/admin/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetPermissions_returns_roles()
    {
        var response = await _client.GetAsync("/api/v1/admin/permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(items.GetArrayLength() >= 4);
    }
}
