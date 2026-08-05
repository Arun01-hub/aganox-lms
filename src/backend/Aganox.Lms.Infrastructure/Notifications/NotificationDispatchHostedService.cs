using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aganox.Lms.Infrastructure.Notifications;

public sealed class NotificationDispatchHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatchHostedService> _logger;

    public NotificationDispatchHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationDispatchHostedService> logger)
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
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification dispatch failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var pending = await db.Notifications
            .Where(x => x.Status == NotificationStatus.Pending && x.Channel == NotificationChannel.Email)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var notification in pending)
        {
            var to = await identity.GetUserEmailAsync(notification.UserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(to))
            {
                notification.Status = NotificationStatus.Failed;
                notification.Error = "Recipient email not found.";
                continue;
            }

            try
            {
                await emailSender.SendAsync(to, notification.Subject, notification.Body, cancellationToken);
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.Error = null;
            }
            catch (Exception ex)
            {
                notification.Status = NotificationStatus.Failed;
                notification.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to send notification {NotificationId}", notification.Id);
            }
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
