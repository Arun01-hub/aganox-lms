using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Notifications;

public static class LeaveNotificationComposer
{
    public static (string Subject, string Body) SubmittedToManager(LeaveRequest request, string employeeName) =>
        ($"Leave request pending your approval — {employeeName}",
         $"{employeeName} submitted {request.NumberOfDays} day(s) leave ({request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}). Please review in Aganox LMS.");

    public static (string Subject, string Body) PendingHr(LeaveRequest request, string employeeName) =>
        ($"Leave request pending HR approval — {employeeName}",
         $"Manager approved leave for {employeeName} ({request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}). HR approval is required.");

    public static (string Subject, string Body) Approved(LeaveRequest request) =>
        ("Leave request approved",
         $"Your leave from {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd} ({request.NumberOfDays} day(s)) has been approved.");

    public static (string Subject, string Body) Rejected(LeaveRequest request, string? reason) =>
        ("Leave request rejected",
         $"Your leave from {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd} was rejected.{(string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}")}");

    public static (string Subject, string Body) Cancelled(LeaveRequest request, string employeeName) =>
        ($"Leave request cancelled — {employeeName}",
         $"{employeeName} cancelled leave ({request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}).");

    public static (string Subject, string Body) PendingReminder(LeaveRequest request, string employeeName, int daysPending) =>
        ($"Reminder: leave approval pending — {employeeName}",
         $"Leave request for {employeeName} has been pending approval for {daysPending} day(s) ({request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}).");
}

public sealed class LeaveNotificationService
{
    private readonly IAppDbContext _db;
    private readonly INotificationQueueService _notifications;
    private readonly IIdentityService _identity;

    public LeaveNotificationService(
        IAppDbContext db,
        INotificationQueueService notifications,
        IIdentityService identity)
    {
        _db = db;
        _notifications = notifications;
        _identity = identity;
    }

    public async Task NotifySubmittedAsync(LeaveRequest request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstAsync(x => x.Id == request.EmployeeId, cancellationToken);

        if (employee.ManagerId.HasValue)
        {
            var managerUserId = await _db.Employees.AsNoTracking()
                .Where(x => x.Id == employee.ManagerId.Value)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (managerUserId.HasValue)
            {
                var (subject, body) = LeaveNotificationComposer.SubmittedToManager(request, employee.FullName);
                await _notifications.QueueEmailAsync(
                    managerUserId.Value,
                    NotificationTemplateKeys.LeaveSubmitted,
                    subject,
                    body,
                    request.Id,
                    cancellationToken);
            }
        }
        else
        {
            await NotifyHrPendingAsync(request, employee.FullName, cancellationToken);
        }
    }

    public async Task NotifyHrPendingAsync(LeaveRequest request, string employeeName, CancellationToken cancellationToken)
    {
        var hrUserIds = await _identity.GetUserIdsInRoleAsync(Roles.Hr, cancellationToken);
        var adminUserIds = await _identity.GetUserIdsInRoleAsync(Roles.Administrator, cancellationToken);
        var recipients = hrUserIds.Concat(adminUserIds).Distinct().ToList();
        var (subject, body) = LeaveNotificationComposer.PendingHr(request, employeeName);

        foreach (var userId in recipients)
        {
            await _notifications.QueueEmailAsync(
                userId,
                NotificationTemplateKeys.LeavePendingHr,
                subject,
                body,
                request.Id,
                cancellationToken);
        }
    }

    public async Task NotifyEmployeeApprovedAsync(LeaveRequest request, CancellationToken cancellationToken)
    {
        var userId = await GetEmployeeUserIdAsync(request.EmployeeId, cancellationToken);
        if (!userId.HasValue)
        {
            return;
        }

        var (subject, body) = LeaveNotificationComposer.Approved(request);
        await _notifications.QueueEmailAsync(
            userId.Value,
            NotificationTemplateKeys.LeaveApproved,
            subject,
            body,
            request.Id,
            cancellationToken);
    }

    public async Task NotifyEmployeeRejectedAsync(LeaveRequest request, string? reason, CancellationToken cancellationToken)
    {
        var userId = await GetEmployeeUserIdAsync(request.EmployeeId, cancellationToken);
        if (!userId.HasValue)
        {
            return;
        }

        var (subject, body) = LeaveNotificationComposer.Rejected(request, reason);
        await _notifications.QueueEmailAsync(
            userId.Value,
            NotificationTemplateKeys.LeaveRejected,
            subject,
            body,
            request.Id,
            cancellationToken);
    }

    public async Task NotifyCancelledAsync(LeaveRequest request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstAsync(x => x.Id == request.EmployeeId, cancellationToken);
        var (subject, body) = LeaveNotificationComposer.Cancelled(request, employee.FullName);

        if (employee.ManagerId.HasValue)
        {
            var managerUserId = await _db.Employees.AsNoTracking()
                .Where(x => x.Id == employee.ManagerId.Value)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (managerUserId.HasValue)
            {
                await _notifications.QueueEmailAsync(
                    managerUserId.Value,
                    NotificationTemplateKeys.LeaveCancelled,
                    subject,
                    body,
                    request.Id,
                    cancellationToken);
            }
        }

        await NotifyRoleUsersAsync(
            [Roles.Hr, Roles.Administrator],
            NotificationTemplateKeys.LeaveCancelled,
            subject,
            body,
            request.Id,
            cancellationToken);
    }

    public async Task NotifyPendingReminderAsync(
        LeaveRequest request,
        string employeeName,
        int daysPending,
        IEnumerable<Guid> approverUserIds,
        CancellationToken cancellationToken)
    {
        var (subject, body) = LeaveNotificationComposer.PendingReminder(request, employeeName, daysPending);
        foreach (var userId in approverUserIds.Distinct())
        {
            await _notifications.QueueEmailAsync(
                userId,
                NotificationTemplateKeys.LeavePendingReminder,
                subject,
                body,
                request.Id,
                cancellationToken);
        }
    }

    private async Task NotifyRoleUsersAsync(
        IEnumerable<string> roles,
        string templateKey,
        string subject,
        string body,
        Guid relatedEntityId,
        CancellationToken cancellationToken)
    {
        var userIds = new HashSet<Guid>();
        foreach (var role in roles)
        {
            foreach (var id in await _identity.GetUserIdsInRoleAsync(role, cancellationToken))
            {
                userIds.Add(id);
            }
        }

        foreach (var userId in userIds)
        {
            await _notifications.QueueEmailAsync(userId, templateKey, subject, body, relatedEntityId, cancellationToken);
        }
    }

    private async Task<Guid?> GetEmployeeUserIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await _db.Employees.AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync(cancellationToken);
}
