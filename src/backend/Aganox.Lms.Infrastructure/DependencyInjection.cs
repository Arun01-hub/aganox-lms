using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Notifications;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Infrastructure.Admin;
using Aganox.Lms.Infrastructure.Auth;
using Aganox.Lms.Infrastructure.Authorization;
using Aganox.Lms.Infrastructure.Email;
using Aganox.Lms.Infrastructure.Identity;
using Aganox.Lms.Infrastructure.Notifications;
using Aganox.Lms.Persistence.Context;
using Aganox.Lms.Persistence.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Aganox.Lms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<OidcSettings>(configuration.GetSection(OidcSettings.SectionName));
        services.AddScoped<IClaimsTransformation, LmsUserClaimsTransformation>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<INotificationQueueService, NotificationQueueService>();
        services.AddScoped<LeaveNotificationService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<IDatabaseBackupService, SqlServerBackupService>();
        services.AddHostedService<NotificationDispatchHostedService>();
        services.AddHostedService<PendingApprovalReminderHostedService>();
        services.AddHostedService<ScheduledBackupHostedService>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        var oidc = configuration.GetSection(OidcSettings.SectionName).Get<OidcSettings>() ?? new OidcSettings();

        const string localJwtScheme = "LocalJwt";
        const string oidcJwtScheme = "OidcJwt";
        var jwtHandler = new JwtSecurityTokenHandler();

        if (oidc.Enabled && !string.IsNullOrWhiteSpace(oidc.Authority))
        {
            var authority = oidc.Authority.TrimEnd('/');

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, "Bearer", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var authorization = context.Request.Headers.Authorization.ToString();
                        if (string.IsNullOrWhiteSpace(authorization)
                            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            return localJwtScheme;
                        }

                        var token = authorization["Bearer ".Length..].Trim();
                        if (!jwtHandler.CanReadToken(token))
                        {
                            return localJwtScheme;
                        }

                        var issuer = jwtHandler.ReadJwtToken(token).Issuer;
                        if (string.Equals(issuer, jwt.Issuer, StringComparison.OrdinalIgnoreCase))
                        {
                            return localJwtScheme;
                        }

                        if (issuer.StartsWith(authority, StringComparison.OrdinalIgnoreCase))
                        {
                            return oidcJwtScheme;
                        }

                        return localJwtScheme;
                    };
                })
                .AddJwtBearer(localJwtScheme, options => ConfigureLocalJwtBearer(options, jwt))
                .AddJwtBearer(oidcJwtScheme, options => ConfigureOidcJwtBearer(options, oidc, authority));
        }
        else
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => ConfigureLocalJwtBearer(options, jwt));
        }

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });
        return services;
    }

    private static void ConfigureLocalJwtBearer(JwtBearerOptions options, JwtSettings jwt)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    private static void ConfigureOidcJwtBearer(JwtBearerOptions options, OidcSettings oidc, string authority)
    {
        options.Authority = authority;
        options.Audience = oidc.Audience;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = true;

        if (oidc.DisableCertificateValidation)
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
    }
}
