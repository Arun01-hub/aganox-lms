namespace Aganox.Lms.Api;

internal static class ServerPaths
{
    public const string ServiceName = "AganoxLmsApi";

    public static string ProgramDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Aganox",
            "LMS");

    public static string ProductionConfigPath =>
        Path.Combine(ProgramDataRoot, "appsettings.Production.json");

    public static string LogsDirectory =>
        Path.Combine(ProgramDataRoot, "logs");
}
