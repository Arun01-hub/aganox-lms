using System.Text.Json;
using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Attendance;
using Aganox.Lms.Domain.Entities.Payroll;
using Aganox.Lms.Domain.Enums;
using Aganox.Lms.Domain.Services.Payroll;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Payroll.Configuration;

public sealed record SalaryComponentDto(
    Guid Id,
    string Code,
    string NameEn,
    string NameTa,
    string Category,
    string CalculationType,
    string? BaseComponentCode,
    int SortOrder,
    bool IsTaxable,
    bool IncludeInGross,
    bool IsActive);

public sealed record SalaryRuleDto(
    Guid Id,
    Guid SalaryComponentId,
    string ComponentCode,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int Priority,
    string ParametersJson,
    bool IsActive);

public sealed record EmployeeSalaryRevisionDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal BasicPay,
    string? Remarks,
    IReadOnlyList<EmployeeSalaryOverrideDto> Overrides);

public sealed record EmployeeSalaryOverrideDto(Guid SalaryComponentId, string ComponentCode, decimal FixedAmount);

public sealed record GetSalaryComponentsQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<SalaryComponentDto>>;
public sealed record CreateSalaryComponentCommand(
    string Code, string NameEn, string NameTa, SalaryComponentCategory Category,
    SalaryCalculationType CalculationType, string? BaseComponentCode, int SortOrder,
    bool IsTaxable, bool IncludeInGross, bool IsActive) : IRequest<Result<SalaryComponentDto>>;
public sealed record UpdateSalaryComponentCommand(
    Guid Id, string Code, string NameEn, string NameTa, SalaryComponentCategory Category,
    SalaryCalculationType CalculationType, string? BaseComponentCode, int SortOrder,
    bool IsTaxable, bool IncludeInGross, bool IsActive) : IRequest<Result<SalaryComponentDto>>;

public sealed record GetSalaryRulesQuery(Guid? ComponentId) : IRequest<IReadOnlyList<SalaryRuleDto>>;
public sealed record CreateSalaryRuleCommand(
    Guid SalaryComponentId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Priority,
    string ParametersJson, bool IsActive) : IRequest<Result<SalaryRuleDto>>;
public sealed record UpdateSalaryRuleCommand(
    Guid Id, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Priority,
    string ParametersJson, bool IsActive) : IRequest<Result<SalaryRuleDto>>;

public sealed record GetEmployeeSalaryRevisionsQuery(Guid EmployeeId) : IRequest<IReadOnlyList<EmployeeSalaryRevisionDto>>;
public sealed record CreateEmployeeSalaryRevisionCommand(
    Guid EmployeeId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal BasicPay, string? Remarks,
    IReadOnlyList<EmployeeSalaryOverrideDto>? Overrides) : IRequest<Result<EmployeeSalaryRevisionDto>>;
public sealed record UpdateEmployeeSalaryRevisionCommand(
    Guid Id, DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal BasicPay, string? Remarks,
    IReadOnlyList<EmployeeSalaryOverrideDto>? Overrides) : IRequest<Result<EmployeeSalaryRevisionDto>>;

public sealed record PreviewEmployeeSalaryLineDto(
    string ComponentCode,
    string ComponentName,
    string Category,
    decimal Amount,
    int SortOrder);

public sealed record PreviewEmployeeSalaryDto(
    decimal BasicPay,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    IReadOnlyList<PreviewEmployeeSalaryLineDto> Lines);

public sealed record PreviewEmployeeSalaryQuery(
    decimal BasicPay,
    DateOnly EffectiveFrom,
    IReadOnlyList<EmployeeSalaryOverrideDto>? Overrides) : IRequest<PreviewEmployeeSalaryDto>;

public sealed class CreateSalaryComponentCommandValidator : AbstractValidator<CreateSalaryComponentCommand>
{
    public CreateSalaryComponentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateEmployeeSalaryRevisionCommandValidator : AbstractValidator<CreateEmployeeSalaryRevisionCommand>
{
    public CreateEmployeeSalaryRevisionCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BasicPay).GreaterThanOrEqualTo(0);
    }
}

internal static class PayrollConfigMapper
{
    public static SalaryComponentDto MapComponent(SalaryComponent x) => new(
        x.Id, x.Code, x.NameEn, x.NameTa, x.Category.ToString(), x.CalculationType.ToString(),
        x.BaseComponentCode, x.SortOrder, x.IsTaxable, x.IncludeInGross, x.IsActive);

