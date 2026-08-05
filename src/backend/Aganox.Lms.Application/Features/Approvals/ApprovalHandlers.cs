using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Approvals.Dtos;
using Aganox.Lms.Application.Features.Approvals.Services;
using Aganox.Lms.Application.Features.LeaveRequests;
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

namespace Aganox.Lms.Application.Features.Approvals;

public sealed record GetPendingApprovalsQuery(int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<PendingApprovalDto>>;

public sealed record GetApprovalHistoryQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ApprovalHistoryDto>>;

public sealed record ApproveLeaveRequestCommand(Guid LeaveRequestId, string? Comments)
    : IRequest<Result<LeaveRequestDetailDto>>;

public sealed record RejectLeaveRequestCommand(Guid LeaveRequestId, string Comments)
    : IRequest<Result<LeaveRequestDetailDto>>;

public sealed class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(1000);
    }
}

public sealed class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, PagedResult<PendingApprovalDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPendingApprovalsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PendingApprovalDto>> Handle(
        GetPendingApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);
        var actorEmployeeId = _currentUser.UserId.HasValue
            ? await LeaveRequestAccessService.GetEmployeeIdForUserAsync(_db, _currentUser.UserId.Value, cancellationToken)
            : null;

        var pendingRequests = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .Include(x => x.ApprovalSteps)
            .Where(x => x.Status == LeaveRequestStatus.Pending)
            .ToListAsync(cancellationToken);

        var items = new List<PendingApprovalDto>();
        foreach (var leaveRequest in pendingRequests)
        {
            var currentStep = ApprovalWorkflow.GetCurrentPendingStep(leaveRequest.ApprovalSteps);
            if (currentStep is null)
            {
                continue;
            }

            if (!ApprovalWorkflow.CanActOnStep(
                    currentStep,
                    leaveRequest.ApprovalSteps,
                    actorEmployeeId,
                    _currentUser.Roles))
            {
                continue;
            }

            items.Add(new PendingApprovalDto(
                leaveRequest.Id,
                leaveRequest.EmployeeId,
                leaveRequest.Employee!.FullName,
                leaveRequest.LeaveType!.Name,
                leaveRequest.FromDate,
                leaveRequest.ToDate,
                leaveRequest.NumberOfDays,
                leaveRequest.Status.ToString(),
                currentStep.StepOrder,
                currentStep.ApproverRole,
                leaveRequest.CreatedAt));
        }

        var ordered = items.OrderBy(x => x.SubmittedAt).ToList();
        var totalCount = ordered.Count;
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<PendingApprovalDto>
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public sealed class GetApprovalHistoryQueryHandler : IRequestHandler<GetApprovalHistoryQuery, PagedResult<ApprovalHistoryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetApprovalHistoryQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ApprovalHistoryDto>> Handle(
        GetApprovalHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);
        var actorEmployeeId = _currentUser.UserId.HasValue
            ? await LeaveRequestAccessService.GetEmployeeIdForUserAsync(_db, _currentUser.UserId.Value, cancellationToken)
            : null;

        var query = _db.LeaveApprovalSteps.AsNoTracking()
            .Where(x => x.Status != ApprovalStepStatus.Pending)
            .AsQueryable();

        if (!_currentUser.Roles.Contains(Roles.Administrator) && !_currentUser.Roles.Contains(Roles.Hr))
        {
            query = query.Where(x =>
                x.ApproverEmployeeId == actorEmployeeId ||
                (x.LeaveRequest != null && x.LeaveRequest.EmployeeId == actorEmployeeId));
        }

        query = query.OrderByDescending(x => x.ActionedAt ?? x.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ApprovalHistoryDto(
                x.LeaveRequestId,
                x.LeaveRequest!.EmployeeId,
                x.LeaveRequest.Employee!.FullName,
                x.LeaveRequest.LeaveType!.Name,
                x.LeaveRequest.FromDate,
                x.LeaveRequest.ToDate,
                x.LeaveRequest.NumberOfDays,
                x.LeaveRequest.Status.ToString(),
                x.Status.ToString(),
                x.ApproverRole,
                x.ApproverEmployee != null ? x.ApproverEmployee.FullName : null,
                x.Comments,
                x.ActionedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ApprovalHistoryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Result<LeaveRequestDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly LeaveNotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    public ApproveLeaveRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        LeaveNotificationService notifications,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _clock = clock;
    }

    public async Task<Result<LeaveRequestDetailDto>> Handle(
        ApproveLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Not authenticated.");
        }

        var entity = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .Include(x => x.ApprovalSteps)
            .FirstOrDefaultAsync(x => x.Id == request.LeaveRequestId, cancellationToken);

        if (entity is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Leave request not found.");
        }

        if (entity.Status != LeaveRequestStatus.Pending)
        {
            return Result<LeaveRequestDetailDto>.Failure("Only pending leave requests can be approved.");
        }

        var actorEmployeeId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(
            _db, _currentUser.UserId.Value, cancellationToken);
        var currentStep = ApprovalWorkflow.GetCurrentPendingStep(entity.ApprovalSteps);
        if (currentStep is null ||
            !ApprovalWorkflow.CanActOnStep(currentStep, entity.ApprovalSteps, actorEmployeeId, _currentUser.Roles))
        {
            return Result<LeaveRequestDetailDto>.Failure("You are not authorized to approve this request at the current step.");
        }

        currentStep.Status = ApprovalStepStatus.Approved;
        currentStep.Comments = request.Comments?.Trim();
        currentStep.ActionedAt = _clock.UtcNow;

        await AddCommentAsync(entity.Id, request.Comments, cancellationToken);

        var nextStep = ApprovalWorkflow.GetCurrentPendingStep(entity.ApprovalSteps);
        if (nextStep is not null)
        {
            await _notifications.NotifyHrPendingAsync(entity, entity.Employee!.FullName, cancellationToken);
        }
        else
        {
            entity.Status = LeaveRequestStatus.Approved;
            await LeaveBalanceLedger.DeductAsync(_db, entity, cancellationToken);
            await _notifications.NotifyEmployeeApprovedAsync(entity, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveRequestDetailDto>.Success(await LoadDetailAsync(entity.Id, cancellationToken));
    }

    private async Task AddCommentAsync(Guid leaveRequestId, string? comments, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(comments) || _currentUser.UserId is null)
        {
            return;
        }

        _db.LeaveRequestComments.Add(new LeaveRequestComment
        {
            LeaveRequestId = leaveRequestId,
            AuthorUserId = _currentUser.UserId.Value,
            Comment = comments.Trim()
        });
        await Task.CompletedTask;
    }

    private async Task<LeaveRequestDetailDto> LoadDetailAsync(Guid id, CancellationToken cancellationToken) =>
        (await new GetLeaveRequestByIdQueryHandler(_db, _currentUser)
            .Handle(new GetLeaveRequestByIdQuery(id), cancellationToken))!;
}

public sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand, Result<LeaveRequestDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly LeaveNotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    public RejectLeaveRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        LeaveNotificationService notifications,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _clock = clock;
    }

    public async Task<Result<LeaveRequestDetailDto>> Handle(
        RejectLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Not authenticated.");
        }

        var entity = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .Include(x => x.ApprovalSteps)
            .FirstOrDefaultAsync(x => x.Id == request.LeaveRequestId, cancellationToken);

        if (entity is null)
        {
            return Result<LeaveRequestDetailDto>.Failure("Leave request not found.");
        }

        if (entity.Status != LeaveRequestStatus.Pending)
        {
            return Result<LeaveRequestDetailDto>.Failure("Only pending leave requests can be rejected.");
        }

        var actorEmployeeId = await LeaveRequestAccessService.GetEmployeeIdForUserAsync(
            _db, _currentUser.UserId.Value, cancellationToken);
        var currentStep = ApprovalWorkflow.GetCurrentPendingStep(entity.ApprovalSteps);
        if (currentStep is null ||
            !ApprovalWorkflow.CanActOnStep(currentStep, entity.ApprovalSteps, actorEmployeeId, _currentUser.Roles))
        {
            return Result<LeaveRequestDetailDto>.Failure("You are not authorized to reject this request at the current step.");
        }

        currentStep.Status = ApprovalStepStatus.Rejected;
        currentStep.Comments = request.Comments.Trim();
        currentStep.ActionedAt = _clock.UtcNow;

        foreach (var step in entity.ApprovalSteps.Where(x => x.Status == ApprovalStepStatus.Pending && x.Id != currentStep.Id))
        {
            step.Status = ApprovalStepStatus.Skipped;
        }

        entity.Status = LeaveRequestStatus.Rejected;

        _db.LeaveRequestComments.Add(new LeaveRequestComment
        {
            LeaveRequestId = entity.Id,
            AuthorUserId = _currentUser.UserId.Value,
            Comment = request.Comments.Trim()
        });

        await _notifications.NotifyEmployeeRejectedAsync(entity, request.Comments, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveRequestDetailDto>.Success(await LoadDetailAsync(entity.Id, cancellationToken));
    }

    private async Task<LeaveRequestDetailDto> LoadDetailAsync(Guid id, CancellationToken cancellationToken) =>
        (await new GetLeaveRequestByIdQueryHandler(_db, _currentUser)
            .Handle(new GetLeaveRequestByIdQuery(id), cancellationToken))!;
}
