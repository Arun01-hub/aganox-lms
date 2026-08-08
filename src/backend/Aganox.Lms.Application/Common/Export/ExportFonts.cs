using System.Reflection;
using QuestPDF.Drawing;

namespace Aganox.Lms.Application.Common.Export;

internal static class ExportFonts
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        lock (Sync)
        {
            if (_registered)
            {
                return;
            }

            RegisterEmbedded("Aganox.Lms.Application.Common.Export.Fonts.NotoSans-Regular.ttf");
            RegisterEmbedded("Aganox.Lms.Application.Common.Export.Fonts.NotoSans-Bold.ttf");
            RegisterEmbedded("Aganox.Lms.Application.Common.Export.Fonts.NotoSansTamil-Regular.ttf");
            RegisterEmbedded("Aganox.Lms.Application.Common.Export.Fonts.NotoSansTamil-Bold.ttf");
            _registered = true;
        }
    }

    public static string Regular(ExportLanguage language) =>
        language == ExportLanguage.Tamil ? "Noto Sans Tamil" : "Noto Sans";

    public static string Bold(ExportLanguage language) =>
        language == ExportLanguage.Tamil ? "Noto Sans Tamil" : "Noto Sans";

    private static void RegisterEmbedded(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Export font resource not found: {resourceName}");
        }

        FontManager.RegisterFont(stream);
    }
}
