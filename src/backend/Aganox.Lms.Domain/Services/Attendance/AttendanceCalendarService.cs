using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Services.Attendance;

public sealed record AttendanceMonthSummary(
    int Year,
    int Month,
    decimal WorkingDays,
    decimal PresentDays,
    decimal AbsentDays,
    decimal LeaveDays,
    decimal LopDays,
    decimal PaidDays);

public static class AttendanceCalendarService
{
    public static IEnumerable<DateOnly> EnumerateMonthDates(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    public static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public static AttendanceDayStatus ResolveDefaultStatus(
        DateOnly date,
        IReadOnlySet<DateOnly> holidays,
        bool skipWeekends,
        bool skipHolidays)
    {
        if (skipWeekends && IsWeekend(date))
        {
            return AttendanceDayStatus.WeekOff;
        }

        if (skipHolidays && holidays.Contains(date))
        {
            return AttendanceDayStatus.Holiday;
        }

        return AttendanceDayStatus.Present;
    }

    public static AttendanceMonthSummary Summarize(
        int year,
        int month,
        IReadOnlyDictionary<DateOnly, AttendanceDayStatus> dayStatuses,
        IReadOnlySet<DateOnly> unpaidLeaveDates)
    {
        decimal workingDays = 0;
        decimal presentDays = 0;
        decimal absentDays = 0;
        decimal leaveDays = 0;
        decimal lopDays = 0;

        foreach (var date in EnumerateMonthDates(year, month))
        {
            if (!dayStatuses.TryGetValue(date, out var status))
            {
                continue;
            }

            if (status is AttendanceDayStatus.WeekOff or AttendanceDayStatus.Holiday)
            {
                continue;
            }

            workingDays += status == AttendanceDayStatus.HalfDay ? 0.5m : 1m;

            switch (status)
            {
                case AttendanceDayStatus.Present:
                    presentDays += 1;
                    break;
                case AttendanceDayStatus.HalfDay:
                    presentDays += 0.5m;
                    absentDays += 0.5m;
                    if (unpaidLeaveDates.Contains(date))
                    {
                        lopDays += 0.5m;
                    }

                    break;
                case AttendanceDayStatus.Absent:
                    absentDays += 1;
                    lopDays += 1;
                    break;
                case AttendanceDayStatus.OnLeave:
                    leaveDays += 1;
                    if (unpaidLeaveDates.Contains(date))
                    {
                        lopDays += 1;
                    }
                    else
                    {
                        presentDays += 1;
                    }

                    break;
            }
        }

        var paidDays = workingDays - lopDays;
        if (paidDays < 0)
        {
            paidDays = 0;
        }

        return new AttendanceMonthSummary(
            year,
            month,
            workingDays,
            presentDays,
            absentDays,
            leaveDays,
            lopDays,
            paidDays);
    }
}
