using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Organization.Dtos;
using Aganox.Lms.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Organization.Employees.Queries;

public sealed record GetEmployeesQuery(
    string? Search,
    Guid? DepartmentId,
    EmployeeStatus? Status,
    Guid? ManagerId,
    string SortBy = "fullName",
    string SortDir = "asc",
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<EmployeeSummaryDto>>;

public sealed record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeSummaryDto?>;

public sealed record GetEmployeeProfileQuery(Guid Id) : IRequest<EmployeeProfileDto?>;

public sealed class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeSummaryDto>>
{
    private readonly IAppDbContext _db;

    public GetEmployeesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<EmployeeSummaryDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);

        var query = _db.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.EmployeeCode.Contains(term) ||
                x.Email.Contains(term));
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == request.DepartmentId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.ManagerId.HasValue)
        {
            query = query.Where(x => x.ManagerId == request.ManagerId.Value);
        }

        query = ApplySorting(query, request.SortBy, request.SortDir);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeSummaryDto(
                x.Id,
                x.EmployeeCode,
                x.FullName,
                x.DepartmentId,
                x.Department!.Name,
                x.Designation,
                x.DateOfJoining,
                x.Status.ToString(),
                x.Email,
                x.Phone,
                x.ManagerId,
                x.Manager != null ? x.Manager.FullName : null,
                x.UserId))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static IQueryable<Domain.Entities.Organization.Employee> ApplySorting(
        IQueryable<Domain.Entities.Organization.Employee> query,
        string sortBy,
        string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "employeecode" => desc ? query.OrderByDescending(x => x.EmployeeCode) : query.OrderBy(x => x.EmployeeCode),
            "dateofjoining" => desc ? query.OrderByDescending(x => x.DateOfJoining) : query.OrderBy(x => x.DateOfJoining),
            "departmentname" => desc
                ? query.OrderByDescending(x => x.Department!.Name)
                : query.OrderBy(x => x.Department!.Name),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => desc ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName)
        };
    }

    internal static EmployeeSummaryDto MapSummary(Domain.Entities.Organization.Employee x) =>
        new(
            x.Id,
            x.EmployeeCode,
            x.FullName,
            x.DepartmentId,
            x.Department!.Name,
            x.Designation,
            x.DateOfJoining,
            x.Status.ToString(),
            x.Email,
            x.Phone,
            x.ManagerId,
            x.Manager?.FullName,
            x.UserId);
}

public sealed class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeSummaryDto?>
{
    private readonly IAppDbContext _db;

    public GetEmployeeByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EmployeeSummaryDto?> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _db.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .Where(x => x.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken) is { } employee
            ? GetEmployeesQueryHandler.MapSummary(employee)
            : null;
    }
}

public sealed class GetEmployeeProfileQueryHandler : IRequestHandler<GetEmployeeProfileQuery, EmployeeProfileDto?>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;

    public GetEmployeeProfileQueryHandler(IAppDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<EmployeeProfileDto?> Handle(GetEmployeeProfileQuery request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .Include(x => x.DirectReports)
                .ThenInclude(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (employee is null)
        {
            return null;
        }

        string? userEmail = employee.UserId.HasValue
            ? await _identity.GetUserEmailAsync(employee.UserId.Value, cancellationToken)
            : null;

        var directReports = employee.DirectReports
            .OrderBy(x => x.FullName)
            .Select(x => GetEmployeesQueryHandler.MapSummary(x) with { ManagerName = employee.FullName })
            .ToList();

        return new EmployeeProfileDto(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.DepartmentId,
            employee.Department!.Name,
            employee.Designation,
            employee.DateOfJoining,
            employee.Status.ToString(),
            employee.Email,
            employee.Phone,
            employee.ManagerId,
            employee.Manager?.FullName,
            employee.UserId,
            userEmail,
            directReports);
    }
}
