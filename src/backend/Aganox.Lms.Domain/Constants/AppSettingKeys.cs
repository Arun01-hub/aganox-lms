namespace Aganox.Lms.Domain.Constants;

public static class AppSettingKeys
{
    public const string CompanyName = "Company.Name";
    public const string SmtpFromEmail = "Smtp.FromEmail";
    public const string BackupFolderPath = "Backup.FolderPath";
    public const string BackupScheduleEnabled = "Backup.ScheduleEnabled";
    public const string BackupScheduleIntervalHours = "Backup.ScheduleIntervalHours";
}

public static class DefaultAppSettings
{
    public static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>
    {
        [AppSettingKeys.CompanyName] = "MPCS",
        [AppSettingKeys.SmtpFromEmail] = "noreply@aganox.local",
        [AppSettingKeys.BackupFolderPath] = "backups",
        [AppSettingKeys.BackupScheduleEnabled] = "false",
        [AppSettingKeys.BackupScheduleIntervalHours] = "24"
    };
}
