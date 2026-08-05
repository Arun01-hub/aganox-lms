using Aganox.Lms.Application.Features.LeaveRequests;
using FluentValidation.TestHelper;

namespace Aganox.Lms.Application.Tests.LeaveRequests;

public class LeaveRequestValidatorTests
{
    [Fact]
    public void CreateLeaveRequest_requires_valid_date_range()
    {
        var validator = new CreateLeaveRequestCommandValidator();
        var result = validator.TestValidate(new CreateLeaveRequestCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 1),
            false,
            null,
            null,
            false));
        result.ShouldHaveValidationErrorFor(x => x.ToDate);
    }

    [Fact]
    public void BalanceAdjustment_requires_reason()
    {
        var validator = new CreateBalanceAdjustmentCommandValidator();
        var result = validator.TestValidate(new CreateBalanceAdjustmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), 2026, 1, ""));
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
