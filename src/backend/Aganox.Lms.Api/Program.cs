using Aganox.Lms.Api.Middleware;
using Aganox.Lms.Application;
using Aganox.Lms.Infrastructure;
using Aganox.Lms.Persistence;
using Aganox.Lms.Persistence.Seed;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile(
        Path.Combine(builder.Environment.ContentRootPath, "appsettings.Local.json"),
        optional: true,
        reloadOnChange: false);

    if (builder.Environment.IsProduction())
    {
        ValidateProductionConfiguration(builder.Configuration);
    }

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/aganox-lms-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddApplication();
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Aganox.Lms.Persistence.Context.AppDbContext>();

    builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Aganox LMS API",
            Version = "v1",
            Description = "Enterprise Leave Management System API"
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            if (builder.Environment.IsDevelopment() || allowedOrigins.Length == 0)
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true);
            }
            else
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    var app = builder.Build();
    var httpsEnabled = IsHttpsEnabled(app.Configuration);

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else if (!app.Environment.IsEnvironment("Testing") && httpsEnabled)
    {
        app.UseHsts();
    }

    if (httpsEnabled && !app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }

    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        app.MapFallbackToFile("index.html");
    }

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }

    Log.Information("Aganox LMS API starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void ValidateProductionConfiguration(IConfiguration configuration)
{
    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) ||
        jwtKey.Length < 32 ||
        jwtKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        jwtKey.Contains("ChangeInProduction", StringComparison.OrdinalIgnoreCase) ||
        jwtKey.Contains("Dev_Secret", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Production requires a secure Jwt:Key (32+ characters). " +
            "Run Start-AganoxLms.cmd to auto-create appsettings.Local.json, " +
            "or set the Jwt__Key environment variable.");
    }
}

static bool IsHttpsEnabled(IConfiguration configuration)
{
    var urls = configuration["ASPNETCORE_URLS"]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
        ?? string.Empty;
    return urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
