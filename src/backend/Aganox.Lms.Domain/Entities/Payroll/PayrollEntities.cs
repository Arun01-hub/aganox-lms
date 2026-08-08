using Aganox.Lms.Domain.Entities.Common;
using Aganox.Lms.Domain.Entities.Organization;
using Aganox.Lms.Domain.Enums;

namespace Aganox.Lms.Domain.Entities.Payroll;

public class SalaryComponent : SoftDeletableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameTa { get; set; } = string.Empty;
    public SalaryComponentCategory Category { get; set; }
    public SalaryCalculationType CalculationType { get; set; }
    public string? BaseComponentCode { get; set; }
    public int SortOrder { get; set; }
    public bool IsTaxable { get; set; }
    public bool IncludeInGross { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public ICollection<SalaryRule> Rules { get; set; } = new List<SalaryRule>();
}

public class SalaryRule : SoftDeletableEntity
{
    public Guid SalaryComponentId { get; set; }
    public SalaryComponent? SalaryComponent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public class EmployeeSalaryRevision : SoftDeletableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal BasicPay { get; set; }
    public string? Remarks { get; set; }

    public ICollection<EmployeeSalaryComponentOverride> ComponentOverrides { get; set; } =
        new List<EmployeeSalaryComponentOverride>();
}

public class EmployeeSalaryComponentOverride : AuditableEntity
{
    public Guid EmployeeSalaryRevisionId { get; set; }
    public EmployeeSalaryRevision? EmployeeSalaryRevision { get; set; }
    public Guid SalaryComponentId { get; set; }
    public SalaryComponent? SalaryComponent { get; set; }
    public decimal FixedAmount { get; set; }
}

public class PayrollRun : AuditableEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public DateTime? CalculatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? LockedByUserId { get; set; }
    public string? Notes { get; set; }

    public ICollection<PayrollEmployeeResult> EmployeeResults { get; set; } = new List<PayrollEmployeeResult>();
    public ICollection<PayrollRunAudit> AuditEntries { get; set; } = new List<PayrollRunAudit>();
}

public class PayrollEmployeeResult : AuditableEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal BasicPaySnapshot { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public decimal PaidDays { get; set; }
    public decimal LopDays { get; set; }
    public bool IsManuallyAdjusted { get; set; }
    public string? CalculatedSnapshotJson { get; set; }
    public string InputSnapshotJson { get; set; } = "{}";
    public string OutputSnapshotJson { get; set; } = "{}";

    public ICollection<PayrollLineItem> LineItems { get; set; } = new List<PayrollLineItem>();
}

public class PayrollLineItem : EntityBase
{
    public Guid PayrollEmployeeResultId { get; set; }
    public PayrollEmployeeResult? PayrollEmployeeResult { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public SalaryComponentCategory Category { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

public class PayrollArrear : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int SourceYear { get; set; }
    public int SourceMonth { get; set; }
    public Guid? TargetPayrollRunId { get; set; }
    public PayrollRun? TargetPayrollRun { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public PayrollArrearStatus Status { get; set; } = PayrollArrearStatus.Pending;
}

public class PayrollRunAudit : EntityBase
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public PayrollAuditAction Action { get; set; }
    public Guid? UserId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
