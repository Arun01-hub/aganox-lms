using Aganox.Lms.Application.Features.Payroll.Payslips;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aganox.Lms.Application.Common.Export.Templates;

public sealed record PayslipTemplateModel(
    string CompanyName,
    PayslipDto Payslip,
    ExportLanguage Language);

public static class PayslipPdfTemplate
{
    private const string HeaderColor = "#1F4E79";
    private const string BorderColor = "#B4C6E7";
    private const string AltRowColor = "#F2F7FB";

    public static byte[] Generate(PayslipTemplateModel model)
    {
        ExportFonts.EnsureRegistered();
        QuestPDF.Settings.License = LicenseType.Community;

        var texts = ExportLocalization.Get(model.Language);
        var payslip = model.Payslip;
        var period = $"{payslip.Year}-{payslip.Month:D2}";
        var earnings = payslip.LineItems.Where(x => x.Category.Equals("Earning", StringComparison.OrdinalIgnoreCase)).ToList();
        var deductions = payslip.LineItems.Where(x => x.Category.Equals("Deduction", StringComparison.OrdinalIgnoreCase)).ToList();
        var fontRegular = ExportFonts.Regular(model.Language);
        var fontBold = ExportFonts.Bold(model.Language);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily(fontRegular).FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Background(HeaderColor).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(model.CompanyName).FontFamily(fontBold).FontSize(14).Bold().FontColor(Colors.White);
                            inner.Item().PaddingTop(4).Text(texts.PayslipTitle).FontFamily(fontBold).FontSize(11).FontColor(Colors.White);
                        });
                        row.ConstantItem(120).AlignRight().AlignMiddle().Column(inner =>
                        {
                            inner.Item().Text($"{texts.Period}: {period}").FontColor(Colors.White);
                            inner.Item().Text($"{texts.GeneratedOn}: {DateTime.UtcNow:yyyy-MM-dd}").FontSize(8).FontColor(Colors.White);
                        });
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Border(1).BorderColor(BorderColor).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        AddLabelValue(table, texts.EmployeeCode, payslip.EmployeeCode, texts.EmployeeName, payslip.EmployeeName, fontBold);
                        AddLabelValue(table, texts.Department, payslip.DepartmentName, texts.PaidDays, payslip.PaidDays.ToString("0.##"), fontBold);
                        AddLabelValue(table, texts.LopDays, payslip.LopDays.ToString("0.##"), string.Empty, string.Empty, fontBold);
                    });

                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Element(c => BuildLineItemsTable(c, texts.Earnings, earnings, model.Language, fontBold, fontRegular));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => BuildLineItemsTable(c, texts.Deductions, deductions, model.Language, fontBold, fontRegular));
                    });

                    col.Item().PaddingTop(12).Border(1).BorderColor(BorderColor).Background(AltRowColor).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text($"{texts.BasicPay}: {payslip.BasicPay:0.00}");
                        row.RelativeItem().Text($"{texts.GrossPay}: {payslip.GrossPay:0.00}");
                        row.RelativeItem().Text($"{texts.TotalDeductions}: {payslip.TotalDeductions:0.00}");
                        row.RelativeItem().AlignRight().Text($"{texts.NetPay}: {payslip.NetPay:0.00}")
                            .FontFamily(fontBold).Bold().FontSize(11);
                    });
                });

                page.Footer().AlignCenter().Text(texts.ComputerGenerated).FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }

    private static void AddLabelValue(
        TableDescriptor table,
        string label1,
        string value1,
        string label2,
        string value2,
        string fontBold)
    {
        table.Cell().Border(1).BorderColor(BorderColor).Background(AltRowColor).Padding(6)
            .Text(label1).FontFamily(fontBold).Bold();
        table.Cell().Border(1).BorderColor(BorderColor).Padding(6).Text(value1);
        if (!string.IsNullOrEmpty(label2))
        {
            table.Cell().Border(1).BorderColor(BorderColor).Background(AltRowColor).Padding(6)
                .Text(label2).FontFamily(fontBold).Bold();
            table.Cell().Border(1).BorderColor(BorderColor).Padding(6).Text(value2);
        }
        else
        {
            table.Cell().Border(1).BorderColor(BorderColor).Padding(6);
            table.Cell().Border(1).BorderColor(BorderColor).Padding(6);
        }
    }

    private static void BuildLineItemsTable(
        IContainer container,
        string title,
        IReadOnlyList<Features.Payroll.Processing.PayrollLineItemDto> items,
        ExportLanguage language,
        string fontBold,
        string fontRegular)
    {
        container.Border(1).BorderColor(BorderColor).Column(col =>
        {
            col.Item().Background("#1F4E79").Padding(6).Text(title).FontFamily(fontBold).Bold().FontColor(Colors.White);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                var texts = ExportLocalization.Get(language);
                table.Header(header =>
                {
                    header.Cell().Background(AltRowColor).Padding(4).Text(texts.Component).FontFamily(fontBold).Bold();
                    header.Cell().Background(AltRowColor).Padding(4).AlignRight().Text(texts.Amount).FontFamily(fontBold).Bold();
                });

                if (items.Count == 0)
                {
                    table.Cell().ColumnSpan(2).Padding(8).AlignCenter().Text("-").FontFamily(fontRegular);
                }
                else
                {
                    foreach (var item in items)
                    {
                        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).Text(item.ComponentName);
                        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).AlignRight().Text(item.Amount.ToString("0.00"));
                    }
                }
            });
        });
    }
}
