using System.Text.Json;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Services.Payroll;

public sealed record SalaryRuleParameters(
    decimal Rate = 0,
    decimal FixedAmount = 0,
    decimal MinAmount = 0,
    decimal MaxAmount = 0,
    string? Expression = null)
{
    public static SalaryRuleParameters Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SalaryRuleParameters();
        }

        try
        {
            return JsonSerializer.Deserialize<SalaryRuleParameters>(json, JsonOptions)
                   ?? new SalaryRuleParameters();
        }
        catch (JsonException)
        {
            return new SalaryRuleParameters();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

public sealed record ResolvedSalaryRule(
    Guid ComponentId,
    string ComponentCode,
    string ComponentNameEn,
    SalaryComponentCategory Category,
    SalaryCalculationType CalculationType,
    string? BaseComponentCode,
    int SortOrder,
    bool IncludeInGross,
    SalaryRuleParameters Parameters,
    decimal? OverrideAmount);

public static class SalaryRuleResolver
{
    public static IReadOnlyList<ResolvedSalaryRule> Resolve(
        DateOnly effectiveDate,
        IEnumerable<SalaryComponentRuleSource> components,
        IReadOnlyDictionary<string, decimal>? overridesByCode = null)
    {
        var list = new List<ResolvedSalaryRule>();

        foreach (var component in components.Where(c => c.IsActive).OrderBy(c => c.SortOrder))
        {
            var rule = component.Rules
                .Where(r => r.IsActive && r.EffectiveFrom <= effectiveDate &&
                            (r.EffectiveTo is null || r.EffectiveTo >= effectiveDate))
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();

            if (rule is null)
            {
                continue;
            }

            decimal? overrideAmount = null;
            if (overridesByCode is not null &&
                overridesByCode.TryGetValue(component.Code, out var overrideValue))
            {
                overrideAmount = overrideValue;
            }

            list.Add(new ResolvedSalaryRule(
                component.Id,
                component.Code,
                component.NameEn,
                component.Category,
                component.CalculationType,
                component.BaseComponentCode,
                component.SortOrder,
                component.IncludeInGross,
                SalaryRuleParameters.Parse(rule.ParametersJson),
                overrideAmount));
        }

        return list;
    }
}

public sealed record SalaryComponentRuleSource(
    Guid Id,
    string Code,
    string NameEn,
    SalaryComponentCategory Category,
    SalaryCalculationType CalculationType,
    string? BaseComponentCode,
    int SortOrder,
    bool IncludeInGross,
    bool IsActive,
    IReadOnlyList<SalaryRuleSource> Rules);

public sealed record SalaryRuleSource(
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int Priority,
    string ParametersJson);

public sealed record PayrollLineResult(
    string ComponentCode,
    string ComponentName,
    SalaryComponentCategory Category,
    decimal Amount,
    int SortOrder);

public sealed record PayrollCalculationResult(
    decimal BasicPay,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    decimal LopDeduction,
    IReadOnlyList<PayrollLineResult> Lines);

public static class PayrollCalculationEngine
{
    public static PayrollCalculationResult Calculate(
        decimal basicPay,
        AttendanceMonthSummaryInput attendance,
        IReadOnlyList<ResolvedSalaryRule> rules)
    {
        var amounts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["BASIC"] = basicPay
        };

        var lines = new List<PayrollLineResult>();
        var gross = basicPay;

        foreach (var rule in rules.Where(r => r.Category == SalaryComponentCategory.Earning).OrderBy(r => r.SortOrder))
        {
            decimal amount;
            if (rule.ComponentCode.Equals("BASIC", StringComparison.OrdinalIgnoreCase))
            {
                amount = basicPay;
            }
            else
            {
                amount = EvaluateAmount(rule, amounts, gross, basicPay);
                if (rule.IncludeInGross)
                {
                    gross += amount;
                }
            }

            amounts[rule.ComponentCode] = amount;
            lines.Add(new PayrollLineResult(
                rule.ComponentCode,
                rule.ComponentNameEn,
                rule.Category,
                amount,
                rule.SortOrder));
        }

        amounts["GROSS"] = gross;
        decimal totalDeductions = 0;

        foreach (var rule in rules.Where(r => r.Category == SalaryComponentCategory.Deduction).OrderBy(r => r.SortOrder))
        {
            var amount = EvaluateAmount(rule, amounts, gross, basicPay);
            totalDeductions += amount;
            lines.Add(new PayrollLineResult(
                rule.ComponentCode,
                rule.ComponentNameEn,
                rule.Category,
                amount,
                rule.SortOrder));
        }

        var lopDeduction = 0m;
        if (attendance.WorkingDays > 0 && attendance.LopDays > 0)
        {
            lopDeduction = Math.Round(basicPay / attendance.WorkingDays * attendance.LopDays, 2, MidpointRounding.AwayFromZero);
            totalDeductions += lopDeduction;
            lines.Add(new PayrollLineResult("LOP", "Loss of Pay", SalaryComponentCategory.Deduction, lopDeduction, 999));
        }

        var net = gross - totalDeductions;
        return new PayrollCalculationResult(basicPay, gross, totalDeductions, net, lopDeduction, lines);
    }

    public static (decimal GrossPay, decimal TotalDeductions, decimal NetPay) ComputeTotalsFromLineItems(
        IEnumerable<(SalaryComponentCategory Category, decimal Amount)> lines)
    {
        var gross = lines.Where(x => x.Category == SalaryComponentCategory.Earning).Sum(x => x.Amount);
        var deductions = lines.Where(x => x.Category == SalaryComponentCategory.Deduction).Sum(x => x.Amount);
        return (gross, deductions, gross - deductions);
    }

    private static decimal EvaluateAmount(
        ResolvedSalaryRule rule,
        IReadOnlyDictionary<string, decimal> amounts,
        decimal gross,
        decimal basicPay)
    {
        if (rule.OverrideAmount.HasValue)
        {
            return Clamp(rule.OverrideAmount.Value, rule.Parameters);
        }

        decimal raw = rule.CalculationType switch
        {
            SalaryCalculationType.Fixed => rule.Parameters.FixedAmount,
            SalaryCalculationType.PercentOfBasic => basicPay * rule.Parameters.Rate,
            SalaryCalculationType.PercentOfGross => gross * rule.Parameters.Rate,
            SalaryCalculationType.PercentOfComponent => ResolveBaseAmount(rule, amounts) * rule.Parameters.Rate,
            SalaryCalculationType.Expression => EvaluateExpression(rule.Parameters.Expression, amounts, gross, basicPay),
            _ => 0
        };

        return Clamp(raw, rule.Parameters);
    }

    private static decimal ResolveBaseAmount(ResolvedSalaryRule rule, IReadOnlyDictionary<string, decimal> amounts)
    {
        if (string.IsNullOrWhiteSpace(rule.BaseComponentCode))
        {
            return 0;
        }

        return amounts.TryGetValue(rule.BaseComponentCode, out var value) ? value : 0;
    }

    private static decimal Clamp(decimal value, SalaryRuleParameters parameters)
    {
        if (parameters.MinAmount > 0 && value < parameters.MinAmount)
        {
            value = parameters.MinAmount;
        }

        if (parameters.MaxAmount > 0 && value > parameters.MaxAmount)
        {
            value = parameters.MaxAmount;
        }

        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal EvaluateExpression(
        string? expression,
        IReadOnlyDictionary<string, decimal> amounts,
        decimal gross,
        decimal basicPay)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return 0;
        }

        var normalized = expression
            .Replace("BASIC", basicPay.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("GROSS", gross.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        foreach (var pair in amounts)
        {
            normalized = normalized.Replace(
                pair.Key,
                pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[\d.\+\-\*/\(\)\s]+$"))
        {
            return 0;
        }

        try
        {
            return Convert.ToDecimal(new System.Data.DataTable().Compute(normalized, null));
        }
        catch
        {
            return 0;
        }
    }
}

public sealed record AttendanceMonthSummaryInput(
    decimal WorkingDays,
    decimal LopDays,
    decimal PaidDays);

public static class LopCalculator
{
    public static decimal CalculateLopDeduction(decimal basicPay, decimal workingDays, decimal lopDays)
    {
        if (workingDays <= 0 || lopDays <= 0)
        {
            return 0;
        }

        return Math.Round(basicPay / workingDays * lopDays, 2, MidpointRounding.AwayFromZero);
    }
}

public static class PayrollSnapshotBuilder
{
    public static string BuildInputSnapshot(object input) =>
        JsonSerializer.Serialize(input, SnapshotOptions);

    public static string BuildOutputSnapshot(object output) =>
        JsonSerializer.Serialize(output, SnapshotOptions);

    public static string BuildCalculatedSnapshot(PayrollEmployeeResult result) =>
        BuildInputSnapshot(new
        {
            result.BasicPaySnapshot,
            result.GrossPay,
            result.TotalDeductions,
            result.NetPay,
            result.PaidDays,
            result.LopDays,
            lineItems = result.LineItems
                .OrderBy(x => x.SortOrder)
                .Select(x => new
                {
                    x.ComponentCode,
                    x.ComponentName,
                    category = x.Category.ToString(),
                    x.Amount,
                    x.SortOrder
                })
        });

    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

public static class PayrollRunGuard
{
    public static bool CanRecalculate(PayrollRunStatus status) =>
        status is not PayrollRunStatus.Locked;

    public static void ResetToDraft(PayrollRun run)
    {
        run.Status = PayrollRunStatus.Draft;
        run.CalculatedAt = null;
        run.LockedAt = null;
        run.LockedByUserId = null;
        ResetApprovalState(run);
    }

    public static void ResetApprovalState(PayrollRun run)
    {
        run.ApprovedAt = null;
        run.ApprovedByUserId = null;
    }

    public static bool CanEditSalaryEntry(PayrollRunStatus status) =>
        status is PayrollRunStatus.Calculated or PayrollRunStatus.PendingApproval;

    public static bool CanApprove(PayrollRunStatus status) =>
        status is PayrollRunStatus.Calculated or PayrollRunStatus.PendingApproval;

    public static bool CanLock(PayrollRunStatus status) =>
        status is PayrollRunStatus.Approved;

    public static bool IsImmutable(PayrollRunStatus status) =>
        status is PayrollRunStatus.Locked;

    public static bool HasPayslipData(PayrollRunStatus status) =>
        status is not PayrollRunStatus.Draft;

    public static bool CanViewPayslip(PayrollRunStatus status, bool canViewPreviewPayslips) =>
        HasPayslipData(status) && (canViewPreviewPayslips || status is PayrollRunStatus.Locked);
}
