using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Domain.Entities.Leave;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveConfiguration.LeavePolicies;

public sealed record GetLeavePoliciesQuery(Guid? LeaveTypeId, int? Year) : IRequest<IReadOnlyList<LeavePolicyDto>>;
public sealed record UpsertLeavePolicyCommand(
    Guid LeaveTypeId, int Year, bool SkipWeekends, bool SkipHolidays,
    decimal CarryForwardMaxDays, bool YearlyResetEnabled, decimal DefaultOpeningBalance, DateOnly? AccrualStartDate)
    : IRequest<Result<LeavePolicyDto>>;

public sealed class UpsertLeavePolicyCommandValidator : AbstractValidator<UpsertLeavePolicyCommand>
{
    public UpsertLeavePolicyCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.CarryForwardMaxDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultOpeningBalance).GreaterThanOrEqualTo(0);
    }
}

public sealed class GetLeavePoliciesQueryHandler : IRequestHandler<GetLeavePoliciesQuery, IReadOnlyList<LeavePolicyDto>>
{
    private readonly IAppDbContext _db;
    public GetLeavePoliciesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeavePolicyDto>> Handle(GetLeavePoliciesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.LeavePolicies.AsNoTracking().Include(x => x.LeaveType).AsQueryable();
        if (request.LeaveTypeId.HasValue) query = query.Where(x => x.LeaveTypeId == request.LeaveTypeId.Value);
        if (request.Year.HasValue) query = query.Where(x => x.Year == request.Year.Value);
        var items = await query.OrderBy(x => x.Year).ThenBy(x => x.LeaveType!.Name).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    internal static LeavePolicyDto Map(LeavePolicy x) => new(
        x.Id, x.LeaveTypeId, x.LeaveType!.Name, x.Year, x.SkipWeekends, x.SkipHolidays,
        x.CarryForwardMaxDays, x.YearlyResetEnabled, x.DefaultOpeningBalance, x.AccrualStartDate);
}

public sealed class UpsertLeavePolicyCommandHandler : IRequestHandler<UpsertLeavePolicyCommand, Result<LeavePolicyDto>>
{
    private readonly IAppDbContext _db;
    public UpsertLeavePolicyCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<LeavePolicyDto>> Handle(UpsertLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.LeaveTypes.AnyAsync(x => x.Id == request.LeaveTypeId, cancellationToken))
            return Result<LeavePolicyDto>.Failure("Leave type not found.");

        var policy = await _db.LeavePolicies
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.LeaveTypeId == request.LeaveTypeId && x.Year == request.Year, cancellationToken);

        if (policy is null)
        {
            policy = new LeavePolicy { LeaveTypeId = request.LeaveTypeId, Year = request.Year };
            _db.LeavePolicies.Add(policy);
        }

        policy.SkipWeekends = request.SkipWeekends;
        policy.SkipHolidays = request.SkipHolidays;
        policy.CarryForwardMaxDays = request.CarryForwardMaxDays;
        policy.YearlyResetEnabled = request.YearlyResetEnabled;
        policy.DefaultOpeningBalance = request.DefaultOpeningBalance;
        policy.AccrualStartDate = request.AccrualStartDate;

        await _db.SaveChangesAsync(cancellationToken);

        var saved = await _db.LeavePolicies.AsNoTracking()
            .Include(x => x.LeaveType)
            .FirstAsync(x => x.Id == policy.Id, cancellationToken);
        return Result<LeavePolicyDto>.Success(GetLeavePoliciesQueryHandler.Map(saved));
    }
}
