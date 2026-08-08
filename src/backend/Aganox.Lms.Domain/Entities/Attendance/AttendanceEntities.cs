using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Entities.Attendance;

public class AttendanceDay : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceDayStatus Status { get; set; } = AttendanceDayStatus.Present;
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;
    public string? Remarks { get; set; }
}
