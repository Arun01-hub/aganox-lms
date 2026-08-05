using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aganox.Lms.Infrastructure.Admin;

public sealed class SqlServerBackupService : IDatabaseBackupService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public SqlServerBackupService(AppDbContext db, IConfiguration configuration, IHostEnvironment environment)
    {
        _db = db;
        _configuration = configuration;
        _environment = environment;
    }

    public bool IsSupported =>
        !_configuration.GetValue<bool>("UseInMemoryDatabase") &&
        _db.Database.IsSqlServer();

    public async Task<(bool Succeeded, string? FilePath, string? Error)> CreateBackupAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not configured.");
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;
            if (!SqlServerBackupValidator.IsValidDatabaseName(databaseName))
            {
                return (false, null, "Invalid database name in connection string.");
            }

            var folderSetting = await _db.AppSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == AppSettingKeys.BackupFolderPath, cancellationToken);
            var folder = folderSetting?.Value ?? DefaultAppSettings.Values[AppSettingKeys.BackupFolderPath];
            var backupDir = Path.IsPathRooted(folder)
                ? folder
                : Path.Combine(_environment.ContentRootPath, folder);
            Directory.CreateDirectory(backupDir);

            var filePath = Path.Combine(backupDir, $"{databaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak");
            await _db.Database.ExecuteSqlRawAsync(
                $"BACKUP DATABASE [{databaseName}] TO DISK = {{0}} WITH FORMAT, INIT, NAME = N'Aganox LMS Backup'",
                [filePath],
                cancellationToken);

            return (true, filePath, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Succeeded, string? Error)> RestoreBackupAsync(
        string backupFilePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not configured.");
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;
            if (!SqlServerBackupValidator.IsValidDatabaseName(databaseName))
            {
                return (false, "Invalid database name in connection string.");
            }

            var folderSetting = await _db.AppSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == AppSettingKeys.BackupFolderPath, cancellationToken);
            var folder = folderSetting?.Value ?? DefaultAppSettings.Values[AppSettingKeys.BackupFolderPath];
            var backupDir = Path.IsPathRooted(folder)
                ? folder
                : Path.Combine(_environment.ContentRootPath, folder);

            if (!SqlServerBackupValidator.TryNormalizeBackupPath(backupFilePath, out var normalizedPath, backupDir))
            {
                return (false, "Backup file path is invalid or outside the configured backup folder.");
            }

            if (!File.Exists(normalizedPath))
            {
                return (false, "Backup file not found.");
            }

            await _db.Database.ExecuteSqlRawAsync(
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                cancellationToken);
            await _db.Database.ExecuteSqlRawAsync(
                $"RESTORE DATABASE [{databaseName}] FROM DISK = {{0}} WITH REPLACE",
                [normalizedPath],
                cancellationToken);
            await _db.Database.ExecuteSqlRawAsync(
                $"ALTER DATABASE [{databaseName}] SET MULTI_USER",
                cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
