using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aganox.Lms.Application.Common.Export.Templates;

public sealed record TabularReportModel(
    string CompanyName,
    string Title,
    string? Subtitle,
    string[] Headers,
    IReadOnlyList<string[]> Rows,
    ExportLanguage Language);

public static class TabularReportPdfTemplate
{
    private const string HeaderColor = "#1F4E79";
    private const string BorderColor = "#B4C6E7";
    private const string AltRowColor = "#F2F7FB";

    public static byte[] Generate(TabularReportModel model)
    {
        ExportFonts.EnsureRegistered();
        QuestPDF.Settings.License = LicenseType.Community;

        var texts = ExportLocalization.Get(model.Language);
        var fontRegular = ExportFonts.Regular(model.Language);
        var fontBold = ExportFonts.Bold(model.Language);
        var columnCount = Math.Max(model.Headers.Length, 1);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontFamily(fontRegular).FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Background(HeaderColor).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(model.CompanyName).FontFamily(fontBold).FontSize(13).Bold().FontColor(Colors.White);
                            inner.Item().PaddingTop(2).Text(model.Title).FontFamily(fontBold).FontSize(10).FontColor(Colors.White);
                            if (!string.IsNullOrWhiteSpace(model.Subtitle))
                            {
                                inner.Item().Text(model.Subtitle).FontSize(8).FontColor(Colors.White);
                            }
                        });
                        row.ConstantItem(140).AlignRight().AlignMiddle()
                            .Text($"{texts.GeneratedOn}: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(8).FontColor(Colors.White);
                    });
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < columnCount; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var h in model.Headers)
                        {
                            header.Cell().Background(HeaderColor).Padding(5)
                                .Text(h).FontFamily(fontBold).Bold().FontColor(Colors.White);
                        }
                    });

                    for (var r = 0; r < model.Rows.Count; r++)
                    {
                        var bg = r % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        var row = model.Rows[r];
                        for (var c = 0; c < columnCount; c++)
                        {
                            var value = c < row.Length ? row[c] : string.Empty;
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderColor).Padding(4).Text(value);
                        }
                    }

                    if (model.Rows.Count == 0)
                    {
                        table.Cell().ColumnSpan((uint)columnCount).Padding(12).AlignCenter().Text("-");
                    }
                });

                page.Footer().AlignCenter().Text(texts.ComputerGenerated).FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }
}
