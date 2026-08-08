using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Attendance;
using Aganox.Lms.Application.Features.Payroll.Configuration;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services.Attendance;
using Aganox.Lms.Domain.Services.Payroll;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Payroll.Processing;

public sealed record PayrollRunDto(
    Guid Id,
    int Year,
    int Month,
    string Status,
    DateTime? CalculatedAt,
    DateTime? ApprovedAt,
    DateTime? LockedAt,
    string? Notes,
    int EmployeeCount,
    decimal TotalNetPay);

public sealed record PayrollEmployeeResultDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    Guid? DepartmentId,
    decimal BasicPay,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    decimal PaidDays,
    decimal LopDays,
    IReadOnlyList<PayrollLineItemDto> LineItems);

public sealed record PayrollLineItemDto(
    string ComponentCode,
    string ComponentName,
    string Category,
    decimal Amount,
    int SortOrder);

public sealed record PayrollRunAuditDto(
    Guid Id,
    string Action,
    Guid? UserId,
    string? Details,
    DateTime Timestamp);

public sealed record GetPayrollRunsQuery(int? Year) : IRequest<IReadOnlyList<PayrollRunDto>>;
public sealed record GetPayrollRunByIdQuery(Guid Id) : IRequest<PayrollRunDto?>;
public sealed record GetPayrollRunResultsQuery(Guid PayrollRunId) : IRequest<IReadOnlyList<PayrollEmployeeResultDto>>;
public sealed record GetPayrollRunAuditQuery(Guid PayrollRunId) : IRequest<IReadOnlyList<PayrollRunAuditDto>>;

public sealed record CreatePayrollRunCommand(int Year, int Month, string? Notes) : IRequest<Result<PayrollRunDto>>;
public sealed record CalculatePayrollRunCommand(Guid PayrollRunId, bool Recalculate = false) : IRequest<Result<PayrollRunDto>>;
public sealed record SubmitPayrollRunForApprovalCommand(Guid PayrollRunId) : IRequest<Result<PayrollRunDto>>;
public sealed record ApprovePayrollRunCommand(Guid PayrollRunId) : IRequest<Result<PayrollRunDto>>;
public sealed record LockPayrollRunCommand(Guid PayrollRunId) : IRequest<Result<PayrollRunDto>>;

public sealed class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

internal static class PayrollRunMapper
{
    public static PayrollRunDto Map(PayrollRun run, int employeeCount, decimal totalNet) => new(
        run.Id,
        run.Year,
        run.Month,
        run.Status.ToString(),
        run.CalculatedAt,
        run.ApprovedAt,
        run.LockedAt,
        run.Notes,
        employeeCount,
        totalNet);
}

internal static class PayrollRunOperations
{
    public static void ClearEmployeeResults(IAppDbContext db, PayrollRun run)
    {
        foreach (var result in run.EmployeeResults.ToList())
        {
            db.PayrollLineItems.RemoveRange(result.LineItems);
        }

        db.PayrollEmployeeResults.RemoveRange(run.EmployeeResults);
    }
}

public sealed class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, IReadOnlyList<PayrollRunDto>>
{
    private readonly IAppDbContext _db;
    public GetPayrollRunsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollRunDto>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.PayrollRuns.AsNoTracking();
        if (request.Year.HasValue) query = query.Where(x => x.Year == request.Year);
        var runs = await query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync(cancellationToken);
        var list = new List<PayrollRunDto>();
        foreach (var run in runs)
        {
            var results = await _db.PayrollEmployeeResults.AsNoTracking()
                .Where(x => x.PayrollRunId == run.Id)
                .ToListAsync(cancellationToken);
            list.Add(PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay)));
        }

        return list;
    }
}

public sealed class GetPayrollRunByIdQueryHandler : IRequestHandler<GetPayrollRunByIdQuery, PayrollRunDto?>
{
    private readonly IAppDbContext _db;
    public GetPayrollRunByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PayrollRunDto?> Handle(GetPayrollRunByIdQuery request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (run is null) return null;
        var results = await _db.PayrollEmployeeResults.AsNoTracking().Where(x => x.PayrollRunId == run.Id).ToListAsync(cancellationToken);
        return PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay));
    }
}

