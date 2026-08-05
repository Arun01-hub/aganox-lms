using Aganox.Lms.Application.Features.LeaveConfiguration.Balances;
using Aganox.Lms.Application.Features.LeaveConfiguration.LeavePolicies;
using Aganox.Lms.Application.Features.LeaveConfiguration.LeaveTypes;
using FluentValidation.TestHelper;

namespace Aganox.Lms.Application.Tests.LeaveConfiguration;

public class LeaveConfigurationValidatorTests
{
    [Fact]
    public void UpsertLeavePolicy_validates_year_range()
    {
        var validator = new UpsertLeavePolicyCommandValidator();
        var result = validator.TestValidate(new UpsertLeavePolicyCommand(
            Guid.NewGuid(), 1999, true, true, 0, true, 12, null));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void SetOpeningBalances_requires_items()
    {
        var validator = new SetEmployeeOpeningBalancesCommandValidator();
        var result = validator.TestValidate(new SetEmployeeOpeningBalancesCommand(
            Guid.NewGuid(), 2026, []));
        result.ShouldHaveValidationErrorFor(x => x.Balances);
    }

    [Fact]
    public void PreviewLeaveDays_requires_valid_date_range()
    {
        var validator = new PreviewLeaveDaysQueryValidator();
        var result = validator.TestValidate(new PreviewLeaveDaysQuery(
            null, Guid.NewGuid(), new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 1), false));
        result.ShouldHaveValidationErrorFor(x => x.ToDate);
    }
}
