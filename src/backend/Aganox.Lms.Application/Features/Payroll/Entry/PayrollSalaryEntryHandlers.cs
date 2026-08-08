using System.Text.Json;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Payroll.Processing;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services.Payroll;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Payroll.Entry;

public sealed record SalaryEntryComponentDto(
    Guid Id,
    string Code,
    string NameEn,
    string NameTa,
    string Category,
    int SortOrder,
    decimal? RatePercent);

public sealed record SalaryEntryDepartmentDto(Guid Id, string Name, string Code);

public sealed record SalaryEntryContextDto(
    PayrollRunDto Run,
    IReadOnlyList<SalaryEntryDepartmentDto> Departments,
    IReadOnlyList<SalaryEntryComponentDto> Earnings,
    IReadOnlyList<SalaryEntryComponentDto> Deductions,
    bool CanEdit);

public sealed record SalaryEntryLineDto(
    string ComponentCode,
    string ComponentName,
    string Category,
    decimal Amount,
    int SortOrder,
    decimal? RatePercent);

public sealed record SalaryEntryEmployeeDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid ResultId,
    decimal BasicPay,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    decimal PaidDays,
    decimal LopDays,
    bool IsManuallyAdjusted,
    IReadOnlyList<SalaryEntryLineDto> Lines);

public sealed record SaveSalaryEntryLineDto(string ComponentCode, decimal Amount);

public sealed record SaveSalaryEntryCommand(
    Guid PayrollRunId,
    Guid EmployeeId,
    IReadOnlyList<SaveSalaryEntryLineDto> Lines) : IRequest<Result<SalaryEntryEmployeeDto>>;

public sealed class SaveSalaryEntryCommandValidator : AbstractValidator<SaveSalaryEntryCommand>
{
    public SaveSalaryEntryCommandValidator()
    {
        RuleFor(x => x.PayrollRunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Lines).NotNull();
    }
}

public sealed record GetSalaryEntryContextQuery(Guid PayrollRunId) : IRequest<SalaryEntryContextDto?>;

public sealed record GetSalaryEntryQuery(
    Guid PayrollRunId,
    Guid? DepartmentId,
    Guid? EmployeeId) : IRequest<SalaryEntryEmployeeDto?>;

public sealed record ResetSalaryEntryCommand(Guid PayrollRunId, Guid EmployeeId)
    : IRequest<Result<SalaryEntryEmployeeDto>>;

public sealed record DeleteSalaryEntryCommand(Guid PayrollRunId, Guid EmployeeId) : IRequest<Result>;

internal sealed record CalculatedSnapshotModel(
    decimal BasicPaySnapshot,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    decimal PaidDays,
    decimal LopDays,
    List<CalculatedSnapshotLineModel> LineItems);

internal sealed record CalculatedSnapshotLineModel(
    string ComponentCode,
    string ComponentName,
    string Category,
    decimal Amount,
    int SortOrder);

public sealed class GetSalaryEntryContextQueryHandler
    : IRequestHandler<GetSalaryEntryContextQuery, SalaryEntryContextDto?>
{
    private readonly IAppDbContext _db;

    public GetSalaryEntryContextQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SalaryEntryContextDto?> Handle(
        GetSalaryEntryContextQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return null;

        var results = await _db.PayrollEmployeeResults.AsNoTracking()
            .Where(x => x.PayrollRunId == run.Id)
            .ToListAsync(cancellationToken);

        var runDto = PayrollRunMapper.Map(run, results.Count, results.Sum(x => x.NetPay));
        var departments = await _db.Departments.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SalaryEntryDepartmentDto(x.Id, x.Name, x.Code))
            .ToListAsync(cancellationToken);

        var effectiveDate = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
        var components = await LoadComponentsWithRatesAsync(effectiveDate, cancellationToken);

        return new SalaryEntryContextDto(
            runDto,
            departments,
            components.Where(x => x.Category == SalaryComponentCategory.Earning.ToString()).ToList(),
            components.Where(x => x.Category == SalaryComponentCategory.Deduction.ToString()).ToList(),
            PayrollRunGuard.CanEditSalaryEntry(run.Status));
    }

    private async Task<IReadOnlyList<SalaryEntryComponentDto>> LoadComponentsWithRatesAsync(
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var components = await _db.SalaryComponents.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var componentIds = components.Select(x => x.Id).ToList();
        var rules = await _db.SalaryRules.AsNoTracking()
            .Where(x => componentIds.Contains(x.SalaryComponentId) && x.IsActive &&
                        x.EffectiveFrom <= effectiveDate &&
                        (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .ToListAsync(cancellationToken);

        return components.Select(c =>
        {
            var rule = rules
                .Where(r => r.SalaryComponentId == c.Id)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();
            var parameters = SalaryRuleParameters.Parse(rule?.ParametersJson);
            decimal? rate = c.CalculationType is SalaryCalculationType.PercentOfBasic
                or SalaryCalculationType.PercentOfGross
                or SalaryCalculationType.PercentOfComponent
                ? parameters.Rate * 100
                : null;

            return new SalaryEntryComponentDto(
                c.Id,
                c.Code,
                c.NameEn,
                c.NameTa,
                c.Category.ToString(),
                c.SortOrder,
                rate);
        }).ToList();
    }
}

public sealed class GetSalaryEntryQueryHandler : IRequestHandler<GetSalaryEntryQuery, SalaryEntryEmployeeDto?>
{
    private readonly IAppDbContext _db;

    public GetSalaryEntryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SalaryEntryEmployeeDto?> Handle(GetSalaryEntryQuery request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null || run.Status == PayrollRunStatus.Draft)
        {
            return null;
        }

        var query = _db.PayrollEmployeeResults.AsNoTracking()
            .Include(x => x.Employee)!.ThenInclude(e => e!.Department)
            .Include(x => x.LineItems)
            .Where(x => x.PayrollRunId == request.PayrollRunId);

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(x => x.Employee!.DepartmentId == request.DepartmentId.Value);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == request.EmployeeId.Value);
        }

        var result = await query.OrderBy(x => x.Employee!.EmployeeCode).FirstOrDefaultAsync(cancellationToken);
        if (result is null) return null;

        var effectiveDate = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
        var components = await _db.SalaryComponents.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var rules = await _db.SalaryRules.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= effectiveDate &&
                        (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .ToListAsync(cancellationToken);

        var lines = SalaryEntryMapper.BuildLines(result, components, rules);
        return SalaryEntryMapper.MapEmployee(result, lines);
    }
}

