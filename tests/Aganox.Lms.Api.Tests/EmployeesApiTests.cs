using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Aganox.Lms.Api.Tests;

public class LmsApiFactory : WebApplicationFactory<Program>
{
    internal const string TestJwtKey = "AganoxLms_Test_Secret_Key_ForIntegrationTests!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "Aganox.Lms",
                ["Jwt:Audience"] = "Aganox.Lms.Clients"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = "Aganox.Lms",
                    ValidAudience = "Aganox.Lms.Clients",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

            var hosted = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
            foreach (var descriptor in hosted)
            {
                services.Remove(descriptor);
            }
        });
    }
}

[Collection("LmsApi")]
public class EmployeesApiTests
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public EmployeesApiTests(LmsApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
        _services = fixture.Factory.Services;
        SeedTestData();
        SetAdminToken();
    }

    [Fact]
    public async Task GetEmployees_returns_paged_list()
    {
        var response = await _client.GetAsync("/api/v1/employees?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("totalCount").GetInt32() >= 1);
        Assert.True(json.GetProperty("items").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task CreateEmployee_returns_created_employee()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var departmentId = db.Departments.Select(d => d.Id).First();

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var payload = new
        {
            employeeCode = $"E{suffix}",
            fullName = "Integration Test User",
            departmentId,
            designation = "Tester",
            dateOfJoining = "2026-01-01",
            status = "Active",
            email = $"integration.{suffix}@example.com",
            createUserAccount = false
        };

        var response = await _client.PostAsJsonAsync("/api/v1/employees", payload);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"E{suffix}", created.GetProperty("employeeCode").GetString());
    }

    private void SeedTestData()
    {
        LeaveTestData.EnsureSeeded(_services);
    }

    private void SetAdminToken()
    {
        var token = TestJwtFactory.CreateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

internal static class TestJwtFactory
{
    public static string CreateAdminToken() => CreateToken(Guid.NewGuid());

    public static string CreateToken(Guid userId)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(LmsApiFactory.TestJwtKey));

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new(System.Security.Claims.ClaimTypes.Email, "test@aganox.local")
        };

        foreach (var permission in Permissions.All)
        {
            claims.Add(new System.Security.Claims.Claim("permission", permission));
        }

        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, Roles.Administrator));

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "Aganox.Lms",
            audience: "Aganox.Lms.Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        return handler.WriteToken(token);
    }
}
