using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Approvals.Dtos;
using Aganox.Lms.Application.Features.Approvals.Services;
using Aganox.Lms.Application.Features.LeaveConfiguration.Balances;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Application.Features.LeaveRequests.Dtos;
using Aganox.Lms.Application.Features.LeaveRequests.Services;
using Aganox.Lms.Application.Features.Notifications;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveRequests;

public sealed record GetLeaveRequestsQuery(
    Guid? EmployeeId,
    LeaveRequestStatus? Status,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<LeaveRequestSummaryDto>>;

public sealed record GetLeaveRequestByIdQuery(Guid Id) : IRequest<LeaveRequestDetailDto?>;

public sealed record CreateLeaveRequestCommand(
    Guid? EmployeeId,
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay,
    HalfDayPeriod? HalfDayPeriod,
    string? Remarks,
    bool Submit) : IRequest<Result<LeaveRequestDetailDto>>;

public sealed record UpdateLeaveRequestCommand(
    Guid Id,
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    bool IsHalfDay,
    HalfDayPeriod? HalfDayPeriod,
    string? Remarks,
    bool Submit) : IRequest<Result<LeaveRequestDetailDto>>;

public sealed record SubmitLeaveRequestCommand(Guid Id) : IRequest<Result<LeaveRequestDetailDto>>;

public sealed record CancelLeaveRequestCommand(Guid Id) : IRequest<Result<LeaveRequestDetailDto>>;

public sealed record CreateBalanceAdjustmentCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    decimal DeltaDays,
    string Reason) : IRequest<Result<LeaveBalanceDto>>;

public sealed class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public sealed class UpdateLeaveRequestCommandValidator : AbstractValidator<UpdateLeaveRequestCommand>
{
    public UpdateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public sealed class CreateBalanceAdjustmentCommandValidator : AbstractValidator<CreateBalanceAdjustmentCommand>
{
    public CreateBalanceAdjustmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, PagedResult<LeaveRequestSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetLeaveRequestsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<LeaveRequestSummaryDto>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);

        var query = _db.LeaveRequests.AsNoTracking().AsQueryable();

        query = await LeaveRequestAccessService.ApplyViewScopeAsync(
            query, _db, _currentUser, request.EmployeeId, cancellationToken);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LeaveRequestSummaryDto(
                x.Id,
                x.EmployeeId,
                x.Employee!.FullName,
                x.LeaveTypeId,
                x.LeaveType!.Name,
                x.LeaveType.Code,
                x.FromDate,
                x.ToDate,
                x.IsHalfDay,
                x.HalfDayPeriod != null ? x.HalfDayPeriod.ToString() : null,
                x.NumberOfDays,
                x.Status.ToString(),
                x.Remarks,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<LeaveRequestSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    internal static LeaveRequestSummaryDto MapSummary(LeaveRequest x) => new(
        x.Id, x.EmployeeId, x.Employee!.FullName, x.LeaveTypeId, x.LeaveType!.Name, x.LeaveType.Code,
        x.FromDate, x.ToDate, x.IsHalfDay, x.HalfDayPeriod?.ToString(), x.NumberOfDays,
        x.Status.ToString(), x.Remarks, x.CreatedAt);

    internal static LeaveRequestDetailDto MapDetail(
        LeaveRequest x,
        IReadOnlyList<ApprovalStepDto> approvalSteps,
        IReadOnlyList<LeaveRequestCommentDto> comments) => new(
        x.Id, x.EmployeeId, x.Employee!.FullName, x.LeaveTypeId, x.LeaveType!.Name, x.LeaveType.Code,
        x.FromDate, x.ToDate, x.IsHalfDay, x.HalfDayPeriod?.ToString(), x.NumberOfDays,
        x.Status.ToString(), x.Remarks, x.CreatedAt, x.ModifiedAt, approvalSteps, comments);
}

internal static class LeaveRequestDetailLoader
{
    public static async Task<LeaveRequestDetailDto> LoadAsync(
        IAppDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstAsync(x => x.Id == id, cancellationToken);

        var steps = await db.LeaveApprovalSteps.AsNoTracking()
            .Include(x => x.ApproverEmployee)
            .Where(x => x.LeaveRequestId == id)
            .OrderBy(x => x.StepOrder)
            .ToListAsync(cancellationToken);

        var comments = await db.LeaveRequestComments.AsNoTracking()
            .Where(x => x.LeaveRequestId == id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var authorIds = comments.Select(x => x.AuthorUserId).Distinct().ToList();
        var authorNames = await db.Employees.AsNoTracking()
            .Where(x => x.UserId.HasValue && authorIds.Contains(x.UserId.Value))
            .ToDictionaryAsync(x => x.UserId!.Value, x => x.FullName, cancellationToken);

        return GetLeaveRequestsQueryHandler.MapDetail(
            entity,
            steps.Select(LeaveRequestMapper.MapStep).ToList(),
            comments.Select(c => LeaveRequestMapper.MapComment(
                c, authorNames.GetValueOrDefault(c.AuthorUserId, "User"))).ToList());
    }
}

public sealed class GetLeaveRequestByIdQueryHandler : IRequestHandler<GetLeaveRequestByIdQuery, LeaveRequestDetailDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetLeaveRequestByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LeaveRequestDetailDto?> Handle(GetLeaveRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (!await CanAccessAsync(entity.EmployeeId, entity.Id, cancellationToken))
        {
            return null;
        }

        return await LeaveRequestDetailLoader.LoadAsync(_db, entity.Id, cancellationToken);
    }

