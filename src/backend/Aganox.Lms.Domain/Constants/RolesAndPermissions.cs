namespace Aganox.Lms.Domain.Constants;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Hr = "HR";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static readonly string[] All =
    [
        Administrator,
        Hr,
        Manager,
        Employee
    ];
}

public static class Permissions
{
    public const string EmployeesView = "Employees.View";
    public const string EmployeesCreate = "Employees.Create";
    public const string EmployeesUpdate = "Employees.Update";
    public const string EmployeesDelete = "Employees.Delete";

    public const string LeaveApply = "Leave.Apply";
    public const string LeaveViewOwn = "Leave.ViewOwn";
    public const string LeaveViewTeam = "Leave.ViewTeam";
    public const string LeaveViewAll = "Leave.ViewAll";

    public const string ApprovalsAct = "Approvals.Act";

    public const string BalancesView = "Balances.View";
    public const string BalancesAdjust = "Balances.Adjust";
    public const string BalancesAdmin = "Balances.Admin";

    public const string LeaveTypesManage = "LeaveTypes.Manage";
    public const string HolidaysManage = "Holidays.Manage";

    public const string ReportsView = "Reports.View";
    public const string ReportsExport = "Reports.Export";

    public const string AdminSettings = "Admin.Settings";
    public const string AdminBackup = "Admin.Backup";
    public const string AdminAudit = "Admin.Audit";
    public const string UsersManage = "Users.Manage";

    public const string AttendanceView = "Attendance.View";
    public const string AttendanceManage = "Attendance.Manage";
    public const string AttendanceImport = "Attendance.Import";

    public const string PayrollConfigView = "Payroll.Config.View";
    public const string PayrollConfigManage = "Payroll.Config.Manage";
    public const string PayrollSalaryView = "Payroll.Salary.View";
    public const string PayrollSalaryManage = "Payroll.Salary.Manage";
    public const string PayrollProcess = "Payroll.Process";
    public const string PayrollApprove = "Payroll.Approve";
    public const string PayrollLock = "Payroll.Lock";
    public const string PayrollPayslipViewOwn = "Payroll.Payslip.ViewOwn";
    public const string PayrollPayslipViewAll = "Payroll.Payslip.ViewAll";
    public const string PayrollReportsView = "Payroll.Reports.View";
    public const string PayrollReportsExport = "Payroll.Reports.Export";
    public const string PayrollArrearsManage = "Payroll.Arrears.Manage";
    public const string PayrollAuditView = "Payroll.Audit.View";

    public static readonly string[] All =
    [
        EmployeesView, EmployeesCreate, EmployeesUpdate, EmployeesDelete,
        LeaveApply, LeaveViewOwn, LeaveViewTeam, LeaveViewAll,
        ApprovalsAct,
        BalancesView, BalancesAdjust, BalancesAdmin,
        LeaveTypesManage, HolidaysManage,
        ReportsView, ReportsExport,
        AdminSettings, AdminBackup, AdminAudit, UsersManage,
        AttendanceView, AttendanceManage, AttendanceImport,
        PayrollConfigView, PayrollConfigManage,
        PayrollSalaryView, PayrollSalaryManage,
        PayrollProcess, PayrollApprove, PayrollLock,
        PayrollPayslipViewOwn, PayrollPayslipViewAll,
        PayrollReportsView, PayrollReportsExport,
        PayrollArrearsManage, PayrollAuditView
    ];
}
