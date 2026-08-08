using Aganox.Lms.Application.Features.Payroll.Configuration;
using Aganox.Lms.Application.Features.Payroll.Processing;
using FluentValidation.TestHelper;

namespace Aganox.Lms.Application.Tests;

public class PayrollValidatorTests
{
    [Fact]
    public void CreatePayrollRunCommandValidator_rejects_invalid_month()
    {
        var validator = new CreatePayrollRunCommandValidator();
        var result = validator.TestValidate(new CreatePayrollRunCommand(2026, 13, null));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void CreateEmployeeSalaryRevisionCommandValidator_requires_employee()
    {
        var validator = new CreateEmployeeSalaryRevisionCommandValidator();
        var result = validator.TestValidate(new CreateEmployeeSalaryRevisionCommand(
            Guid.Empty, new DateOnly(2026, 1, 1), null, 10000, null, null));
        result.ShouldHaveValidationErrorFor(x => x.EmployeeId);
    }
}