public sealed class GetPayrollRunResultsQueryHandler
    : IRequestHandler<GetPayrollRunResultsQuery, IReadOnlyList<PayrollEmployeeResultDto>>
{
    private readonly IAppDbContext _db;
    public GetPayrollRunResultsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollEmployeeResultDto>> Handle(
        GetPayrollRunResultsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await _db.PayrollEmployeeResults.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LineItems)
            .Where(x => x.PayrollRunId == request.PayrollRunId)
            .OrderBy(x => x.Employee!.EmployeeCode)
            .ToListAsync(cancellationToken);

        return results.Select(x => new PayrollEmployeeResultDto(
            x.Id,
            x.EmployeeId,
            x.Employee!.FullName,
            x.Employee.EmployeeCode,
            x.Employee.DepartmentId,
            x.BasicPaySnapshot,
            x.GrossPay,
            x.TotalDeductions,
            x.NetPay,
            x.PaidDays,
            x.LopDays,
            x.LineItems.OrderBy(l => l.SortOrder).Select(l => new PayrollLineItemDto(
                l.ComponentCode,
                l.ComponentName,
                l.Category.ToString(),
                l.Amount,
                l.SortOrder)).ToList())).ToList();
    }
}

public sealed class GetPayrollRunAuditQueryHandler : IRequestHandler<GetPayrollRunAuditQuery, IReadOnlyList<PayrollRunAuditDto>>
{
    private readonly IAppDbContext _db;
    public GetPayrollRunAuditQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PayrollRunAuditDto>> Handle(
        GetPayrollRunAuditQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _db.PayrollRunAudits.AsNoTracking()
            .Where(x => x.PayrollRunId == request.PayrollRunId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        return items.Select(x => new PayrollRunAuditDto(
            x.Id,
            x.Action.ToString(),
            x.UserId,
            x.Details,
            x.Timestamp)).ToList();
    }
}

public sealed class CreatePayrollRunCommandHandler : IRequestHandler<CreatePayrollRunCommand, Result<PayrollRunDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreatePayrollRunCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PayrollRunDto>> Handle(CreatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var existing = await _db.PayrollRuns
            .Include(x => x.EmployeeResults)
            .ThenInclude(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Year == request.Year && x.Month == request.Month, cancellationToken);

        if (existing is not null)
        {
            if (PayrollRunGuard.IsImmutable(existing.Status))
            {
                return Result<PayrollRunDto>.Failure("Locked payroll runs cannot be replaced.");
            }

            PayrollRunOperations.ClearEmployeeResults(_db, existing);
            PayrollRunGuard.ResetToDraft(existing);
            if (request.Notes is not null)
            {
                existing.Notes = request.Notes;
            }

            _db.PayrollRunAudits.Add(new PayrollRunAudit
            {
                PayrollRunId = existing.Id,
                Action = PayrollAuditAction.Recalculated,
                UserId = _currentUser.UserId,
                Details = $"Existing payroll run reset for {request.Year}-{request.Month:D2}"
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Result<PayrollRunDto>.Success(PayrollRunMapper.Map(existing, 0, 0));
        }

        var run = new PayrollRun
        {
            Year = request.Year,
            Month = request.Month,
            Status = PayrollRunStatus.Draft,
            Notes = request.Notes
        };
        _db.PayrollRuns.Add(run);
        _db.PayrollRunAudits.Add(new PayrollRunAudit
        {
            PayrollRunId = run.Id,
            Action = PayrollAuditAction.Created,
            UserId = _currentUser.UserId,
            Details = $"Payroll run created for {request.Year}-{request.Month:D2}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PayrollRunDto>.Success(PayrollRunMapper.Map(run, 0, 0));
    }
}

public sealed class CalculatePayrollRunCommandHandler : IRequestHandler<CalculatePayrollRunCommand, Result<PayrollRunDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly AttendanceService _attendance;

    public CalculatePayrollRunCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        AttendanceService attendance)
    {
        _db = db;
        _currentUser = currentUser;
        _attendance = attendance;
    }

    public async Task<Result<PayrollRunDto>> Handle(
        CalculatePayrollRunCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns
            .Include(x => x.EmployeeResults)
            .ThenInclude(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);

        if (run is null) return Result<PayrollRunDto>.Failure("Payroll run not found.");
        if (PayrollRunGuard.IsImmutable(run.Status))
        {
            return Result<PayrollRunDto>.Failure("Locked payroll runs cannot be recalculated.");
        }

        if (!PayrollRunGuard.CanRecalculate(run.Status))
        {
            return Result<PayrollRunDto>.Failure("Payroll run cannot be calculated in its current status.");
        }

        if (run.Status is PayrollRunStatus.Approved or PayrollRunStatus.PendingApproval)
        {
            PayrollRunGuard.ResetApprovalState(run);
        }

        PayrollRunOperations.ClearEmployeeResults(_db, run);

        var effectiveDate = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
        var employees = await _db.Employees.AsNoTracking()
            .Where(x => x.Status == EmployeeStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var employee in employees)
        {
            var revision = await PayrollConfigurationLoader.GetEffectiveRevisionAsync(
                _db, employee.Id, effectiveDate, cancellationToken);
            if (revision is null)
            {
                continue;
            }

            var holidays = await _attendance.GetHolidayDatesAsync(employee.LeaveCalendarId, run.Year, run.Month, cancellationToken);
            var dayMap = await _attendance.BuildDayStatusMapAsync(employee.Id, run.Year, run.Month, holidays, cancellationToken);
            var unpaid = await _attendance.GetUnpaidLeaveDatesAsync(employee.Id, run.Year, run.Month, cancellationToken);
            var attendanceSummary = AttendanceCalendarService.Summarize(run.Year, run.Month, dayMap, unpaid);

            var rules = await PayrollConfigurationLoader.LoadRulesForDateAsync(
                _db, effectiveDate, revision, cancellationToken);

            var proratedBasic = revision.BasicPay;
            if (attendanceSummary.WorkingDays > 0 && attendanceSummary.PaidDays < attendanceSummary.WorkingDays)
            {
                proratedBasic = Math.Round(
                    revision.BasicPay / attendanceSummary.WorkingDays * attendanceSummary.PaidDays,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            var calc = PayrollCalculationEngine.Calculate(
                proratedBasic,
                new AttendanceMonthSummaryInput(
                    attendanceSummary.WorkingDays,
                    attendanceSummary.LopDays,
                    attendanceSummary.PaidDays),
                rules);

            var pendingArrears = await _db.PayrollArrears
                .Where(x => x.EmployeeId == employee.Id && x.Status == PayrollArrearStatus.Pending)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

            var netWithArrears = calc.NetPay + pendingArrears;

            var inputSnapshot = PayrollSnapshotBuilder.BuildInputSnapshot(new
            {
                employeeId = employee.Id,
                revisionId = revision.Id,
                revision.BasicPay,
                proratedBasic,
                attendanceSummary,
                ruleCount = rules.Count
            });

            var outputSnapshot = PayrollSnapshotBuilder.BuildOutputSnapshot(calc);

            var result = new PayrollEmployeeResult
            {
                PayrollRunId = run.Id,
                EmployeeId = employee.Id,
                BasicPaySnapshot = proratedBasic,
                GrossPay = calc.GrossPay,
                TotalDeductions = calc.TotalDeductions,
                NetPay = netWithArrears,
                PaidDays = attendanceSummary.PaidDays,
                LopDays = attendanceSummary.LopDays,
                InputSnapshotJson = inputSnapshot,
                OutputSnapshotJson = outputSnapshot
            };

            foreach (var line in calc.Lines)
            {
                result.LineItems.Add(new PayrollLineItem
                {
                    ComponentCode = line.ComponentCode,
                    ComponentName = line.ComponentName,
                    Category = line.Category,
                    Amount = line.Amount,
                    SortOrder = line.SortOrder
                });
            }

            if (pendingArrears > 0)
            {
                result.LineItems.Add(new PayrollLineItem
                {
                    ComponentCode = "ARREAR",
                    ComponentName = "Arrears",
                    Category = SalaryComponentCategory.Earning,
                    Amount = pendingArrears,
                    SortOrder = 1000
                });
            }

            result.IsManuallyAdjusted = false;
            result.CalculatedSnapshotJson = PayrollSnapshotBuilder.BuildCalculatedSnapshot(result);

            _db.PayrollEmployeeResults.Add(result);
        }

        run.Status = PayrollRunStatus.Calculated;
        run.CalculatedAt = DateTime.UtcNow;
        _db.PayrollRunAudits.Add(new PayrollRunAudit
        {
            PayrollRunId = run.Id,
            Action = request.Recalculate ? PayrollAuditAction.Recalculated : PayrollAuditAction.Calculated,
            UserId = _currentUser.UserId,
            Details = $"Calculated payroll for {employees.Count} active employees"
        });

        await _db.SaveChangesAsync(cancellationToken);

        var results = await _db.PayrollEmployeeResults.AsNoTracking().Where(x => x.PayrollRunId == run.Id).ToListAsync(cancellationToken);
        return Result<PayrollRunDto>.Success(PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay)));
    }
}

public sealed class SubmitPayrollRunForApprovalCommandHandler : IRequestHandler<SubmitPayrollRunForApprovalCommand, Result<PayrollRunDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitPayrollRunForApprovalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PayrollRunDto>> Handle(
        SubmitPayrollRunForApprovalCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return Result<PayrollRunDto>.Failure("Payroll run not found.");
        if (run.Status != PayrollRunStatus.Calculated)
        {
            return Result<PayrollRunDto>.Failure("Only calculated payroll runs can be submitted for approval.");
        }

        run.Status = PayrollRunStatus.PendingApproval;
        _db.PayrollRunAudits.Add(new PayrollRunAudit
        {
            PayrollRunId = run.Id,
            Action = PayrollAuditAction.SubmittedForApproval,
            UserId = _currentUser.UserId
        });
        await _db.SaveChangesAsync(cancellationToken);
        var results = await _db.PayrollEmployeeResults.AsNoTracking().Where(x => x.PayrollRunId == run.Id).ToListAsync(cancellationToken);
        return Result<PayrollRunDto>.Success(PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay)));
    }
}

