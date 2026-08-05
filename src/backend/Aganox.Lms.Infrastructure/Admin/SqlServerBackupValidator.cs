using System.Text.RegularExpressions;

namespace Aganox.Lms.Infrastructure.Admin;

internal static partial class SqlServerBackupValidator
{
    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex SafeDatabaseNamePattern();

    public static bool IsValidDatabaseName(string? databaseName) =>
        !string.IsNullOrWhiteSpace(databaseName) && SafeDatabaseNamePattern().IsMatch(databaseName);

    public static bool TryNormalizeBackupPath(string path, out string fullPath, string? allowedRoot = null)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (!path.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        if (allowedRoot is not null)
        {
            var allowedFull = Path.GetFullPath(allowedRoot);
            if (!fullPath.StartsWith(allowedFull, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
