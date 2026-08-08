using System.Globalization;
using System.Text;
using Aganox.Lms.Application.Common.Export.Templates;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Common.Export;

public static class TabularReportCsvBuilder
{
    public static byte[] Build(string[] headers, IReadOnlyList<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(EscapeCsv)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public static class PayrollReportRowFormatter
{
    public static string[] FormatRow(string reportType, IDictionary<string, object> row, ExportLanguage language)
    {
        var culture = CultureInfo.InvariantCulture;
        return reportType.ToLowerInvariant() switch
        {
            "register" =>
            [
                Convert.ToString(row["employeeCode"], culture) ?? string.Empty,
                Convert.ToString(row["employeeName"], culture) ?? string.Empty,
                Convert.ToDecimal(row["basicPay"], culture).ToString("0.00", culture),
                Convert.ToDecimal(row["grossPay"], culture).ToString("0.00", culture),
                Convert.ToDecimal(row["deductions"], culture).ToString("0.00", culture),
                Convert.ToDecimal(row["netPay"], culture).ToString("0.00", culture),
                Convert.ToDecimal(row["lopDays"], culture).ToString("0.##", culture)
            ],
            "components" =>
            [
                Convert.ToString(row["componentCode"], culture) ?? string.Empty,
                Convert.ToString(row["componentName"], culture) ?? string.Empty,
                Convert.ToDecimal(row["totalAmount"], culture).ToString("0.00", culture)
            ],
            "lop" =>
            [
                Convert.ToString(row["employeeCode"], culture) ?? string.Empty,
                Convert.ToString(row["employeeName"], culture) ?? string.Empty,
                Convert.ToDecimal(row["lopDays"], culture).ToString("0.##", culture),
                Convert.ToDecimal(row["netPay"], culture).ToString("0.00", culture)
            ],
            _ => row.Values.Select(v => Convert.ToString(v, culture) ?? string.Empty).ToArray()
        };
    }
}

public static class ExportCompanyName
{
    public static async Task<string> ResolveAsync(
        Common.Interfaces.IAppDbContext db,
        CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings.AsNoTracking()
            .Where(x => x.Key == Domain.Constants.AppSettingKeys.CompanyName)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(setting) || setting == "Aganox LMS"
            ? Domain.Constants.DefaultAppSettings.Values[Domain.Constants.AppSettingKeys.CompanyName]
            : setting;
    }
}
