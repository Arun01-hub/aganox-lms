using System.Text;
using Aganox.Lms.Application.Common.Export;
using Aganox.Lms.Application.Common.Export.Templates;
using Aganox.Lms.Application.Features.Payroll.Payslips;
using Aganox.Lms.Application.Features.Payroll.Processing;

namespace Aganox.Lms.Application.Tests;

public class ExportTemplateTests
{
    private static PayslipDto SamplePayslip => new(
        Guid.NewGuid(),
        2026,
        8,
        Guid.NewGuid(),
        "Arun Kumar",
        "EMP001",
        "Engineering",
        25000m,
        32000m,
        2000m,
        30000m,
        26m,
        0m,
        [
            new PayrollLineItemDto("BASIC", "Basic Pay", "Earning", 25000m, 1),
            new PayrollLineItemDto("HRA", "House Rent", "Earning", 7000m, 2),
            new PayrollLineItemDto("PF", "Provident Fund", "Deduction", 2000m, 3)
        ]);

    [Theory]
    [InlineData("en")]
    [InlineData("ta")]
    public void PayslipPdfTemplate_generates_valid_pdf(string languageCode)
    {
        var language = ExportLanguageParser.Parse(languageCode);
        var bytes = PayslipPdfTemplate.Generate(new PayslipTemplateModel("MPCS", SamplePayslip, language));

        var header = Encoding.ASCII.GetString(bytes.AsSpan(0, 4));
        var footer = Encoding.ASCII.GetString(bytes);

        Assert.Equal("%PDF", header);
        Assert.Contains("%%EOF", footer);
        Assert.True(bytes.Length > 1024);
    }

    [Fact]
    public void TabularReportPdfTemplate_uses_tamil_headers()
    {
        var headers = ExportLocalization.GetPayrollReportHeaders(ExportLanguage.Tamil, "register");
        var model = new TabularReportModel(
            "MPCS",
            ExportLocalization.GetPayrollReportTitle(ExportLanguage.Tamil, "register"),
            "காலம்: 2026-08",
            headers,
            [["EMP001", "Arun Kumar", "25000.00", "32000.00", "2000.00", "30000.00", "0"]],
            ExportLanguage.Tamil);

        var bytes = TabularReportPdfTemplate.Generate(model);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes.AsSpan(0, 4)));
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes));
    }

    [Fact]
    public void TabularReportExcelTemplate_generates_xlsx()
    {
        var headers = ExportLocalization.GetLeaveReportHeaders(ExportLanguage.English, "trends");
        var model = new TabularReportModel(
            "MPCS",
            "Leave Trends",
            null,
            headers,
            [["2026-01", "3", "5"]],
            ExportLanguage.English);

        var bytes = TabularReportExcelTemplate.Generate(model);

        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }
}
