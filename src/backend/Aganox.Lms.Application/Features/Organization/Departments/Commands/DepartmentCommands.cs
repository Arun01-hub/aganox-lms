using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Organization.Dtos;
using Aganox.Lms.Domain.Entities.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Organization.Departments.Commands;

public sealed record CreateDepartmentCommand(string Name, string Code, bool IsActive) : IRequest<Result<DepartmentDto>>;

public sealed record UpdateDepartmentCommand(Guid Id, string Name, string Code, bool IsActive) : IRequest<Result<DepartmentDto>>;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest<Result>;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}

public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<DepartmentDto>>
{
    private readonly IAppDbContext _db;

    public CreateDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<DepartmentDto>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Departments.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return Result<DepartmentDto>.Failure("Department code already exists.");
        }

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = code,
            IsActive = request.IsActive
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<DepartmentDto>.Success(new DepartmentDto(
            department.Id, department.Name, department.Code, department.IsActive, 0));
    }
}

public sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentDto>>
{
    private readonly IAppDbContext _db;

    public UpdateDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<DepartmentDto>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (department is null)
        {
            return Result<DepartmentDto>.Failure("Department not found.");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Departments.AnyAsync(x => x.Code == code && x.Id != request.Id, cancellationToken))
        {
            return Result<DepartmentDto>.Failure("Department code already exists.");
        }

        department.Name = request.Name.Trim();
        department.Code = code;
        department.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<DepartmentDto>.Success(new DepartmentDto(
            department.Id,
            department.Name,
            department.Code,
            department.IsActive,
            department.Employees.Count));
    }
}

public sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (department is null)
        {
            return Result.Failure("Department not found.");
        }

        if (department.Employees.Count > 0)
        {
            return Result.Failure("Cannot delete a department that has employees.");
        }

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
