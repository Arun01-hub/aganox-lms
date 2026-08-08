using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services.Attendance;
using Aganox.Lms.Domain.Services.Payroll;

namespace Aganox.Lms.Domain.Tests;

public class AttendanceCalendarServiceTests
{
    [Fact]
    public void Summarize_CountsLopForUnpaidLeaveAndAbsentDays()
    {
        var statuses = new Dictionary<DateOnly, AttendanceDayStatus>
        {
            [new DateOnly(2026, 8, 4)] = AttendanceDayStatus.Present,
            [new DateOnly(2026, 8, 5)] = AttendanceDayStatus.Absent,
            [new DateOnly(2026, 8, 6)] = AttendanceDayStatus.OnLeave
        };
        var unpaid = new HashSet<DateOnly> { new(2026, 8, 6) };

        var summary = AttendanceCalendarService.Summarize(2026, 8, statuses, unpaid);

        Assert.Equal(3, summary.WorkingDays);
        Assert.Equal(1, summary.PresentDays);
        Assert.Equal(1, summary.AbsentDays);
        Assert.Equal(1, summary.LeaveDays);
        Assert.Equal(2, summary.LopDays);
        Assert.Equal(1, summary.PaidDays);
    }
}

public class PayrollCalculationEngineTests
{
    [Fact]
    public void Calculate_AppliesPercentRulesAndLopDeduction()
    {
        var rules = new List<ResolvedSalaryRule>
        {
            new(Guid.NewGuid(), "DA", "DA", SalaryComponentCategory.Earning, SalaryCalculationType.PercentOfBasic,
                null, 2, true, new SalaryRuleParameters(Rate: 0.1m), null),
            new(Guid.NewGuid(), "PF", "PF", SalaryComponentCategory.Deduction, SalaryCalculationType.PercentOfBasic,
                null, 10, false, new SalaryRuleParameters(Rate: 0.12m), null)
        };

        var result = PayrollCalculationEngine.Calculate(
            10000,
            new AttendanceMonthSummaryInput(22, 2, 20),
            rules);

        Assert.Equal(10000, result.BasicPay);
        Assert.Equal(11000, result.GrossPay);
        Assert.True(result.TotalDeductions >= 1200 + result.LopDeduction);
        Assert.True(result.NetPay < result.GrossPay);
        Assert.Contains(result.Lines, x => x.ComponentCode == "LOP");
    }

    [Fact]
    public void Calculate_SetsBasicLineAmountFromEmployeeBasicPay()
    {
        var rules = new List<ResolvedSalaryRule>
        {
            new(Guid.NewGuid(), "BASIC", "Basic Pay", SalaryComponentCategory.Earning, SalaryCalculationType.Fixed,
                null, 1, true, new SalaryRuleParameters(FixedAmount: 0), null),
            new(Guid.NewGuid(), "DA", "DA", SalaryComponentCategory.Earning, SalaryCalculationType.PercentOfBasic,
                null, 2, true, new SalaryRuleParameters(Rate: 0.1m), null)
        };

        var result = PayrollCalculationEngine.Calculate(
            100000,
            new AttendanceMonthSummaryInput(22, 0, 22),
            rules);

        var basicLine = Assert.Single(result.Lines, x => x.ComponentCode == "BASIC");
        Assert.Equal(100000, basicLine.Amount);
        Assert.Equal(100000, result.BasicPay);
        Assert.Equal(110000, result.GrossPay);
    }
}

public class SalaryRuleResolverTests
{
    [Fact]
    public void Resolve_PicksHighestPriorityActiveRuleForDate()
    {
        var componentId = Guid.NewGuid();
        var sources = new[]
        {
            new SalaryComponentRuleSource(
                componentId,
                "DA",
                "DA",
                SalaryComponentCategory.Earning,
                SalaryCalculationType.PercentOfBasic,
                null,
                1,
                true,
                true,
                [
                    new SalaryRuleSource(true, new DateOnly(2026, 1, 1), null, 1, """{"rate":0.05}"""),
                    new SalaryRuleSource(true, new DateOnly(2026, 1, 1), null, 5, """{"rate":0.12}""")
                ])
        };

        var resolved = SalaryRuleResolver.Resolve(new DateOnly(2026, 8, 1), sources);
        Assert.Single(resolved);
        Assert.Equal(0.12m, resolved[0].Parameters.Rate);
    }
}

public class PayrollRunGuardTests
{
    [Fact]
    public void IsImmutable_OnlyWhenLocked()
    {
        Assert.True(PayrollRunGuard.IsImmutable(PayrollRunStatus.Locked));
        Assert.False(PayrollRunGuard.IsImmutable(PayrollRunStatus.Approved));
    }

    [Fact]
    public void CanRecalculate_WhenNotLocked()
    {
        Assert.True(PayrollRunGuard.CanRecalculate(PayrollRunStatus.Draft));
        Assert.True(PayrollRunGuard.CanRecalculate(PayrollRunStatus.Calculated));
        Assert.True(PayrollRunGuard.CanRecalculate(PayrollRunStatus.PendingApproval));
        Assert.True(PayrollRunGuard.CanRecalculate(PayrollRunStatus.Approved));
        Assert.False(PayrollRunGuard.CanRecalculate(PayrollRunStatus.Locked));
    }

    [Fact]
    public void CanViewPayslip_AllowsPreviewForCalculatedWhenPermitted()
    {
        Assert.True(PayrollRunGuard.CanViewPayslip(PayrollRunStatus.Calculated, canViewPreviewPayslips: true));
        Assert.False(PayrollRunGuard.CanViewPayslip(PayrollRunStatus.Calculated, canViewPreviewPayslips: false));
        Assert.True(PayrollRunGuard.CanViewPayslip(PayrollRunStatus.Locked, canViewPreviewPayslips: false));
        Assert.False(PayrollRunGuard.CanViewPayslip(PayrollRunStatus.Draft, canViewPreviewPayslips: true));
    }

    [Fact]
    public void CanEditSalaryEntry_WhenCalculatedOrPendingApproval()
    {
        Assert.True(PayrollRunGuard.CanEditSalaryEntry(PayrollRunStatus.Calculated));
        Assert.True(PayrollRunGuard.CanEditSalaryEntry(PayrollRunStatus.PendingApproval));
        Assert.False(PayrollRunGuard.CanEditSalaryEntry(PayrollRunStatus.Approved));
        Assert.False(PayrollRunGuard.CanEditSalaryEntry(PayrollRunStatus.Locked));
    }
}

public class PayrollLineItemTotalsTests
{
    [Fact]
    public void ComputeTotalsFromLineItems_SumsEarningsAndDeductions()
    {
        var lines = new (SalaryComponentCategory Category, decimal Amount)[]
        {
            (SalaryComponentCategory.Earning, 10000),
            (SalaryComponentCategory.Earning, 1000),
            (SalaryComponentCategory.Deduction, 1200),
            (SalaryComponentCategory.Deduction, 200)
        };

        var (gross, deductions, net) = PayrollCalculationEngine.ComputeTotalsFromLineItems(lines);

        Assert.Equal(11000, gross);
        Assert.Equal(1400, deductions);
        Assert.Equal(9600, net);
    }
}
