using ClosedXML.Excel;

namespace Aganox.Lms.Application.Common.Export.Templates;

public static class TabularReportExcelTemplate
{
    private const string HeaderColor = "#1F4E79";
    private const string AltRowColor = "#F2F7FB";
    private const string BorderColor = "#B4C6E7";

    public static byte[] Generate(TabularReportModel model)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");
        var texts = ExportLocalization.Get(model.Language);
        var columnCount = Math.Max(model.Headers.Length, 1);
        var lastColumn = columnCount;

        sheet.Cell(1, 1).Value = model.CompanyName;
        sheet.Range(1, 1, 1, lastColumn).Merge();
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderColor);
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

        sheet.Cell(2, 1).Value = model.Title;
        sheet.Range(2, 1, 2, lastColumn).Merge();
        sheet.Cell(2, 1).Style.Font.Bold = true;
        sheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (!string.IsNullOrWhiteSpace(model.Subtitle))
        {
            sheet.Cell(3, 1).Value = model.Subtitle;
            sheet.Range(3, 1, 3, lastColumn).Merge();
            sheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var metaRow = string.IsNullOrWhiteSpace(model.Subtitle) ? 3 : 4;
        sheet.Cell(metaRow, 1).Value = $"{texts.GeneratedOn}: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        sheet.Range(metaRow, 1, metaRow, lastColumn).Merge();
        sheet.Cell(metaRow, 1).Style.Font.Italic = true;
        sheet.Cell(metaRow, 1).Style.Font.FontColor = XLColor.Gray;

        var headerRow = metaRow + 2;
        for (var c = 0; c < model.Headers.Length; c++)
        {
            var cell = sheet.Cell(headerRow, c + 1);
            cell.Value = model.Headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderColor);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderColor);
        }

        for (var r = 0; r < model.Rows.Count; r++)
        {
            var excelRow = headerRow + 1 + r;
            var bg = r % 2 == 0 ? XLColor.White : XLColor.FromHtml(AltRowColor);
            for (var c = 0; c < columnCount; c++)
            {
                var value = c < model.Rows[r].Length ? model.Rows[r][c] : string.Empty;
                var cell = sheet.Cell(excelRow, c + 1);
                cell.Value = value;
                cell.Style.Fill.BackgroundColor = bg;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderColor);
            }
        }

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, lastColumn).AdjustToContents();
        sheet.Row(1).Height = 24;
        sheet.Row(headerRow).Height = 20;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