    private async Task<bool> CanAccessAsync(Guid employeeId, Guid leaveRequestId, CancellationToken cancellationToken)
    {
        if (LeaveRequestAccessService.CanViewAll(_currentUser))
        {
            return true;
        }

        if (!_currentUser.UserId.HasValue)
        {
            return false;
        }

        var ownId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(_db, _currentUser.UserId.Value, cancellationToken);
        if (ownId == employeeId)
        {
            return true;
        }

        if (LeaveRequestAccessService.CanViewTeam(_currentUser) && ownId.HasValue)
        {
            return await _db.Employees.AsNoTracking()
                .AnyAsync(x => x.Id == employeeId && x.ManagerId == ownId.Value, cancellationToken);
        }

        if (_currentUser.Permissions.Contains(Permissions.ApprovalsAct))
        {
            var steps = await _db.LeaveApprovalSteps.AsNoTracking()
                .Where(x => x.LeaveRequestId == leaveRequestId)
                .ToListAsync(cancellationToken);

            if (_currentUser.Roles.Contains(Roles.Hr) || _currentUser.Roles.Contains(Roles.Administrator))
            {
                return steps.Count > 0;
            }

            if (ownId.HasValue)
            {
                var current = ApprovalWorkflow.GetCurrentPendingStep(steps);
                if (current is not null &&
                    ApprovalWorkflow.CanActOnStep(current, steps, ownId, _currentUser.Roles))
                {
                    return true;
                }

                if (steps.Any(x => x.ApproverEmployeeId == ownId))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

public sealed class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Result<LeaveRequestDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly LeaveNotificationService _notifications;

    public CreateLeaveRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        LeaveNotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result<LeaveRequestDetailDto>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Not authenticated.");
        }

        Guid targetEmployeeId;
        if (request.EmployeeId.HasValue)
        {
            if (!LeaveRequestAccessService.CanViewAll(_currentUser))
            {
                return Result<LeaveRequestDetailDto>.Failure("You are not allowed to apply leave for another employee.");
            }

            var exists = await _db.Employees.AsNoTracking()
                .AnyAsync(x => x.Id == request.EmployeeId.Value && x.Status == EmployeeStatus.Active, cancellationToken);
            if (!exists)
            {
                return Result<LeaveRequestDetailDto>.Failure("Employee not found.");
            }

            targetEmployeeId = request.EmployeeId.Value;
        }
        else
        {
            var employeeId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(_db, _currentUser.UserId.Value, cancellationToken);
            if (!employeeId.HasValue)
            {
                return Result<LeaveRequestDetailDto>.Failure("Your user account is not linked to an employee profile.");
            }

            targetEmployeeId = employeeId.Value;
        }

        var calcResult = await LeaveDayCalculationService.CalculateAsync(
            _db, targetEmployeeId, request.LeaveTypeId, request.FromDate, request.ToDate, request.IsHalfDay, cancellationToken);
        if (!calcResult.Result.Succeeded)
        {
            return Result<LeaveRequestDetailDto>.Failure(calcResult.Result.Errors);
        }

        var leaveType = calcResult.LeaveType!;
        if (leaveType.MaxDaysPerRequest.HasValue && calcResult.Result.Data!.NumberOfDays > leaveType.MaxDaysPerRequest.Value)
        {
            return Result<LeaveRequestDetailDto>.Failure(
                $"Maximum {leaveType.MaxDaysPerRequest.Value} day(s) allowed per request for this leave type.");
        }

        if (calcResult.Result.Data!.NumberOfDays <= 0)
        {
            return Result<LeaveRequestDetailDto>.Failure("Leave duration must be at least 0.5 day.");
        }

        var entity = new LeaveRequest
        {
            EmployeeId = targetEmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            IsHalfDay = request.IsHalfDay,
            HalfDayPeriod = request.IsHalfDay ? request.HalfDayPeriod : null,
            NumberOfDays = calcResult.Result.Data.NumberOfDays,
            Remarks = request.Remarks?.Trim(),
            Status = LeaveRequestStatus.Draft
        };

        if (request.Submit)
        {
            var submitCheck = await ValidateForSubmitAsync(entity, leaveType, null, cancellationToken);
            if (!submitCheck.Succeeded)
            {
                return Result<LeaveRequestDetailDto>.Failure(submitCheck.Errors);
            }
        }

        _db.LeaveRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.Submit)
        {
            await FinalizeSubmitAsync(entity, leaveType, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result<LeaveRequestDetailDto>.Success(await LeaveRequestDetailLoader.LoadAsync(_db, entity.Id, cancellationToken));
    }

    private async Task FinalizeSubmitAsync(LeaveRequest entity, LeaveType leaveType, CancellationToken cancellationToken)
    {
        if (!leaveType.RequiresApproval)
        {
            entity.Status = LeaveRequestStatus.Approved;
            await LeaveBalanceLedger.DeductAsync(_db, entity, cancellationToken);
            await _notifications.NotifyEmployeeApprovedAsync(entity, cancellationToken);
            return;
        }

        entity.Status = LeaveRequestStatus.Pending;
        await ApprovalStepFactory.CreateStepsAsync(_db, entity, cancellationToken);
        await _notifications.NotifySubmittedAsync(entity, cancellationToken);
    }

    private async Task<Result> ValidateForSubmitAsync(
        LeaveRequest request,
        LeaveType leaveType,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.LeaveRequests.AsNoTracking()
            .Where(x => x.EmployeeId == request.EmployeeId)
            .Select(x => new LeaveRequestRange(x.Id, x.FromDate, x.ToDate, x.Status))
            .ToListAsync(cancellationToken);

        if (LeaveOverlapChecker.HasOverlap(request.FromDate, request.ToDate, existing, excludeId))
        {
            return Result.Failure("Leave dates overlap with an existing pending or approved request.");
        }

        var balance = await GetOrCreateBalanceAsync(request.EmployeeId, request.LeaveTypeId, request.FromDate.Year, cancellationToken);
        if (!LeaveBalanceGuard.HasSufficientBalance(balance.Remaining, request.NumberOfDays, leaveType.AllowNegativeBalance))
        {
            return Result.Failure(
                $"Insufficient leave balance. Remaining: {balance.Remaining}, requested: {request.NumberOfDays}.");
        }

        return Result.Success();
    }

    private async Task<LeaveBalance> GetOrCreateBalanceAsync(
        Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken)
    {
        var balance = await _db.LeaveBalances
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year, cancellationToken);

        if (balance is not null)
        {
            return balance;
        }

        var policy = await _db.LeavePolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LeaveTypeId == leaveTypeId && x.Year == year, cancellationToken);

        balance = new LeaveBalance
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            OpeningBalance = policy?.DefaultOpeningBalance ?? 0
        };
        balance.RecalculateRemaining();
        _db.LeaveBalances.Add(balance);
        await _db.SaveChangesAsync(cancellationToken);
        return balance;
    }
}

