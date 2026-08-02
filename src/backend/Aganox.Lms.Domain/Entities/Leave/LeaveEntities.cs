using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Entities.Leave;

public class LeaveType : SoftDeletableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public bool AllowHalfDay { get; set; } = true;
    public bool AllowNegativeBalance { get; set; }
    public int? MaxDaysPerRequest { get; set; }
    public string? ColorHex { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<LeavePolicy> Policies { get; set; } = new List<LeavePolicy>();
    public ICollection<LeaveBalance> Balances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> Requests { get; set; } = new List<LeaveRequest>();
}

public class LeavePolicy : SoftDeletableEntity
{
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public bool SkipWeekends { get; set; } = true;
    public bool SkipHolidays { get; set; } = true;
    public decimal CarryForwardMaxDays { get; set; }
    public bool YearlyResetEnabled { get; set; } = true;
    public decimal DefaultOpeningBalance { get; set; }
    public DateOnly? AccrualStartDate { get; set; }
}

public class LeaveBalance : SoftDeletableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Credited { get; set; }
    public decimal Consumed { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Remaining { get; set; }

    public ICollection<LeaveBalanceAdjustment> Adjustments { get; set; } = new List<LeaveBalanceAdjustment>();

    public void RecalculateRemaining()
    {
        Remaining = OpeningBalance + Credited + Adjusted - Consumed;
    }
}

public class LeaveBalanceAdjustment : AuditableEntity
{
    public Guid LeaveBalanceId { get; set; }
    public LeaveBalance? LeaveBalance { get; set; }
    public decimal DeltaDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid AdjustedByUserId { get; set; }
}

public class LeaveRequest : SoftDeletableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public bool IsHalfDay { get; set; }
    public HalfDayPeriod? HalfDayPeriod { get; set; }
    public decimal NumberOfDays { get; set; }
    public string? Remarks { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<LeaveApprovalStep> ApprovalSteps { get; set; } = new List<LeaveApprovalStep>();
    public ICollection<LeaveRequestComment> Comments { get; set; } = new List<LeaveRequestComment>();
}

public class LeaveApprovalStep : AuditableEntity
{
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public int StepOrder { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public Guid? ApproverEmployeeId { get; set; }
    public Employee? ApproverEmployee { get; set; }
    public ApprovalStepStatus Status { get; set; } = ApprovalStepStatus.Pending;
    public string? Comments { get; set; }
    public DateTime? ActionedAt { get; set; }
}

public class LeaveRequestComment : AuditableEntity
{
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Comment { get; set; } = string.Empty;
}
