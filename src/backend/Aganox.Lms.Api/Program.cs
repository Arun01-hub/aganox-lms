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

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true));
    });

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    await DatabaseSeeder.SeedAsync(app.Services);

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

public partial class Program;
