using Aganox.Lms.Application.Features.Organization.Departments.Commands;
using Aganox.Lms.Application.Features.Organization.Employees.Commands;
using FluentValidation.TestHelper;

namespace Aganox.Lms.Application.Tests.Organization;

public class DepartmentValidatorTests
{
    [Fact]
    public void CreateDepartment_requires_name_and_code()
    {
        var validator = new CreateDepartmentCommandValidator();
        var result = validator.TestValidate(new CreateDepartmentCommand("", "", true));
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void UpdateDepartment_requires_id()
    {
        var validator = new UpdateDepartmentCommandValidator();
        var result = validator.TestValidate(new UpdateDepartmentCommand(Guid.Empty, "HR", "HR", true));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}

public class EmployeeValidatorTests
{
    [Fact]
    public void CreateEmployee_requires_core_fields()
    {
        var validator = new CreateEmployeeCommandValidator();
        var result = validator.TestValidate(new CreateEmployeeCommand(
            "",
            "",
            Guid.Empty,
            "",
            DateOnly.FromDateTime(DateTime.UtcNow),
            Domain.Enums.EmployeeStatus.Active,
            "",
            null,
            null,
            null,
            false,
            null,
            null));

        result.ShouldHaveValidationErrorFor(x => x.EmployeeCode);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
        result.ShouldHaveValidationErrorFor(x => x.DepartmentId);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void CreateEmployee_requires_password_when_creating_user()
    {
        var validator = new CreateEmployeeCommandValidator();
        var result = validator.TestValidate(new CreateEmployeeCommand(
            "E001",
            "Test User",
            Guid.NewGuid(),
            "Engineer",
            DateOnly.FromDateTime(DateTime.UtcNow),
            Domain.Enums.EmployeeStatus.Active,
            "test@example.com",
            null,
            null,
            null,
            true,
            "",
            "Employee"));

        result.ShouldHaveValidationErrorFor(x => x.UserPassword);
    }

    [Fact]
    public void LinkEmployeeUser_requires_user_or_email()
    {
        var validator = new LinkEmployeeUserCommandValidator();
        var result = validator.TestValidate(new LinkEmployeeUserCommand(Guid.NewGuid(), null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x);
    }
}
