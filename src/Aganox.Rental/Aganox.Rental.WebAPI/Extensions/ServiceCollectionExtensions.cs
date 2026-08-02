using Aganox.Rental.Application.DependencyInjection;
using Aganox.Rental.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace Aganox.Rental.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRentalWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(
                    System.Text.Json.JsonNamingPolicy.CamelCase));
            });
        services.AddProblemDetails();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Aganox Rental API",
                Version = "v1",
                Description = "Property rental microservice for the Aganox Rental platform."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT access token from Aganox Identity Server."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    ["aganox.api"]
                }
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<Infrastructure.Persistence.RentalDbContext>("database");

        var authority = configuration["Auth:Authority"]
            ?? throw new InvalidOperationException("Auth:Authority is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = configuration["Auth:Audience"] ?? "aganox.api";
                options.RequireHttpsMetadata = !configuration.GetValue("Auth:AllowHttpMetadata", false);
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = configuration["Auth:Audience"] ?? "aganox.api";
            });

        services.AddAuthorization();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
        services.AddCors(options =>
        {
            options.AddPolicy("SpaClient", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