public sealed class SaveSalaryEntryCommandHandler : IRequestHandler<SaveSalaryEntryCommand, Result<SalaryEntryEmployeeDto>>
{
    private readonly IAppDbContext _db;

    public SaveSalaryEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<SalaryEntryEmployeeDto>> Handle(
        SaveSalaryEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null)
        {
            return Result<SalaryEntryEmployeeDto>.Failure("At least one salary line is required.");
        }

        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return Result<SalaryEntryEmployeeDto>.Failure("Payroll run not found.");
        if (!PayrollRunGuard.CanEditSalaryEntry(run.Status))
        {
            return Result<SalaryEntryEmployeeDto>.Failure("Salary entry cannot be edited in the current payroll run status.");
        }

        var result = await _db.PayrollEmployeeResults
            .Include(x => x.Employee)!.ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(
                x => x.PayrollRunId == request.PayrollRunId && x.EmployeeId == request.EmployeeId,
                cancellationToken);

        if (result is null)
        {
            return Result<SalaryEntryEmployeeDto>.Failure("Payroll result not found for this employee. Run Calculate first.");
        }

        var existingLineItems = await _db.PayrollLineItems.AsNoTracking()
            .Where(x => x.PayrollEmployeeResultId == result.Id)
            .ToListAsync(cancellationToken);

        var components = await _db.SalaryComponents.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var amountByCode = request.Lines
            .GroupBy(x => x.ComponentCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Amount, StringComparer.OrdinalIgnoreCase);
        var rebuiltLines = new List<(string Code, string Name, SalaryComponentCategory Category, decimal Amount, int SortOrder)>();

        foreach (var component in components.OrderBy(x => x.SortOrder))
        {
            if (!amountByCode.TryGetValue(component.Code, out var amount))
            {
                continue;
            }

            if (amount == 0 && component.Code is not ("BASIC" or "PF"))
            {
                continue;
            }

            rebuiltLines.Add((
                component.Code,
                component.NameEn,
                component.Category,
                Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                component.SortOrder));
        }

        foreach (var existing in existingLineItems.Where(x =>
                     x.ComponentCode is "LOP" or "ARREAR" &&
                     !rebuiltLines.Any(r => r.Code.Equals(x.ComponentCode, StringComparison.OrdinalIgnoreCase))))
        {
            rebuiltLines.Add((
                existing.ComponentCode,
                existing.ComponentName,
                existing.Category,
                existing.Amount,
                existing.SortOrder));
        }

