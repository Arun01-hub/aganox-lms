using Aganox.Lms.Application.Features.Attendance;
using Aganox.Lms.Domain.Entities.Attendance;
using Aganox.Lms.Domain.Entities.Leave;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Application.Tests;

public class AttendanceLeaveOverlayTests
{
    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void BuildApprovedClElLeaveMap_includes_counted_working_days_only()
    {
        var clType = new LeaveType { Id = ClTypeId, Code = "CL", Name = "Casual Leave" };
        var request = new LeaveRequest
        {
            EmployeeId = EmployeeId,
            LeaveTypeId = ClTypeId,
            LeaveType = clType,
            FromDate = new DateOnly(2026, 8, 3),
            ToDate = new DateOnly(2026, 8, 7),
            Status = LeaveRequestStatus.Approved,
            IsHalfDay = false
        };

        var map = AttendanceService.BuildApprovedClElLeaveMap(
            EmployeeId,
            2026,
            8,
            new HashSet<DateOnly>(),
            [request],
            new Dictionary<Guid, LeavePolicy>());

        Assert.Equal(5, map.Count);
        Assert.Equal(AttendanceDayStatus.OnLeave, map[new DateOnly(2026, 8, 3)].Status);
        Assert.Equal("CL", map[new DateOnly(2026, 8, 3)].LeaveTypeCode);
        Assert.False(map.ContainsKey(new DateOnly(2026, 8, 1)));
    }

    [Fact]
    public void ApplyLeaveOverlay_respects_manual_attendance()
    {
        var dayStatuses = new Dictionary<DateOnly, AttendanceDayStatus>
        {
            [new DateOnly(2026, 8, 4)] = AttendanceDayStatus.Present,
            [new DateOnly(2026, 8, 5)] = AttendanceDayStatus.Present
        };

        var storedDays = new List<AttendanceDay>
        {
            new()
            {
                EmployeeId = EmployeeId,
                Date = new DateOnly(2026, 8, 4),
                Status = AttendanceDayStatus.Present,
                Source = AttendanceSource.Manual
            }
        };

        var overlay = new Dictionary<DateOnly, ApprovedLeaveDayInfo>
        {
            [new DateOnly(2026, 8, 4)] = new(AttendanceDayStatus.OnLeave, "CL"),
            [new DateOnly(2026, 8, 5)] = new(AttendanceDayStatus.OnLeave, "EL")
        };

        AttendanceService.ApplyLeaveOverlay(dayStatuses, storedDays, overlay);

        Assert.Equal(AttendanceDayStatus.Present, dayStatuses[new DateOnly(2026, 8, 4)]);
        Assert.Equal(AttendanceDayStatus.OnLeave, dayStatuses[new DateOnly(2026, 8, 5)]);
    }
}
