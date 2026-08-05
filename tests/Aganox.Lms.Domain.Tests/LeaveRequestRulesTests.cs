using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;

namespace Aganox.Lms.Domain.Tests;

public class LeaveRequestRulesTests
{
    [Fact]
    public void Overlap_detects_pending_request_on_same_dates()
    {
        var existing = new[]
        {
            new LeaveRequestRange(Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 6), LeaveRequestStatus.Pending)
        };

        var hasOverlap = LeaveOverlapChecker.HasOverlap(
            new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 7), existing);

        Assert.True(hasOverlap);
    }

    [Fact]
    public void Overlap_ignores_cancelled_and_draft_requests()
    {
        var existing = new[]
        {
            new LeaveRequestRange(Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 6), LeaveRequestStatus.Cancelled),
            new LeaveRequestRange(Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 6), LeaveRequestStatus.Draft)
        };

        var hasOverlap = LeaveOverlapChecker.HasOverlap(
            new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 7), existing);

        Assert.False(hasOverlap);
    }

    [Fact]
    public void Overlap_excludes_current_request_when_editing()
    {
        var id = Guid.NewGuid();
        var existing = new[]
        {
            new LeaveRequestRange(id, new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 6), LeaveRequestStatus.Pending)
        };

        var hasOverlap = LeaveOverlapChecker.HasOverlap(
            new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 6), existing, id);

        Assert.False(hasOverlap);
    }

    [Theory]
    [InlineData(5, 3, false, true)]
    [InlineData(2, 3, false, false)]
    [InlineData(0, 1, true, true)]
    public void BalanceGuard_respects_remaining_and_negative_policy(
        decimal remaining, decimal requested, bool allowNegative, bool expected)
    {
        Assert.Equal(expected, LeaveBalanceGuard.HasSufficientBalance(remaining, requested, allowNegative));
    }
}
