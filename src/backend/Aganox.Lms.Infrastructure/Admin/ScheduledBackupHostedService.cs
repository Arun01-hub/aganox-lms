using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Admin;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aganox.Lms.Infrastructure.Admin;

public sealed class ScheduledBackupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledBackupHostedService> _logger;

    public ScheduledBackupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledBackupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryRunScheduledBackupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled backup check failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task TryRunScheduledBackupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (config.GetValue<bool>("UseInMemoryDatabase"))
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await AdminSettingsInitializer.EnsureDefaultsAsync(db, cancellationToken);

        var settings = await db.AppSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        if (!bool.TryParse(settings.GetValueOrDefault(AppSettingKeys.BackupScheduleEnabled), out var enabled) || !enabled)
        {
            return;
        }

        var intervalHours = int.TryParse(settings.GetValueOrDefault(AppSettingKeys.BackupScheduleIntervalHours), out var h) ? h : 24;
        var last = await db.BackupHistories.AsNoTracking()
            .Where(x => x.Status == "Completed")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (last.HasValue && last.Value > DateTime.UtcNow.AddHours(-intervalHours))
        {
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new CreateBackupCommand(), cancellationToken);
        if (result.Succeeded)
        {
            _logger.LogInformation("Scheduled backup completed: {Path}", result.Data!.FilePath);
        }
        else
        {
            _logger.LogWarning("Scheduled backup failed: {Errors}", string.Join("; ", result.Errors));
        }
    }
}
