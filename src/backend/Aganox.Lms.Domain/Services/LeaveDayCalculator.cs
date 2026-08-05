namespace Aganox.Lms.Domain.Services;

public sealed record LeaveDayBreakdown(DateOnly Date, bool Counted, string? Reason);

public sealed record LeaveDayCalculationResult(
    decimal NumberOfDays,
    int SkippedWeekends,
    int SkippedHolidays,
    IReadOnlyList<LeaveDayBreakdown> Breakdown);

public static class LeaveDayCalculator
{
    public static LeaveDayCalculationResult Calculate(
        DateOnly fromDate,
        DateOnly toDate,
        bool isHalfDay,
        bool skipWeekends,
        bool skipHolidays,
        IReadOnlySet<DateOnly> holidays)
    {
        if (toDate < fromDate)
        {
            throw new ArgumentException("To date must be on or after from date.");
        }

        if (isHalfDay)
        {
            if (fromDate != toDate)
            {
                throw new ArgumentException("Half-day leave must use the same from and to date.");
            }

            return new LeaveDayCalculationResult(
                0.5m,
                0,
                0,
                [new LeaveDayBreakdown(fromDate, true, null)]);
        }

        var breakdown = new List<LeaveDayBreakdown>();
        var skippedWeekends = 0;
        var skippedHolidays = 0;
        decimal days = 0;

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (skipWeekends && date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                skippedWeekends++;
                breakdown.Add(new LeaveDayBreakdown(date, false, "Weekend"));
                continue;
            }

            if (skipHolidays && holidays.Contains(date))
            {
                skippedHolidays++;
                breakdown.Add(new LeaveDayBreakdown(date, false, "Holiday"));
                continue;
            }

            days++;
            breakdown.Add(new LeaveDayBreakdown(date, true, null));
        }

        return new LeaveDayCalculationResult(days, skippedWeekends, skippedHolidays, breakdown);
    }
}
