using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Admin;

public static class AdminSettingsInitializer
{
    public static async Task EnsureDefaultsAsync(IAppDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.AppSettings.AsNoTracking().Select(x => x.Key).ToListAsync(cancellationToken);
        var added = false;
        foreach (var (key, value) in DefaultAppSettings.Values)
        {
            if (!existing.Contains(key))
            {
                db.AppSettings.Add(new AppSetting { Key = key, Value = value });
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