public sealed class UpdateLeaveRequestCommandHandler : IRequestHandler<UpdateLeaveRequestCommand, Result<LeaveRequestDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly LeaveNotificationService _notifications;

    public UpdateLeaveRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        LeaveNotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result<LeaveRequestDetailDto>> Handle(UpdateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Leave request not found.");
        }

        if (!await IsOwnerAsync(entity.EmployeeId, cancellationToken))
        {
            return Result<LeaveRequestDetailDto>.Failure("You can only edit your own leave requests.");
        }

        if (entity.Status is not LeaveRequestStatus.Draft and not LeaveRequestStatus.Pending and not LeaveRequestStatus.Approved)
        {
            return Result<LeaveRequestDetailDto>.Failure("Only draft, pending, or approved leave requests can be edited.");
        }

        var wasApproved = entity.Status == LeaveRequestStatus.Approved;
        var wasPending = entity.Status == LeaveRequestStatus.Pending;

        var calcResult = await LeaveDayCalculationService.CalculateAsync(
            _db, entity.EmployeeId, request.LeaveTypeId, request.FromDate, request.ToDate, request.IsHalfDay, cancellationToken);
        if (!calcResult.Result.Succeeded)
        {
            return Result<LeaveRequestDetailDto>.Failure(calcResult.Result.Errors);
        }

        var leaveType = calcResult.LeaveType!;
        var materialChange = ApprovalWorkflow.IsMaterialChange(
            entity,
            request.LeaveTypeId,
            request.FromDate,
            request.ToDate,
            request.IsHalfDay,
            calcResult.Result.Data!.NumberOfDays);

        entity.LeaveTypeId = request.LeaveTypeId;
        entity.FromDate = request.FromDate;
        entity.ToDate = request.ToDate;
        entity.IsHalfDay = request.IsHalfDay;
        entity.HalfDayPeriod = request.IsHalfDay ? request.HalfDayPeriod : null;
        entity.NumberOfDays = calcResult.Result.Data!.NumberOfDays;
        entity.Remarks = request.Remarks?.Trim();

        if (request.Submit && entity.Status == LeaveRequestStatus.Draft)
        {
            var submitCheck = await InvokeSubmitValidationAsync(entity, leaveType, cancellationToken);
            if (!submitCheck.Succeeded)
            {
                return Result<LeaveRequestDetailDto>.Failure(submitCheck.Errors);
            }

            await FinalizeSubmitAsync(entity, leaveType, cancellationToken);
        }
        else if (entity.Status == LeaveRequestStatus.Pending || wasPending)
        {
            var submitCheck = await InvokeSubmitValidationAsync(entity, leaveType, cancellationToken);
            if (!submitCheck.Succeeded)
            {
                return Result<LeaveRequestDetailDto>.Failure(submitCheck.Errors);
            }

            if (materialChange && leaveType.RequiresApproval)
            {
                entity.Status = LeaveRequestStatus.Pending;
                await ApprovalStepFactory.CreateStepsAsync(_db, entity, cancellationToken);
                await _notifications.NotifySubmittedAsync(entity, cancellationToken);
            }
        }
        else if (wasApproved && materialChange)
        {
            var submitCheck = await InvokeSubmitValidationAsync(entity, leaveType, cancellationToken);
            if (!submitCheck.Succeeded)
            {
                return Result<LeaveRequestDetailDto>.Failure(submitCheck.Errors);
            }

            await LeaveBalanceLedger.ReleaseAsync(_db, entity, cancellationToken);
            if (leaveType.RequiresApproval)
            {
                entity.Status = LeaveRequestStatus.Pending;
                await ApprovalStepFactory.CreateStepsAsync(_db, entity, cancellationToken);
                await _notifications.NotifySubmittedAsync(entity, cancellationToken);
            }
            else
            {
                entity.Status = LeaveRequestStatus.Approved;
                await LeaveBalanceLedger.DeductAsync(_db, entity, cancellationToken);
                await _notifications.NotifyEmployeeApprovedAsync(entity, cancellationToken);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveRequestDetailDto>.Success(await LeaveRequestDetailLoader.LoadAsync(_db, entity.Id, cancellationToken));
    }

    private async Task FinalizeSubmitAsync(LeaveRequest entity, LeaveType leaveType, CancellationToken cancellationToken)
    {
        if (!leaveType.RequiresApproval)
        {
            entity.Status = LeaveRequestStatus.Approved;
            await LeaveBalanceLedger.DeductAsync(_db, entity, cancellationToken);
            await _notifications.NotifyEmployeeApprovedAsync(entity, cancellationToken);
            return;
        }

        entity.Status = LeaveRequestStatus.Pending;
        await ApprovalStepFactory.CreateStepsAsync(_db, entity, cancellationToken);
        await _notifications.NotifySubmittedAsync(entity, cancellationToken);
    }

    private async Task<Result> InvokeSubmitValidationAsync(LeaveRequest entity, LeaveType leaveType, CancellationToken ct)
    {
        var existing = await _db.LeaveRequests.AsNoTracking()
            .Where(x => x.EmployeeId == entity.EmployeeId)
            .Select(x => new LeaveRequestRange(x.Id, x.FromDate, x.ToDate, x.Status))
            .ToListAsync(ct);

        if (LeaveOverlapChecker.HasOverlap(entity.FromDate, entity.ToDate, existing, entity.Id))
        {
            return Result.Failure("Leave dates overlap with an existing pending or approved request.");
        }

        var balance = await _db.LeaveBalances
            .FirstOrDefaultAsync(x => x.EmployeeId == entity.EmployeeId && x.LeaveTypeId == entity.LeaveTypeId && x.Year == entity.FromDate.Year, ct);

        if (balance is null)
        {
            var policy = await _db.LeavePolicies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.LeaveTypeId == entity.LeaveTypeId && x.Year == entity.FromDate.Year, ct);
            balance = new LeaveBalance
            {
                EmployeeId = entity.EmployeeId,
                LeaveTypeId = entity.LeaveTypeId,
                Year = entity.FromDate.Year,
                OpeningBalance = policy?.DefaultOpeningBalance ?? 0
            };
            balance.RecalculateRemaining();
        }

        var remaining = balance.Remaining;
        if (!LeaveBalanceGuard.HasSufficientBalance(remaining, entity.NumberOfDays, leaveType.AllowNegativeBalance))
        {
            return Result.Failure($"Insufficient leave balance. Remaining: {remaining}, requested: {entity.NumberOfDays}.");
        }

        return Result.Success();
    }

    private async Task<bool> IsOwnerAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return false;
        }

        var ownId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(_db, _currentUser.UserId.Value, cancellationToken);
        return ownId == employeeId;
    }
}

