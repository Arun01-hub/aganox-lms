using Aganox.Lms.Domain.Services;

namespace Aganox.Lms.Domain.Tests;

public class LeaveDayCalculatorTests
{
    [Fact]
    public void Calculate_full_week_excluding_weekend()
    {
        // Mon 2026-08-03 to Fri 2026-08-07 = 5 days
        var result = LeaveDayCalculator.Calculate(
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 7),
            isHalfDay: false,
            skipWeekends: true,
            skipHolidays: false,
            holidays: new HashSet<DateOnly>());

        Assert.Equal(5m, result.NumberOfDays);
        Assert.Equal(0, result.SkippedWeekends);
    }

    [Fact]
    public void Calculate_skips_weekend_in_range()
    {
        // Fri 2026-08-07 to Mon 2026-08-10 = 2 days (Fri + Mon)
        var result = LeaveDayCalculator.Calculate(
            new DateOnly(2026, 8, 7),
            new DateOnly(2026, 8, 10),
            isHalfDay: false,
            skipWeekends: true,
            skipHolidays: false,
            holidays: new HashSet<DateOnly>());

        Assert.Equal(2m, result.NumberOfDays);
        Assert.Equal(2, result.SkippedWeekends);
    }

    [Fact]
    public void Calculate_skips_holiday()
    {
        var holidays = new HashSet<DateOnly> { new(2026, 8, 10) };
        var result = LeaveDayCalculator.Calculate(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 10),
            isHalfDay: false,
            skipWeekends: false,
            skipHolidays: true,
            holidays);

        Assert.Equal(0m, result.NumberOfDays);
        Assert.Equal(1, result.SkippedHolidays);
    }

    [Fact]
    public void Calculate_half_day_returns_point_five()
    {
        var result = LeaveDayCalculator.Calculate(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 10),
            isHalfDay: true,
            skipWeekends: true,
            skipHolidays: true,
            holidays: new HashSet<DateOnly>());

        Assert.Equal(0.5m, result.NumberOfDays);
        Assert.Single(result.Breakdown);
        Assert.True(result.Breakdown[0].Counted);
    }
}
