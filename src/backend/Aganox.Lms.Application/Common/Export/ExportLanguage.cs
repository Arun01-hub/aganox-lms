namespace Aganox.Lms.Application.Common.Export;

public enum ExportLanguage
{
    English,
    Tamil
}

public static class ExportLanguageParser
{
    public static ExportLanguage Parse(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return ExportLanguage.English;
        }

        return languageCode.Trim().StartsWith("ta", StringComparison.OrdinalIgnoreCase)
            ? ExportLanguage.Tamil
            : ExportLanguage.English;
    }
}
