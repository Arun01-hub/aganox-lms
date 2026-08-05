using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Notifications;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;
using Aganox.Lms.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aganox.Lms.Infrastructure.Notifications;

public sealed class PendingApprovalReminderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingApprovalReminderHostedService> _logger;

    public PendingApprovalReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingApprovalReminderHostedService> logger)
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
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending approval reminder job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<LeaveNotificationService>();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var cutoff = DateTime.UtcNow.AddDays(-3);

        var staleRequests = await db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.ApprovalSteps)
            .Where(x => x.Status == LeaveRequestStatus.Pending && x.CreatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var request in staleRequests)
        {
            var currentStep = ApprovalWorkflow.GetCurrentPendingStep(request.ApprovalSteps);
            if (currentStep is null)
            {
                continue;
            }

            var alreadySent = await db.Notifications.AnyAsync(x =>
                x.RelatedEntityId == request.Id &&
                x.TemplateKey == NotificationTemplateKeys.LeavePendingReminder &&
                x.CreatedAt >= DateTime.UtcNow.AddDays(-1), cancellationToken);
            if (alreadySent)
            {
                continue;
            }

            var recipientUserIds = new List<Guid>();
            if (currentStep.ApproverEmployeeId.HasValue)
            {
                var managerUserId = await db.Employees.AsNoTracking()
                    .Where(x => x.Id == currentStep.ApproverEmployeeId.Value)
                    .Select(x => x.UserId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (managerUserId.HasValue)
                {
                    recipientUserIds.Add(managerUserId.Value);
                }
            }
            else if (string.Equals(currentStep.ApproverRole, ApprovalRoles.Hr, StringComparison.OrdinalIgnoreCase))
            {
                recipientUserIds.AddRange(await identity.GetUserIdsInRoleAsync(Roles.Hr, cancellationToken));
                recipientUserIds.AddRange(await identity.GetUserIdsInRoleAsync(Roles.Administrator, cancellationToken));
            }

            if (recipientUserIds.Count == 0)
            {
                continue;
            }

            var daysPending = Math.Max(1, (int)(DateTime.UtcNow - request.CreatedAt).TotalDays);
            await notifications.NotifyPendingReminderAsync(
                request,
                request.Employee!.FullName,
                daysPending,
                recipientUserIds,
                cancellationToken);
        }
    }
}
