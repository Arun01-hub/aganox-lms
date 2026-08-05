using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Domain.Entities.Leave;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveConfiguration.LeaveTypes;

public sealed record GetLeaveTypesQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<LeaveTypeDto>>;
public sealed record GetLeaveTypeByIdQuery(Guid Id) : IRequest<LeaveTypeDto?>;
public sealed record CreateLeaveTypeCommand(
    string Code, string Name, bool IsPaid, bool RequiresApproval, bool AllowHalfDay,
    bool AllowNegativeBalance, int? MaxDaysPerRequest, string? ColorHex, int SortOrder, bool IsActive)
    : IRequest<Result<LeaveTypeDto>>;
public sealed record UpdateLeaveTypeCommand(
    Guid Id, string Code, string Name, bool IsPaid, bool RequiresApproval, bool AllowHalfDay,
    bool AllowNegativeBalance, int? MaxDaysPerRequest, string? ColorHex, int SortOrder, bool IsActive)
    : IRequest<Result<LeaveTypeDto>>;
public sealed record DeleteLeaveTypeCommand(Guid Id) : IRequest<Result>;

public sealed class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateLeaveTypeCommandValidator : AbstractValidator<UpdateLeaveTypeCommand>
{
    public UpdateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, IReadOnlyList<LeaveTypeDto>>
{
    private readonly IAppDbContext _db;
    public GetLeaveTypesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveTypeDto>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.LeaveTypes.AsNoTracking();
        if (request.ActiveOnly) query = query.Where(x => x.IsActive);
        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    internal static LeaveTypeDto Map(LeaveType x) => new(
        x.Id, x.Code, x.Name, x.IsPaid, x.RequiresApproval, x.AllowHalfDay,
        x.AllowNegativeBalance, x.MaxDaysPerRequest, x.ColorHex, x.SortOrder, x.IsActive);
}

public sealed class GetLeaveTypeByIdQueryHandler : IRequestHandler<GetLeaveTypeByIdQuery, LeaveTypeDto?>
{
    private readonly IAppDbContext _db;
    public GetLeaveTypeByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LeaveTypeDto?> Handle(GetLeaveTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        return entity is null ? null : GetLeaveTypesQueryHandler.Map(entity);
    }
}

public sealed class CreateLeaveTypeCommandHandler : IRequestHandler<CreateLeaveTypeCommand, Result<LeaveTypeDto>>
{
    private readonly IAppDbContext _db;
    public CreateLeaveTypeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<LeaveTypeDto>> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.LeaveTypes.AnyAsync(x => x.Code == code, cancellationToken))
            return Result<LeaveTypeDto>.Failure("Leave type code already exists.");

        var entity = new LeaveType
        {
            Code = code,
            Name = request.Name.Trim(),
            IsPaid = request.IsPaid,
            RequiresApproval = request.RequiresApproval,
            AllowHalfDay = request.AllowHalfDay,
            AllowNegativeBalance = request.AllowNegativeBalance,
            MaxDaysPerRequest = request.MaxDaysPerRequest,
            ColorHex = request.ColorHex,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        _db.LeaveTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveTypeDto>.Success(GetLeaveTypesQueryHandler.Map(entity));
    }
}

public sealed class UpdateLeaveTypeCommandHandler : IRequestHandler<UpdateLeaveTypeCommand, Result<LeaveTypeDto>>
{
    private readonly IAppDbContext _db;
    public UpdateLeaveTypeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<LeaveTypeDto>> Handle(UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result<LeaveTypeDto>.Failure("Leave type not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.LeaveTypes.AnyAsync(x => x.Code == code && x.Id != request.Id, cancellationToken))
            return Result<LeaveTypeDto>.Failure("Leave type code already exists.");

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsPaid = request.IsPaid;
        entity.RequiresApproval = request.RequiresApproval;
        entity.AllowHalfDay = request.AllowHalfDay;
        entity.AllowNegativeBalance = request.AllowNegativeBalance;
        entity.MaxDaysPerRequest = request.MaxDaysPerRequest;
        entity.ColorHex = request.ColorHex;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveTypeDto>.Success(GetLeaveTypesQueryHandler.Map(entity));
    }
}

public sealed class DeleteLeaveTypeCommandHandler : IRequestHandler<DeleteLeaveTypeCommand, Result>
{
    private readonly IAppDbContext _db;
    public DeleteLeaveTypeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result.Failure("Leave type not found.");

        if (await _db.LeaveRequests.AnyAsync(x => x.LeaveTypeId == request.Id, cancellationToken))
            return Result.Failure("Cannot delete a leave type that has leave requests.");

        _db.LeaveTypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
