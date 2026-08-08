namespace Aganox.Lms.Application.Common.Export;

public sealed record ExportTextSet(
    string PayslipTitle,
    string PayrollRegisterTitle,
    string PayrollComponentsTitle,
    string PayrollLopTitle,
    string Period,
    string GeneratedOn,
    string EmployeeCode,
    string EmployeeName,
    string Department,
    string PaidDays,
    string LopDays,
    string Earnings,
    string Deductions,
    string Component,
    string Category,
    string Amount,
    string BasicPay,
    string GrossPay,
    string TotalDeductions,
    string NetPay,
    string Code,
    string LeaveType,
    string FromDate,
    string ToDate,
    string Days,
    string Status,
    string Requests,
    string TotalDaysLabel,
    string Opening,
    string Consumed,
    string Remaining,
    string ComputerGenerated);

public static class ExportLocalization
{
    private static readonly ExportTextSet English = new(
        PayslipTitle: "Payslip",
        PayrollRegisterTitle: "Payroll Register",
        PayrollComponentsTitle: "Payroll Component Summary",
        PayrollLopTitle: "Loss of Pay Report",
        Period: "Period",
        GeneratedOn: "Generated on",
        EmployeeCode: "Employee Code",
        EmployeeName: "Employee Name",
        Department: "Department",
        PaidDays: "Paid Days",
        LopDays: "LOP Days",
        Earnings: "Earnings",
        Deductions: "Deductions",
        Component: "Component",
        Category: "Category",
        Amount: "Amount",
        BasicPay: "Basic Pay",
        GrossPay: "Gross Pay",
        TotalDeductions: "Total Deductions",
        NetPay: "Net Pay",
        Code: "Code",
        LeaveType: "Leave Type",
        FromDate: "From",
        ToDate: "To",
        Days: "Days",
        Status: "Status",
        Requests: "Requests",
        TotalDaysLabel: "Total Days",
        Opening: "Opening",
        Consumed: "Consumed",
        Remaining: "Remaining",
        ComputerGenerated: "This is a computer-generated document.");

    private static readonly ExportTextSet Tamil = new(
        PayslipTitle: "சம்பள சீட்டு",
        PayrollRegisterTitle: "சம்பள பதிவேடு",
        PayrollComponentsTitle: "சம்பள கூறு சுருக்கம்",
        PayrollLopTitle: "LOP அறிக்கை",
        Period: "காலம்",
        GeneratedOn: "உருவாக்கிய தேதி",
        EmployeeCode: "ஊழியர் குறியீடு",
        EmployeeName: "ஊழியர் பெயர்",
        Department: "துறை",
        PaidDays: "பணியாற்றிய நாட்கள்",
        LopDays: "LOP நாட்கள்",
        Earnings: "வருமானம்",
        Deductions: "கழிவுகள்",
        Component: "கூறு",
        Category: "வகை",
        Amount: "தொகை",
        BasicPay: "அடிப்படை சம்பளம்",
        GrossPay: "மொத்த வருமானம்",
        TotalDeductions: "மொத்த கழிவுகள்",
        NetPay: "நிகர சம்பளம்",
        Code: "குறியீடு",
        LeaveType: "விடுப்பு வகை",
        FromDate: "தொடக்கம்",
        ToDate: "முடிவு",
        Days: "நாட்கள்",
        Status: "நிலை",
        Requests: "கோரிக்கைகள்",
        TotalDaysLabel: "மொத்த நாட்கள்",
        Opening: "தொடக்க இருப்பு",
        Consumed: "பயன்படுத்தியது",
        Remaining: "மீதம்",
        ComputerGenerated: "இது கணினி உருவாக்கிய ஆவணம்.");

    public static ExportTextSet Get(ExportLanguage language) =>
        language == ExportLanguage.Tamil ? Tamil : English;

    public static string[] GetLeaveReportHeaders(ExportLanguage language, string reportName)
    {
        var t = Get(language);
        return reportName.ToLowerInvariant() switch
        {
            "employee-leave" =>
            [
                t.EmployeeCode, t.EmployeeName, t.Department, t.LeaveType,
                t.FromDate, t.ToDate, t.Days, t.Status
            ],
            "monthly" or "yearly" =>
            [
                t.Department, t.LeaveType, t.Requests, t.TotalDaysLabel
            ],
            "balances" =>
            [
                t.Code, t.EmployeeName, t.Department, t.LeaveType,
                t.Opening, t.Consumed, t.Remaining
            ],
            "trends" =>
            [
                t.Period, t.Requests, t.TotalDaysLabel
            ],
            _ => []
        };
    }

    public static string GetPayrollReportTitle(ExportLanguage language, string reportType)
    {
        var t = Get(language);
        return reportType.ToLowerInvariant() switch
        {
            "register" => t.PayrollRegisterTitle,
            "components" => t.PayrollComponentsTitle,
            "lop" => t.PayrollLopTitle,
            _ => reportType
        };
    }

    public static string[] GetPayrollReportHeaders(ExportLanguage language, string reportType)
    {
        var t = Get(language);
        return reportType.ToLowerInvariant() switch
        {
            "register" =>
            [
                t.EmployeeCode, t.EmployeeName, t.BasicPay, t.GrossPay,
                t.TotalDeductions, t.NetPay, t.LopDays
            ],
            "components" =>
            [
                t.Code, t.Component, t.Amount
            ],
            "lop" =>
            [
                t.EmployeeCode, t.EmployeeName, t.LopDays, t.NetPay
            ],
            _ => []
        };
    }
}
