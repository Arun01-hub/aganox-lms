using Aganox.Lms.Domain.Constants;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services;

namespace Aganox.Lms.Domain.Tests;

public class ApprovalWorkflowTests
{
    [Fact]
    public void CanActOnStep_allows_assigned_manager()
    {
        var managerId = Guid.NewGuid();
        var steps = new[]
        {
            new LeaveApprovalStep
            {
                Id = Guid.NewGuid(),
                StepOrder = 1,
                ApproverRole = ApprovalRoles.Manager,
                ApproverEmployeeId = managerId,
                Status = ApprovalStepStatus.Pending
            }
        };

        Assert.True(ApprovalWorkflow.CanActOnStep(
            steps[0], steps, managerId, [Roles.Manager]));
    }

    [Fact]
    public void CanActOnStep_allows_hr_on_hr_step_after_manager_approved()
    {
        var steps = new[]
        {
            new LeaveApprovalStep
            {
                Id = Guid.NewGuid(),
                StepOrder = 1,
                ApproverRole = ApprovalRoles.Manager,
                Status = ApprovalStepStatus.Approved
            },
            new LeaveApprovalStep
            {
                Id = Guid.NewGuid(),
                StepOrder = 2,
                ApproverRole = ApprovalRoles.Hr,
                Status = ApprovalStepStatus.Pending
            }
        };

        Assert.True(ApprovalWorkflow.CanActOnStep(
            steps[1], steps, null, [Roles.Hr]));
    }

    [Fact]
    public void IsMaterialChange_detects_date_change()
    {
        var existing = new LeaveRequest
        {
            LeaveTypeId = Guid.NewGuid(),
            FromDate = new DateOnly(2026, 8, 1),
            ToDate = new DateOnly(2026, 8, 2),
            IsHalfDay = false,
            NumberOfDays = 2
        };

        Assert.True(ApprovalWorkflow.IsMaterialChange(
            existing, existing.LeaveTypeId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), false, 3));
    }
}
