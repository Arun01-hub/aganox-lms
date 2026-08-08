using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Payroll.Arrears;

public sealed record PayrollArrearDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    int SourceYear,
    int SourceMonth,
    Guid? TargetPayrollRunId,
    decimal Amount,
    string Reason,
    string Status);

public sealed record GetPayrollArrearsQuery(Guid? EmployeeId, PayrollArrearStatus? Status)
    : IRequest<IReadOnlyList<PayrollArrearDto>>;

public sealed record CreatePayrollArrearCommand(
    Guid EmployeeId,
    int SourceYear,
    int SourceMonth,
    decimal Amount,
    string Reason) : IRequest<Result<PayrollArrearDto>>;

public sealed record CancelPayrollArrearCommand(Guid Id) : IRequest<Result>;

public sealed class CreatePayrollArrearCommandValidator : AbstractValidator<CreatePayrollArrearCommand>
{
    public CreatePayrollArrearCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetPayrollArrearsQueryHandler : IRequestHandler<GetPayrollArrearsQuery, IReadOnlyList<PayrollArrearDto>>
{
    private readonly IAppDbContext _db;
    public GetPayrollArrearsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollArrearDto>> Handle(
        GetPayrollArrearsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.PayrollArrears.AsNoTracking().Include(x => x.Employee).AsQueryable();
        if (request.EmployeeId.HasValue) query = query.Where(x => x.EmployeeId == request.EmployeeId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        var items = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(x => new PayrollArrearDto(
            x.Id,
            x.EmployeeId,
            x.Employee!.FullName,
            x.SourceYear,
            x.SourceMonth,
            x.TargetPayrollRunId,
            x.Amount,
            x.Reason,
            x.Status.ToString())).ToList();
    }
}

public sealed class CreatePayrollArrearCommandHandler : IRequestHandler<CreatePayrollArrearCommand, Result<PayrollArrearDto>>
{
    private readonly IAppDbContext _db;
    public CreatePayrollArrearCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PayrollArrearDto>> Handle(
        CreatePayrollArrearCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee is null) return Result<PayrollArrearDto>.Failure("Employee not found.");

        var entity = new PayrollArrear
        {
            EmployeeId = request.EmployeeId,
            SourceYear = request.SourceYear,
            SourceMonth = request.SourceMonth,
            Amount = request.Amount,
            Reason = request.Reason.Trim(),
            Status = PayrollArrearStatus.Pending
        };
        _db.PayrollArrears.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PayrollArrearDto>.Success(new PayrollArrearDto(
            entity.Id,
            entity.EmployeeId,
            employee.FullName,
            entity.SourceYear,
            entity.SourceMonth,
            null,
            entity.Amount,
            entity.Reason,
            entity.Status.ToString()));
    }
}

public sealed class CancelPayrollArrearCommandHandler : IRequestHandler<CancelPayrollArrearCommand, Result>
{
    private readonly IAppDbContext _db;
    public CancelPayrollArrearCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(CancelPayrollArrearCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.PayrollArrears.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result.Failure("Arrear not found.");
        if (entity.Status != PayrollArrearStatus.Pending)
        {
            return Result.Failure("Only pending arrears can be cancelled.");
        }

        entity.Status = PayrollArrearStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