    public static SalaryRuleDto MapRule(SalaryRule x, string componentCode) => new(
        x.Id, x.SalaryComponentId, componentCode, x.EffectiveFrom, x.EffectiveTo,
        x.Priority, x.ParametersJson, x.IsActive);

    public static async Task<EmployeeSalaryRevisionDto> MapRevisionAsync(
        IAppDbContext db,
        EmployeeSalaryRevision x,
        CancellationToken cancellationToken)
    {
        var employee = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == x.EmployeeId, cancellationToken);
        var overrides = await db.EmployeeSalaryComponentOverrides.AsNoTracking()
            .Include(o => o.SalaryComponent)
            .Where(o => o.EmployeeSalaryRevisionId == x.Id)
            .ToListAsync(cancellationToken);

        return new EmployeeSalaryRevisionDto(
            x.Id,
            x.EmployeeId,
            employee.FullName,
            x.EffectiveFrom,
            x.EffectiveTo,
            x.BasicPay,
            x.Remarks,
            overrides.Select(o => new EmployeeSalaryOverrideDto(
                o.SalaryComponentId,
                o.SalaryComponent!.Code,
                o.FixedAmount)).ToList());
    }
}

public sealed class GetSalaryComponentsQueryHandler : IRequestHandler<GetSalaryComponentsQuery, IReadOnlyList<SalaryComponentDto>>
{
    private readonly IAppDbContext _db;
    public GetSalaryComponentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalaryComponentDto>> Handle(GetSalaryComponentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.SalaryComponents.AsNoTracking();
        if (request.ActiveOnly) query = query.Where(x => x.IsActive);
        var items = await query.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return items.Select(PayrollConfigMapper.MapComponent).ToList();
    }
}

public sealed class CreateSalaryComponentCommandHandler : IRequestHandler<CreateSalaryComponentCommand, Result<SalaryComponentDto>>
{
    private readonly IAppDbContext _db;
    public CreateSalaryComponentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<SalaryComponentDto>> Handle(CreateSalaryComponentCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.SalaryComponents.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return Result<SalaryComponentDto>.Failure("Component code already exists.");
        }

        var entity = new SalaryComponent
        {
            Code = code,
            NameEn = request.NameEn.Trim(),
            NameTa = request.NameTa.Trim(),
            Category = request.Category,
            CalculationType = request.CalculationType,
            BaseComponentCode = request.BaseComponentCode?.Trim().ToUpperInvariant(),
            SortOrder = request.SortOrder,
            IsTaxable = request.IsTaxable,
            IncludeInGross = request.IncludeInGross,
            IsActive = request.IsActive
        };
        _db.SalaryComponents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<SalaryComponentDto>.Success(PayrollConfigMapper.MapComponent(entity));
    }
}

public sealed class UpdateSalaryComponentCommandHandler : IRequestHandler<UpdateSalaryComponentCommand, Result<SalaryComponentDto>>
{
    private readonly IAppDbContext _db;
    public UpdateSalaryComponentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<SalaryComponentDto>> Handle(UpdateSalaryComponentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.SalaryComponents.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result<SalaryComponentDto>.Failure("Salary component not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.SalaryComponents.AnyAsync(x => x.Code == code && x.Id != request.Id, cancellationToken))
        {
            return Result<SalaryComponentDto>.Failure("Component code already exists.");
        }

        entity.Code = code;
        entity.NameEn = request.NameEn.Trim();
        entity.NameTa = request.NameTa.Trim();
        entity.Category = request.Category;
        entity.CalculationType = request.CalculationType;
        entity.BaseComponentCode = request.BaseComponentCode?.Trim().ToUpperInvariant();
        entity.SortOrder = request.SortOrder;
        entity.IsTaxable = request.IsTaxable;
        entity.IncludeInGross = request.IncludeInGross;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<SalaryComponentDto>.Success(PayrollConfigMapper.MapComponent(entity));
    }
}

public sealed class GetSalaryRulesQueryHandler : IRequestHandler<GetSalaryRulesQuery, IReadOnlyList<SalaryRuleDto>>
{
    private readonly IAppDbContext _db;
    public GetSalaryRulesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalaryRuleDto>> Handle(GetSalaryRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.SalaryRules.AsNoTracking().Include(x => x.SalaryComponent).AsQueryable();
        if (request.ComponentId.HasValue) query = query.Where(x => x.SalaryComponentId == request.ComponentId);
        var items = await query.OrderBy(x => x.SalaryComponent!.SortOrder).ThenByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken);
        return items.Select(x => PayrollConfigMapper.MapRule(x, x.SalaryComponent!.Code)).ToList();
    }
}

