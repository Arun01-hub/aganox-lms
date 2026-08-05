using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Entities.System;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Persistence.Context;

namespace Aganox.Lms.Infrastructure.Notifications;

public sealed class NotificationQueueService : INotificationQueueService
{
    private readonly AppDbContext _db;

    public NotificationQueueService(AppDbContext db) => _db = db;

    public async Task QueueEmailAsync(
        Guid userId,
        string templateKey,
        string subject,
        string body,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Channel = NotificationChannel.Email,
            TemplateKey = templateKey,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            RelatedEntityId = relatedEntityId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
