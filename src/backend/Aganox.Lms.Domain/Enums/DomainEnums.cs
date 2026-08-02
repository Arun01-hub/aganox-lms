namespace Aganox.Lms.Domain.Enums;

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Terminated = 3
}

public enum LeaveRequestStatus
{
    Draft = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum ApprovalStepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Skipped = 4
}

public enum HalfDayPeriod
{
    AM = 1,
    PM = 2
}

public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Teams = 3,
    Slack = 4,
    InApp = 5
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}
