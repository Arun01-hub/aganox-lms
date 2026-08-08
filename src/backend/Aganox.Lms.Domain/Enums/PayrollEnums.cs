namespace Aganox.Lms.Domain.Enums;

public enum CustomFieldEntityScope
{
    Employee = 1,
    PayrollRun = 2,
    PayrollEmployee = 3
}

public enum CustomFieldDataType
{
    Text = 1,
    Number = 2,
    Date = 3,
    Boolean = 4,
    Select = 5
}

public enum AttendanceDayStatus
{
    Present = 1,
    Absent = 2,
    HalfDay = 3,
    OnLeave = 4,
    Holiday = 5,
    WeekOff = 6
}

public enum AttendanceSource
{
    Manual = 1,
    LeaveSync = 2,
    Import = 3
}

public enum SalaryComponentCategory
{
    Earning = 1,
    Deduction = 2
}

public enum SalaryCalculationType
{
    Fixed = 1,
    PercentOfBasic = 2,
    PercentOfGross = 3,
    PercentOfComponent = 4,
    Expression = 5
}

public enum PayrollRunStatus
{
    Draft = 1,
    Calculated = 2,
    PendingApproval = 3,
    Approved = 4,
    Locked = 5
}

public enum PayrollArrearStatus
{
    Pending = 1,
    Applied = 2,
    Cancelled = 3
}

public enum PayrollAuditAction
{
    Created = 1,
    Calculated = 2,
    Recalculated = 3,
    SubmittedForApproval = 4,
    Approved = 5,
    Locked = 6,
    PayslipGenerated = 7,
    ArrearApplied = 8
}
