using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Services;

public static class LeaveOverlapChecker
{
    private static readonly LeaveRequestStatus[] BlockingStatuses =
    [
        LeaveRequestStatus.Pending,
        LeaveRequestStatus.Approved
    ];

    public static bool HasOverlap(
        DateOnly fromDate,
        DateOnly toDate,
        IEnumerable<LeaveRequestRange> existingRequests,
        Guid? excludeRequestId = null)
    {
        foreach (var existing in existingRequests)
        {
            if (excludeRequestId.HasValue && existing.Id == excludeRequestId.Value)
            {
                continue;
            }

            if (!BlockingStatuses.Contains(existing.Status))
            {
                continue;
            }

            if (fromDate <= existing.ToDate && toDate >= existing.FromDate)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed record LeaveRequestRange(Guid Id, DateOnly FromDate, DateOnly ToDate, LeaveRequestStatus Status);

public static class LeaveBalanceGuard
{
    public static bool HasSufficientBalance(decimal remaining, decimal requestedDays, bool allowNegativeBalance) =>
        allowNegativeBalance || remaining >= requestedDays;
}