public sealed class CreateSalaryRuleCommandHandler : IRequestHandler<CreateSalaryRuleCommand, Result<SalaryRuleDto>>
{
    private readonly IAppDbContext _db;
    public CreateSalaryRuleCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<SalaryRuleDto>> Handle(CreateSalaryRuleCommand request, CancellationToken cancellationToken)
    {
        var component = await _db.SalaryComponents.FirstOrDefaultAsync(x => x.Id == request.SalaryComponentId, cancellationToken);
        if (component is null) return Result<SalaryRuleDto>.Failure("Salary component not found.");

        var entity = new SalaryRule
        {
            SalaryComponentId = request.SalaryComponentId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Priority = request.Priority,
            ParametersJson = request.ParametersJson,
            IsActive = request.IsActive
        };
        _db.SalaryRules.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<SalaryRuleDto>.Success(PayrollConfigMapper.MapRule(entity, component.Code));
    }
}

public sealed class UpdateSalaryRuleCommandHandler : IRequestHandler<UpdateSalaryRuleCommand, Result<SalaryRuleDto>>
{
    private readonly IAppDbContext _db;
    public UpdateSalaryRuleCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<SalaryRuleDto>> Handle(UpdateSalaryRuleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.SalaryRules.Include(x => x.SalaryComponent)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result<SalaryRuleDto>.Failure("Salary rule not found.");

        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Priority = request.Priority;
        entity.ParametersJson = request.ParametersJson;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<SalaryRuleDto>.Success(PayrollConfigMapper.MapRule(entity, entity.SalaryComponent!.Code));
    }
}

public sealed class GetEmployeeSalaryRevisionsQueryHandler
    : IRequestHandler<GetEmployeeSalaryRevisionsQuery, IReadOnlyList<EmployeeSalaryRevisionDto>>
{
    private readonly IAppDbContext _db;
    public GetEmployeeSalaryRevisionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmployeeSalaryRevisionDto>> Handle(
        GetEmployeeSalaryRevisionsQuery request,
        CancellationToken cancellationToken)
    {
        var revisions = await _db.EmployeeSalaryRevisions.AsNoTracking()
            .Where(x => x.EmployeeId == request.EmployeeId)
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var list = new List<EmployeeSalaryRevisionDto>();
        foreach (var revision in revisions)
        {
            list.Add(await PayrollConfigMapper.MapRevisionAsync(_db, revision, cancellationToken));
        }

        return list;
    }
}

public sealed class CreateEmployeeSalaryRevisionCommandHandler
    : IRequestHandler<CreateEmployeeSalaryRevisionCommand, Result<EmployeeSalaryRevisionDto>>
{
    private readonly IAppDbContext _db;
    public CreateEmployeeSalaryRevisionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<EmployeeSalaryRevisionDto>> Handle(
        CreateEmployeeSalaryRevisionCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _db.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken))
        {
            return Result<EmployeeSalaryRevisionDto>.Failure("Employee not found.");
        }

        var entity = new EmployeeSalaryRevision
        {
            EmployeeId = request.EmployeeId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            BasicPay = request.BasicPay,
            Remarks = request.Remarks
        };
        _db.EmployeeSalaryRevisions.Add(entity);
        await AddOverridesAsync(entity, request.Overrides, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<EmployeeSalaryRevisionDto>.Success(
            await PayrollConfigMapper.MapRevisionAsync(_db, entity, cancellationToken));
    }

    private async Task AddOverridesAsync(
        EmployeeSalaryRevision revision,
        IReadOnlyList<EmployeeSalaryOverrideDto>? overrides,
        CancellationToken cancellationToken)
    {
        if (overrides is null) return;
        foreach (var item in overrides)
        {
            _db.EmployeeSalaryComponentOverrides.Add(new EmployeeSalaryComponentOverride
            {
                EmployeeSalaryRevisionId = revision.Id,
                SalaryComponentId = item.SalaryComponentId,
                FixedAmount = item.FixedAmount
            });
        }

        await Task.CompletedTask;
    }
}

