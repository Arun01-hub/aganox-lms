using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Organization.Dtos;
using Aganox.Lms.Application.Features.Organization.Employees.Queries;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Organization.Employees.Commands;

public sealed record CreateEmployeeCommand(
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    string Designation,
    DateOnly DateOfJoining,
    EmployeeStatus Status,
    string Email,
    string? Phone,
    Guid? ManagerId,
    Guid? LeaveCalendarId,
    bool CreateUserAccount,
    string? UserPassword,
    string? UserRole) : IRequest<Result<EmployeeSummaryDto>>;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    string Designation,
    DateOnly DateOfJoining,
    EmployeeStatus Status,
    string Email,
    string? Phone,
    Guid? ManagerId,
    Guid? LeaveCalendarId) : IRequest<Result<EmployeeSummaryDto>>;

public sealed record DeleteEmployeeCommand(Guid Id) : IRequest<Result>;

public sealed record LinkEmployeeUserCommand(
    Guid EmployeeId,
    Guid? UserId,
    string? Email,
    string? Password,
    string? Role) : IRequest<Result<EmployeeSummaryDto>>;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.UserPassword)
            .NotEmpty()
            .MinimumLength(8)
            .When(x => x.CreateUserAccount);
        RuleFor(x => x.UserRole)
            .NotEmpty()
            .Must(r => Roles.All.Contains(r!))
            .When(x => x.CreateUserAccount);
    }
}

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}

public sealed class LinkEmployeeUserCommandValidator : AbstractValidator<LinkEmployeeUserCommand>
{
    public LinkEmployeeUserCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Either UserId or Email must be provided.");
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .When(x => !x.UserId.HasValue);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Roles.All.Contains(r!))
            .When(x => !x.UserId.HasValue);
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<EmployeeSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;

    public CreateEmployeeCommandHandler(IAppDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<Result<EmployeeSummaryDto>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var validation = await ValidateReferencesAsync(
            request.DepartmentId,
            request.ManagerId,
            request.LeaveCalendarId,
            null,
            cancellationToken);
        if (!validation.Succeeded)
        {
            return Result<EmployeeSummaryDto>.Failure(validation.Errors);
        }

        var code = request.EmployeeCode.Trim().ToUpperInvariant();
        var email = request.Email.Trim();

        if (await _db.Employees.AnyAsync(x => x.EmployeeCode == code, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Employee code already exists.");
        }

        if (await _db.Employees.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Employee email already exists.");
        }

        var employee = new Employee
        {
            EmployeeCode = code,
            FullName = request.FullName.Trim(),
            DepartmentId = request.DepartmentId,
            Designation = request.Designation.Trim(),
            DateOfJoining = request.DateOfJoining,
            Status = request.Status,
            Email = email,
            Phone = request.Phone?.Trim(),
            ManagerId = request.ManagerId,
            LeaveCalendarId = request.LeaveCalendarId
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.CreateUserAccount)
        {
            var link = await _identity.CreateUserForEmployeeAsync(
                employee.Id,
                email,
                request.UserPassword!,
                request.UserRole!,
                cancellationToken);

            if (!link.Succeeded)
            {
                return Result<EmployeeSummaryDto>.Failure(link.Errors);
            }

            employee.UserId = link.UserId;
        }

        return Result<EmployeeSummaryDto>.Success(await LoadSummaryAsync(employee.Id, cancellationToken));
    }

    private async Task<Result> ValidateReferencesAsync(
        Guid departmentId,
        Guid? managerId,
        Guid? leaveCalendarId,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Departments.AnyAsync(x => x.Id == departmentId, cancellationToken))
        {
            return Result.Failure("Department not found.");
        }

        if (managerId.HasValue)
        {
            if (employeeId.HasValue && managerId.Value == employeeId.Value)
            {
                return Result.Failure("Employee cannot be their own manager.");
            }

            if (!await _db.Employees.AnyAsync(x => x.Id == managerId.Value, cancellationToken))
            {
                return Result.Failure("Manager not found.");
            }
        }

        if (leaveCalendarId.HasValue &&
            !await _db.LeaveCalendars.AnyAsync(x => x.Id == leaveCalendarId.Value, cancellationToken))
        {
            return Result.Failure("Leave calendar not found.");
        }

        return Result.Success();
    }

    private async Task<EmployeeSummaryDto> LoadSummaryAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return GetEmployeesQueryHandler.MapSummary(employee);
    }
}

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<EmployeeSummaryDto>>
{
    private readonly IAppDbContext _db;

    public UpdateEmployeeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<EmployeeSummaryDto>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (employee is null)
        {
            return Result<EmployeeSummaryDto>.Failure("Employee not found.");
        }

        if (request.ManagerId.HasValue && request.ManagerId.Value == request.Id)
        {
            return Result<EmployeeSummaryDto>.Failure("Employee cannot be their own manager.");
        }

        if (!await _db.Departments.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Department not found.");
        }

        if (request.ManagerId.HasValue &&
            !await _db.Employees.AnyAsync(x => x.Id == request.ManagerId.Value, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Manager not found.");
        }

        if (request.LeaveCalendarId.HasValue &&
            !await _db.LeaveCalendars.AnyAsync(x => x.Id == request.LeaveCalendarId.Value, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Leave calendar not found.");
        }

        var code = request.EmployeeCode.Trim().ToUpperInvariant();
        var email = request.Email.Trim();

        if (await _db.Employees.AnyAsync(x => x.EmployeeCode == code && x.Id != request.Id, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Employee code already exists.");
        }

        if (await _db.Employees.AnyAsync(x => x.Email == email && x.Id != request.Id, cancellationToken))
        {
            return Result<EmployeeSummaryDto>.Failure("Employee email already exists.");
        }

        employee.EmployeeCode = code;
        employee.FullName = request.FullName.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.Designation = request.Designation.Trim();
        employee.DateOfJoining = request.DateOfJoining;
        employee.Status = request.Status;
        employee.Email = email;
        employee.Phone = request.Phone?.Trim();
        employee.ManagerId = request.ManagerId;
        employee.LeaveCalendarId = request.LeaveCalendarId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<EmployeeSummaryDto>.Success(GetEmployeesQueryHandler.MapSummary(employee));
    }
}

public sealed class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteEmployeeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .Include(x => x.DirectReports)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure("Employee not found.");
        }

        if (employee.DirectReports.Count > 0)
        {
            return Result.Failure("Cannot delete an employee who has direct reports. Reassign reports first.");
        }

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class LinkEmployeeUserCommandHandler : IRequestHandler<LinkEmployeeUserCommand, Result<EmployeeSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;

    public LinkEmployeeUserCommandHandler(IAppDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<Result<EmployeeSummaryDto>> Handle(LinkEmployeeUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId.HasValue)
        {
            var link = await _identity.LinkExistingUserAsync(request.EmployeeId, request.UserId.Value, cancellationToken);
            if (!link.Succeeded)
            {
                return Result<EmployeeSummaryDto>.Failure(link.Errors);
            }
        }
        else
        {
            var create = await _identity.CreateUserForEmployeeAsync(
                request.EmployeeId,
                request.Email!.Trim(),
                request.Password!,
                request.Role!,
                cancellationToken);

            if (!create.Succeeded)
            {
                return Result<EmployeeSummaryDto>.Failure(create.Errors);
            }
        }

        var employee = await _db.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .FirstAsync(x => x.Id == request.EmployeeId, cancellationToken);

        return Result<EmployeeSummaryDto>.Success(GetEmployeesQueryHandler.MapSummary(employee));
    }
}