public sealed class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Result<LeaveRequestDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly LeaveNotificationService _notifications;

    public SubmitLeaveRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        LeaveNotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result<LeaveRequestDetailDto>> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Leave request not found.");
        }

        if (entity.Status != LeaveRequestStatus.Draft)
        {
            return Result<LeaveRequestDetailDto>.Failure("Only draft requests can be submitted.");
        }

        return await new UpdateLeaveRequestCommandHandler(_db, _currentUser, _notifications).Handle(
            new UpdateLeaveRequestCommand(
                entity.Id, entity.LeaveTypeId, entity.FromDate, entity.ToDate,
                entity.IsHalfDay, entity.HalfDayPeriod, entity.Remarks, Submit: true),
            cancellationToken);
    }
}

public sealed class CancelLeaveRequestCommandHandler : IRequestHandler<CancelLeaveRequestCommand, Result<LeaveRequestDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly LeaveNotificationService _notifications;

    public CancelLeaveRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        LeaveNotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result<LeaveRequestDetailDto>> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Leave request not found.");
        }

        if (!_currentUser.UserId.HasValue)
        {
            return Result<LeaveRequestDetailDto>.Failure("Not authenticated.");
        }

        var ownId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(_db, _currentUser.UserId.Value, cancellationToken);
        if (ownId != entity.EmployeeId && !LeaveRequestAccessService.CanViewAll(_currentUser))
        {
            return Result<LeaveRequestDetailDto>.Failure("You can only cancel your own leave requests.");
        }

        var wasApproved = entity.Status == LeaveRequestStatus.Approved;
        var wasDraft = entity.Status == LeaveRequestStatus.Draft;

        if (entity.Status is not LeaveRequestStatus.Draft and not LeaveRequestStatus.Pending and not LeaveRequestStatus.Approved)
        {
            return Result<LeaveRequestDetailDto>.Failure("Only draft, pending, or approved requests can be cancelled.");
        }

        if (wasApproved)
        {
            await LeaveBalanceLedger.ReleaseAsync(_db, entity, cancellationToken);
        }

        entity.Status = LeaveRequestStatus.Cancelled;
        if (!wasDraft)
        {
            await _notifications.NotifyCancelledAsync(entity, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveRequestDetailDto>.Success(await LeaveRequestDetailLoader.LoadAsync(_db, entity.Id, cancellationToken));
    }
}