public sealed class ApprovePayrollRunCommandHandler : IRequestHandler<ApprovePayrollRunCommand, Result<PayrollRunDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ApprovePayrollRunCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PayrollRunDto>> Handle(
        ApprovePayrollRunCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return Result<PayrollRunDto>.Failure("Payroll run not found.");
        if (!PayrollRunGuard.CanApprove(run.Status))
        {
            return Result<PayrollRunDto>.Failure("Payroll run cannot be approved in its current status.");
        }

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedAt = DateTime.UtcNow;
        run.ApprovedByUserId = _currentUser.UserId;
        _db.PayrollRunAudits.Add(new PayrollRunAudit
        {
            PayrollRunId = run.Id,
            Action = PayrollAuditAction.Approved,
            UserId = _currentUser.UserId
        });
        await _db.SaveChangesAsync(cancellationToken);
        var results = await _db.PayrollEmployeeResults.AsNoTracking().Where(x => x.PayrollRunId == run.Id).ToListAsync(cancellationToken);
        return Result<PayrollRunDto>.Success(PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay)));
    }
}

public sealed class LockPayrollRunCommandHandler : IRequestHandler<LockPayrollRunCommand, Result<PayrollRunDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LockPayrollRunCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PayrollRunDto>> Handle(LockPayrollRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return Result<PayrollRunDto>.Failure("Payroll run not found.");
        if (!PayrollRunGuard.CanLock(run.Status))
        {
            return Result<PayrollRunDto>.Failure("Only approved payroll runs can be locked.");
        }

        run.Status = PayrollRunStatus.Locked;
        run.LockedAt = DateTime.UtcNow;
        run.LockedByUserId = _currentUser.UserId;

        var arrears = await _db.PayrollArrears
            .Where(x => x.Status == PayrollArrearStatus.Pending &&
                        _db.PayrollEmployeeResults.Any(r => r.PayrollRunId == run.Id && r.EmployeeId == x.EmployeeId))
            .ToListAsync(cancellationToken);

        foreach (var arrear in arrears)
        {
            arrear.Status = PayrollArrearStatus.Applied;
            arrear.TargetPayrollRunId = run.Id;
        }

        _db.PayrollRunAudits.Add(new PayrollRunAudit
        {
            PayrollRunId = run.Id,
            Action = PayrollAuditAction.Locked,
            UserId = _currentUser.UserId,
            Details = "Payroll locked; results are immutable"
        });
        await _db.SaveChangesAsync(cancellationToken);
        var results = await _db.PayrollEmployeeResults.AsNoTracking().Where(x => x.PayrollRunId == run.Id).ToListAsync(cancellationToken);
        return Result<PayrollRunDto>.Success(PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay)));
    }
}
