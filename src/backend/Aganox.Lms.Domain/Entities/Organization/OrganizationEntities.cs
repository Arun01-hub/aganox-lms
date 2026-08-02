using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Entities.Organization;

public class Department : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class LeaveCalendar : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Holiday : SoftDeletableEntity
{
    public Guid LeaveCalendarId { get; set; }
    public LeaveCalendar? LeaveCalendar { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
}

public class Employee : SoftDeletableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Designation { get; set; } = string.Empty;
    public DateOnly DateOfJoining { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public Guid? UserId { get; set; }
    public Guid? LeaveCalendarId { get; set; }
    public LeaveCalendar? LeaveCalendar { get; set; }
}
