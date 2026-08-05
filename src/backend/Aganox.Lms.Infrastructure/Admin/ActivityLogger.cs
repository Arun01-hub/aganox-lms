using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Entities.System;
using Aganox.Lms.Persistence.Context;

namespace Aganox.Lms.Infrastructure.Admin;

public sealed class ActivityLogger : IActivityLogger
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivityLogger(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string? details = null, CancellationToken cancellationToken = default)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = _currentUser.UserId,
            Action = action,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