        foreach (var (code, amount) in amountByCode)
        {
            if (rebuiltLines.Any(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (amount == 0 && code is not ("BASIC" or "PF" or "LOP" or "ARREAR"))
            {
                continue;
            }

            var existing = existingLineItems.FirstOrDefault(x =>
                x.ComponentCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            rebuiltLines.Add((
                code,
                existing?.ComponentName ?? code,
                existing?.Category ?? SalaryComponentCategory.Earning,
                Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                existing?.SortOrder ?? 999));
        }

        var totals = PayrollCalculationEngine.ComputeTotalsFromLineItems(
            rebuiltLines.Select(x => (x.Category, x.Amount)));

        var basicLine = rebuiltLines.FirstOrDefault(x => x.Code.Equals("BASIC", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(basicLine.Code))
        {
            result.BasicPaySnapshot = basicLine.Amount;
        }

        result.GrossPay = totals.GrossPay;
        result.TotalDeductions = totals.TotalDeductions;
        result.NetPay = totals.NetPay;
        result.IsManuallyAdjusted = true;
        result.OutputSnapshotJson = PayrollSnapshotBuilder.BuildOutputSnapshot(new
        {
            result.GrossPay,
            result.TotalDeductions,
            result.NetPay,
            manual = true
        });

        try
        {
            await SalaryEntryLinePersistence.ReplaceAsync(_db, result.Id, rebuiltLines, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SalaryEntryEmployeeDto>.Failure(
                "Payroll data changed while saving. Refresh salary entry and try again.");
        }

        await SalaryEntryLinePersistence.LoadLineItemsAsync(_db, result, cancellationToken);

        var effectiveDate = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
        var rules = await _db.SalaryRules.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= effectiveDate &&
                        (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .ToListAsync(cancellationToken);

        var lines = SalaryEntryMapper.BuildLines(result, components, rules);
        return Result<SalaryEntryEmployeeDto>.Success(SalaryEntryMapper.MapEmployee(result, lines));
    }
}

public sealed class ResetSalaryEntryCommandHandler : IRequestHandler<ResetSalaryEntryCommand, Result<SalaryEntryEmployeeDto>>
{
    private readonly IAppDbContext _db;

    public ResetSalaryEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<SalaryEntryEmployeeDto>> Handle(
        ResetSalaryEntryCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return Result<SalaryEntryEmployeeDto>.Failure("Payroll run not found.");
        if (!PayrollRunGuard.CanEditSalaryEntry(run.Status))
        {
            return Result<SalaryEntryEmployeeDto>.Failure("Salary entry cannot be reset in the current payroll run status.");
        }

        var result = await _db.PayrollEmployeeResults
            .Include(x => x.Employee)!.ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(
                x => x.PayrollRunId == request.PayrollRunId && x.EmployeeId == request.EmployeeId,
                cancellationToken);

        if (result is null)
        {
            return Result<SalaryEntryEmployeeDto>.Failure("Payroll result not found for this employee.");
        }

        if (string.IsNullOrWhiteSpace(result.CalculatedSnapshotJson))
        {
            return Result<SalaryEntryEmployeeDto>.Failure("No calculated snapshot available to reset.");
        }

        CalculatedSnapshotModel? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<CalculatedSnapshotModel>(
                result.CalculatedSnapshotJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return Result<SalaryEntryEmployeeDto>.Failure("Invalid calculated snapshot.");
        }

        if (snapshot is null)
        {
            return Result<SalaryEntryEmployeeDto>.Failure("Invalid calculated snapshot.");
        }

        result.BasicPaySnapshot = snapshot.BasicPaySnapshot;
        result.GrossPay = snapshot.GrossPay;
        result.TotalDeductions = snapshot.TotalDeductions;
        result.NetPay = snapshot.NetPay;
        result.PaidDays = snapshot.PaidDays;
        result.LopDays = snapshot.LopDays;
        result.IsManuallyAdjusted = false;

        var snapshotLines = snapshot.LineItems
            .OrderBy(x => x.SortOrder)
            .Select(line =>
            {
                if (!Enum.TryParse<SalaryComponentCategory>(line.Category, true, out var category))
                {
                    category = SalaryComponentCategory.Earning;
                }

                return (
                    line.ComponentCode,
                    line.ComponentName,
                    category,
                    line.ComponentCode.Equals("BASIC", StringComparison.OrdinalIgnoreCase) && line.Amount == 0
                        ? snapshot.BasicPaySnapshot
                        : line.Amount,
                    line.SortOrder);
            })
            .ToList();

        try
        {
            await SalaryEntryLinePersistence.ReplaceAsync(_db, result.Id, snapshotLines, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SalaryEntryEmployeeDto>.Failure(
                "Payroll data changed while resetting. Refresh salary entry and try again.");
        }

        await SalaryEntryLinePersistence.LoadLineItemsAsync(_db, result, cancellationToken);

        var components = await _db.SalaryComponents.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var effectiveDate = new DateOnly(run.Year, run.Month, DateTime.DaysInMonth(run.Year, run.Month));
        var rules = await _db.SalaryRules.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= effectiveDate &&
                        (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .ToListAsync(cancellationToken);

        var lines = SalaryEntryMapper.BuildLines(result, components, rules);
        return Result<SalaryEntryEmployeeDto>.Success(SalaryEntryMapper.MapEmployee(result, lines));
    }
}

public sealed class DeleteSalaryEntryCommandHandler : IRequestHandler<DeleteSalaryEntryCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteSalaryEntryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteSalaryEntryCommand request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns.FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken);
        if (run is null) return Result.Failure("Payroll run not found.");
        if (!PayrollRunGuard.CanEditSalaryEntry(run.Status))
        {
            return Result.Failure("Salary entry cannot be deleted in the current payroll run status.");
        }

        var result = await _db.PayrollEmployeeResults
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(
                x => x.PayrollRunId == request.PayrollRunId && x.EmployeeId == request.EmployeeId,
                cancellationToken);

        if (result is null) return Result.Failure("Payroll result not found for this employee.");

        _db.PayrollLineItems.RemoveRange(result.LineItems);
        _db.PayrollEmployeeResults.Remove(result);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class SalaryEntryLinePersistence
{
    public static async Task ReplaceAsync(
        IAppDbContext db,
        Guid payrollEmployeeResultId,
        IReadOnlyList<(string Code, string Name, SalaryComponentCategory Category, decimal Amount, int SortOrder)> lines,
        CancellationToken cancellationToken)
    {
        if (db is DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries<PayrollLineItem>()
                         .Where(e => e.Entity.PayrollEmployeeResultId == payrollEmployeeResultId)
                         .ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        var existing = await db.PayrollLineItems
            .Where(x => x.PayrollEmployeeResultId == payrollEmployeeResultId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            db.PayrollLineItems.RemoveRange(existing);
        }

        foreach (var line in lines.OrderBy(x => x.SortOrder))
        {
            db.PayrollLineItems.Add(new PayrollLineItem
            {
                PayrollEmployeeResultId = payrollEmployeeResultId,
                ComponentCode = line.Code,
                ComponentName = line.Name,
                Category = line.Category,
                Amount = line.Amount,
                SortOrder = line.SortOrder
            });
        }
    }

    public static async Task LoadLineItemsAsync(
        IAppDbContext db,
        PayrollEmployeeResult result,
        CancellationToken cancellationToken)
    {
        if (db is DbContext context)
        {
            await context.Entry(result).Collection(x => x.LineItems).LoadAsync(cancellationToken);
        }
    }
}

internal static class SalaryEntryMapper
{
    public static IReadOnlyList<SalaryEntryLineDto> BuildLines(
        PayrollEmployeeResult result,
        IReadOnlyList<SalaryComponent> components,
        IReadOnlyList<SalaryRule> rules)
    {
        var amountByCode = result.LineItems.ToDictionary(x => x.ComponentCode, x => x.Amount, StringComparer.OrdinalIgnoreCase);
        var lines = new List<SalaryEntryLineDto>();

        foreach (var component in components.OrderBy(x => x.SortOrder))
        {
            var rule = rules
                .Where(r => r.SalaryComponentId == component.Id)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();
            var parameters = SalaryRuleParameters.Parse(rule?.ParametersJson);
            decimal? rate = component.CalculationType is SalaryCalculationType.PercentOfBasic
                or SalaryCalculationType.PercentOfGross
                or SalaryCalculationType.PercentOfComponent
                ? parameters.Rate * 100
                : null;

            amountByCode.TryGetValue(component.Code, out var amount);
            if (component.Code.Equals("BASIC", StringComparison.OrdinalIgnoreCase) && amount == 0)
            {
                amount = result.BasicPaySnapshot;
            }

            lines.Add(new SalaryEntryLineDto(
                component.Code,
                component.NameEn,
                component.Category.ToString(),
                amount,
                component.SortOrder,
                rate));
        }

        foreach (var extra in result.LineItems.Where(x =>
                     !components.Any(c => c.Code.Equals(x.ComponentCode, StringComparison.OrdinalIgnoreCase))))
        {
            lines.Add(new SalaryEntryLineDto(
                extra.ComponentCode,
                extra.ComponentName,
                extra.Category.ToString(),
                extra.Amount,
                extra.SortOrder,
                null));
        }

        return lines.OrderBy(x => x.SortOrder).ToList();
    }

    public static SalaryEntryEmployeeDto MapEmployee(
        PayrollEmployeeResult result,
        IReadOnlyList<SalaryEntryLineDto> lines) => new(
        result.EmployeeId,
        result.Employee!.EmployeeCode,
        result.Employee.FullName,
        result.Employee.DepartmentId,
        result.Employee.Department?.Name,
        result.Id,
        result.BasicPaySnapshot,
        result.GrossPay,
        result.TotalDeductions,
        result.NetPay,
        result.PaidDays,
        result.LopDays,
        result.IsManuallyAdjusted,
        lines);
}