public sealed class UpdateEmployeeSalaryRevisionCommandHandler
    : IRequestHandler<UpdateEmployeeSalaryRevisionCommand, Result<EmployeeSalaryRevisionDto>>
{
    private readonly IAppDbContext _db;
    public UpdateEmployeeSalaryRevisionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<EmployeeSalaryRevisionDto>> Handle(
        UpdateEmployeeSalaryRevisionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.EmployeeSalaryRevisions
            .Include(x => x.ComponentOverrides)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result<EmployeeSalaryRevisionDto>.Failure("Salary revision not found.");

        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.BasicPay = request.BasicPay;
        entity.Remarks = request.Remarks;

        _db.EmployeeSalaryComponentOverrides.RemoveRange(entity.ComponentOverrides);
        if (request.Overrides is not null)
        {
            foreach (var item in request.Overrides)
            {
                _db.EmployeeSalaryComponentOverrides.Add(new EmployeeSalaryComponentOverride
                {
                    EmployeeSalaryRevisionId = entity.Id,
                    SalaryComponentId = item.SalaryComponentId,
                    FixedAmount = item.FixedAmount
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<EmployeeSalaryRevisionDto>.Success(
            await PayrollConfigMapper.MapRevisionAsync(_db, entity, cancellationToken));
    }
}

public sealed class PreviewEmployeeSalaryQueryHandler
    : IRequestHandler<PreviewEmployeeSalaryQuery, PreviewEmployeeSalaryDto>
{
    private readonly IAppDbContext _db;

    public PreviewEmployeeSalaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PreviewEmployeeSalaryDto> Handle(
        PreviewEmployeeSalaryQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await PayrollConfigurationLoader.LoadRulesForDateAsync(
            _db,
            request.EffectiveFrom,
            revision: null,
            request.Overrides,
            cancellationToken);

        var calc = PayrollCalculationEngine.Calculate(
            request.BasicPay,
            new AttendanceMonthSummaryInput(0, 0, 0),
            rules);

        return new PreviewEmployeeSalaryDto(
            calc.BasicPay,
            calc.GrossPay,
            calc.TotalDeductions,
            calc.NetPay,
            calc.Lines.Select(x => new PreviewEmployeeSalaryLineDto(
                x.ComponentCode,
                x.ComponentName,
                x.Category.ToString(),
                x.Amount,
                x.SortOrder)).ToList());
    }
}

public static class PayrollConfigurationLoader
{
    public static async Task<IReadOnlyList<ResolvedSalaryRule>> LoadRulesForDateAsync(
        IAppDbContext db,
        DateOnly effectiveDate,
        EmployeeSalaryRevision? revision,
        CancellationToken cancellationToken) =>
        await LoadRulesForDateAsync(db, effectiveDate, revision, previewOverrides: null, cancellationToken);

    public static async Task<IReadOnlyList<ResolvedSalaryRule>> LoadRulesForDateAsync(
        IAppDbContext db,
        DateOnly effectiveDate,
        EmployeeSalaryRevision? revision,
        IReadOnlyList<EmployeeSalaryOverrideDto>? previewOverrides,
        CancellationToken cancellationToken)
    {
        var components = await db.SalaryComponents.AsNoTracking()
            .Include(x => x.Rules)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var sources = components.Select(c => new SalaryComponentRuleSource(
            c.Id,
            c.Code,
            c.NameEn,
            c.Category,
            c.CalculationType,
            c.BaseComponentCode,
            c.SortOrder,
            c.IncludeInGross,
            c.IsActive,
            c.Rules.Select(r => new SalaryRuleSource(
                r.IsActive, r.EffectiveFrom, r.EffectiveTo, r.Priority, r.ParametersJson)).ToList())).ToList();

        Dictionary<string, decimal>? overrides = null;
        if (revision is not null)
        {
            var overrideRows = await db.EmployeeSalaryComponentOverrides.AsNoTracking()
                .Include(x => x.SalaryComponent)
                .Where(x => x.EmployeeSalaryRevisionId == revision.Id)
                .ToListAsync(cancellationToken);
            overrides = overrideRows.ToDictionary(x => x.SalaryComponent!.Code, x => x.FixedAmount, StringComparer.OrdinalIgnoreCase);
        }
        else if (previewOverrides is { Count: > 0 })
        {
            overrides = previewOverrides.ToDictionary(
                x => x.ComponentCode,
                x => x.FixedAmount,
                StringComparer.OrdinalIgnoreCase);
        }

        return SalaryRuleResolver.Resolve(effectiveDate, sources, overrides);
    }

    public static async Task<EmployeeSalaryRevision?> GetEffectiveRevisionAsync(
        IAppDbContext db,
        Guid employeeId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken) =>
        await db.EmployeeSalaryRevisions.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId &&
                        x.EffectiveFrom <= effectiveDate &&
                        (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
}
