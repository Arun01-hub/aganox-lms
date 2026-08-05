using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Services;

public static class ApprovalWorkflow
{
    public static LeaveApprovalStep? GetCurrentPendingStep(IEnumerable<LeaveApprovalStep> steps) =>
        steps.OrderBy(x => x.StepOrder).FirstOrDefault(x => x.Status == ApprovalStepStatus.Pending);

    public static bool CanActOnStep(
        LeaveApprovalStep step,
        IEnumerable<LeaveApprovalStep> allSteps,
        Guid? actorEmployeeId,
        IReadOnlyCollection<string> roles)
    {
        var current = GetCurrentPendingStep(allSteps);
        if (current is null || current.Id != step.Id || step.Status != ApprovalStepStatus.Pending)
        {
            return false;
        }

        if (string.Equals(step.ApproverRole, ApprovalRoles.Manager, StringComparison.OrdinalIgnoreCase))
        {
            return actorEmployeeId.HasValue && step.ApproverEmployeeId == actorEmployeeId;
        }

        if (string.Equals(step.ApproverRole, ApprovalRoles.Hr, StringComparison.OrdinalIgnoreCase))
        {
            return roles.Contains(Roles.Hr, StringComparer.OrdinalIgnoreCase) ||
                   roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    public static bool IsMaterialChange(
        LeaveRequest existing,
        Guid leaveTypeId,
        DateOnly fromDate,
        DateOnly toDate,
        bool isHalfDay,
        decimal numberOfDays) =>
        existing.LeaveTypeId != leaveTypeId ||
        existing.FromDate != fromDate ||
        existing.ToDate != toDate ||
        existing.IsHalfDay != isHalfDay ||
        existing.NumberOfDays != numberOfDays;
}
