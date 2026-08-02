using Aganox.Lms.Domain.Entities.Leave;

namespace Aganox.Lms.Domain.Tests;

public class LeaveBalanceTests
{
    [Fact]
    public void RecalculateRemaining_ComputesOpeningCreditedAdjustedMinusConsumed()
    {
        var balance = new LeaveBalance
        {
            OpeningBalance = 12,
            Credited = 2,
            Adjusted = 1,
            Consumed = 3
        };

        balance.RecalculateRemaining();

        Assert.Equal(12, balance.Remaining);
    }
}
