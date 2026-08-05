namespace Aganox.Lms.Application.Common.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(string action, string? details = null, CancellationToken cancellationToken = default);
}

public interface IDatabaseBackupService
{
    Task<(bool Succeeded, string? FilePath, string? Error)> CreateBackupAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);

    bool IsSupported { get; }
}
