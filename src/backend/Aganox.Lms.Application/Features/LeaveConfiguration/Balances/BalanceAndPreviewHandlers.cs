using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveConfiguration.Balances;

public sealed record GetEmployeeLeaveBalancesQuery(Guid EmployeeId, int Year) : IRequest<IReadOnlyList<LeaveBalanceDto>>;
public sealed record SetEmployeeOpeningBalancesCommand(
    Guid EmployeeId, int Year, IReadOnlyList<OpeningBalanceItemRequest> Balances)
    : IRequest<Result<IReadOnlyList<LeaveBalanceDto>>>;

public sealed record PreviewLeaveDaysQuery(
    Guid? EmployeeId, Guid LeaveTypeId, DateOnly FromDate, DateOnly ToDate, bool IsHalfDay)
    : IRequest<Result<LeaveDayPreviewDto>>;

public sealed class SetEmployeeOpeningBalancesCommandValidator : AbstractValidator<SetEmployeeOpeningBalancesCommand>
{
    public SetEmployeeOpeningBalancesCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Balances).NotEmpty();
        RuleForEach(x => x.Balances).ChildRules(b =>
        {
            b.RuleFor(x => x.LeaveTypeId).NotEmpty();
            b.RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class PreviewLeaveDaysQueryValidator : AbstractValidator<PreviewLeaveDaysQuery>
{
    public PreviewLeaveDaysQueryValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}

public sealed class GetEmployeeLeaveBalancesQueryHandler : IRequestHandler<GetEmployeeLeaveBalancesQuery, IReadOnlyList<LeaveBalanceDto>>
{
    private readonly IAppDbContext _db;
    public GetEmployeeLeaveBalancesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveBalanceDto>> Handle(GetEmployeeLeaveBalancesQuery request, CancellationToken cancellationToken) =>
        await _db.LeaveBalances.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == request.EmployeeId && x.Year == request.Year)
            .OrderBy(x => x.LeaveType!.SortOrder)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

    internal static LeaveBalanceDto Map(LeaveBalance x) => new(
        x.Id, x.EmployeeId, x.LeaveTypeId, x.LeaveType!.Code, x.LeaveType.Name,
        x.Year, x.OpeningBalance, x.Credited, x.Consumed, x.Adjusted, x.Remaining);
}

public sealed class SetEmployeeOpeningBalancesCommandHandler : IRequestHandler<SetEmployeeOpeningBalancesCommand, Result<IReadOnlyList<LeaveBalanceDto>>>
{
    private readonly IAppDbContext _db;
    public SetEmployeeOpeningBalancesCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<LeaveBalanceDto>>> Handle(
        SetEmployeeOpeningBalancesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken))
            return Result<IReadOnlyList<LeaveBalanceDto>>.Failure("Employee not found.");

        foreach (var item in request.Balances)
        {
            if (!await _db.LeaveTypes.AnyAsync(x => x.Id == item.LeaveTypeId, cancellationToken))
                return Result<IReadOnlyList<LeaveBalanceDto>>.Failure("Leave type not found.");

            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.LeaveTypeId == item.LeaveTypeId &&
                    x.Year == request.Year, cancellationToken);

            if (balance is null)
            {
                balance = new LeaveBalance
                {
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = item.LeaveTypeId,
                    Year = request.Year
                };
                _db.LeaveBalances.Add(balance);
            }

            if (balance.Consumed > 0 && item.OpeningBalance < balance.OpeningBalance)
                return Result<IReadOnlyList<LeaveBalanceDto>>.Failure(
                    "Cannot reduce opening balance after leave has been consumed.");

            balance.OpeningBalance = item.OpeningBalance;
            balance.RecalculateRemaining();
        }

        await _db.SaveChangesAsync(cancellationToken);
        var results = await _db.LeaveBalances.AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.EmployeeId == request.EmployeeId && x.Year == request.Year)
            .OrderBy(x => x.LeaveType!.SortOrder)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LeaveBalanceDto>>.Success(
            results.Select(GetEmployeeLeaveBalancesQueryHandler.Map).ToList());
    }
}

public sealed class PreviewLeaveDaysQueryHandler : IRequestHandler<PreviewLeaveDaysQuery, Result<LeaveDayPreviewDto>>
{
    private readonly IAppDbContext _db;
    public PreviewLeaveDaysQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Result<LeaveDayPreviewDto>> Handle(PreviewLeaveDaysQuery request, CancellationToken cancellationToken)
    {
        var leaveType = await _db.LeaveTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LeaveTypeId, cancellationToken);
        if (leaveType is null) return Result<LeaveDayPreviewDto>.Failure("Leave type not found.");

        if (request.IsHalfDay && !leaveType.AllowHalfDay)
            return Result<LeaveDayPreviewDto>.Failure("Half-day is not allowed for this leave type.");

        var year = request.FromDate.Year;
        var policy = await _db.LeavePolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LeaveTypeId == request.LeaveTypeId && x.Year == year, cancellationToken);

        var skipWeekends = policy?.SkipWeekends ?? true;
        var skipHolidays = policy?.SkipHolidays ?? true;

        Guid? calendarId = null;
        if (request.EmployeeId.HasValue)
        {
            calendarId = await _db.Employees.AsNoTracking()
                .Where(x => x.Id == request.EmployeeId.Value)
                .Select(x => x.LeaveCalendarId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!calendarId.HasValue)
        {
            calendarId = await _db.LeaveCalendars.AsNoTracking()
                .Where(x => x.IsDefault)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var holidayDates = new HashSet<DateOnly>();
        if (calendarId.HasValue && skipHolidays)
        {
            var dates = await _db.Holidays.AsNoTracking()
                .Where(x => x.LeaveCalendarId == calendarId.Value &&
                            x.HolidayDate >= request.FromDate &&
                            x.HolidayDate <= request.ToDate)
                .Select(x => x.HolidayDate)
                .ToListAsync(cancellationToken);
            holidayDates = dates.ToHashSet();
        }

        try
        {
            var calc = LeaveDayCalculator.Calculate(
                request.FromDate, request.ToDate, request.IsHalfDay,
                skipWeekends, skipHolidays, holidayDates);

            return Result<LeaveDayPreviewDto>.Success(new LeaveDayPreviewDto(
                calc.NumberOfDays,
                calc.SkippedWeekends,
                calc.SkippedHolidays,
                calc.Breakdown.Select(b => new LeaveDayBreakdownDto(b.Date, b.Counted, b.Reason)).ToList()));
        }
        catch (ArgumentException ex)
        {
            return Result<LeaveDayPreviewDto>.Failure(ex.Message);
        }
    }
}