public sealed class CreateBalanceAdjustmentCommandHandler : IRequestHandler<CreateBalanceAdjustmentCommand, Result<LeaveBalanceDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateBalanceAdjustmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<LeaveBalanceDto>> Handle(CreateBalanceAdjustmentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result<LeaveBalanceDto>.Failure("Not authenticated.");
        }

        var balance = await _db.LeaveBalances
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.LeaveTypeId == request.LeaveTypeId &&
                x.Year == request.Year, cancellationToken);

        if (balance is null)
        {
            var policy = await _db.LeavePolicies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.LeaveTypeId == request.LeaveTypeId && x.Year == request.Year, cancellationToken);

            balance = new LeaveBalance
            {
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                Year = request.Year,
                OpeningBalance = policy?.DefaultOpeningBalance ?? 0
            };
            balance.RecalculateRemaining();
            _db.LeaveBalances.Add(balance);
            await _db.SaveChangesAsync(cancellationToken);
            balance = await _db.LeaveBalances
                .Include(x => x.LeaveType)
                .FirstAsync(x => x.Id == balance.Id, cancellationToken);
        }

        balance.Adjusted += request.DeltaDays;
        balance.RecalculateRemaining();

        if (!balance.LeaveType!.AllowNegativeBalance && balance.Remaining < 0)
        {
            return Result<LeaveBalanceDto>.Failure("Adjustment would result in negative balance.");
        }

        _db.LeaveBalanceAdjustments.Add(new LeaveBalanceAdjustment
        {
            LeaveBalanceId = balance.Id,
            DeltaDays = request.DeltaDays,
            Reason = request.Reason.Trim(),
            AdjustedByUserId = _currentUser.UserId.Value
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveBalanceDto>.Success(GetEmployeeLeaveBalancesQueryHandler.Map(balance));
    }
}
