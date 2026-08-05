using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Features.Approvals.Dtos;
using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Approvals.Services;

internal static class ApprovalStepFactory
{
    public static async Task CreateStepsAsync(
        IAppDbContext db,
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.LeaveApprovalSteps
            .Where(x => x.LeaveRequestId == request.Id)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            db.LeaveApprovalSteps.RemoveRange(existing);
        }

        var employee = await db.Employees.AsNoTracking()
            .FirstAsync(x => x.Id == request.EmployeeId, cancellationToken);

        var order = 1;
        if (employee.ManagerId.HasValue)
        {
            db.LeaveApprovalSteps.Add(new LeaveApprovalStep
            {
                LeaveRequestId = request.Id,
                StepOrder = order++,
                ApproverRole = ApprovalRoles.Manager,
                ApproverEmployeeId = employee.ManagerId,
                Status = ApprovalStepStatus.Pending
            });
        }

        db.LeaveApprovalSteps.Add(new LeaveApprovalStep
        {
            LeaveRequestId = request.Id,
            StepOrder = order,
            ApproverRole = ApprovalRoles.Hr,
            Status = ApprovalStepStatus.Pending
        });
    }
}

internal static class LeaveBalanceLedger
{
    public static async Task<LeaveBalance?> GetBalanceAsync(
        IAppDbContext db,
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken) =>
        await db.LeaveBalances
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == employeeId &&
                x.LeaveTypeId == leaveTypeId &&
                x.Year == year, cancellationToken);

    public static async Task DeductAsync(
        IAppDbContext db,
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var balance = await GetBalanceAsync(
            db, request.EmployeeId, request.LeaveTypeId, request.FromDate.Year, cancellationToken);
        if (balance is null)
        {
            return;
        }

        balance.Consumed += request.NumberOfDays;
        balance.RecalculateRemaining();
    }

    public static async Task ReleaseAsync(
        IAppDbContext db,
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var balance = await GetBalanceAsync(
            db, request.EmployeeId, request.LeaveTypeId, request.FromDate.Year, cancellationToken);
        if (balance is null)
        {
            return;
        }

        balance.Consumed = Math.Max(0, balance.Consumed - request.NumberOfDays);
        balance.RecalculateRemaining();
    }
}

internal static class LeaveRequestMapper
{
    public static ApprovalStepDto MapStep(LeaveApprovalStep step) => new(
        step.Id,
        step.StepOrder,
        step.ApproverRole,
        step.ApproverEmployeeId,
        step.ApproverEmployee?.FullName,
        step.Status.ToString(),
        step.Comments,
        step.ActionedAt);

    public static LeaveRequestCommentDto MapComment(LeaveRequestComment comment, string authorName) => new(
        comment.Id,
        authorName,
        comment.Comment,
        comment.CreatedAt);
}
