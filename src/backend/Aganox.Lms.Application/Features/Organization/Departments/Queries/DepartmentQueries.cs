using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.Organization.Dtos;
using Aganox.Lms.Domain.Entities.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.Organization.Departments.Queries;

public sealed record GetDepartmentsQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<DepartmentDto>>;

public sealed record GetDepartmentByIdQuery(Guid Id) : IRequest<DepartmentDto?>;

public sealed class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IAppDbContext _db;

    public GetDepartmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Departments.AsNoTracking();
        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new DepartmentDto(
                x.Id,
                x.Name,
                x.Code,
                x.IsActive,
                x.Employees.Count))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto?>
{
    private readonly IAppDbContext _db;

    public GetDepartmentByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DepartmentDto?> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _db.Departments
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new DepartmentDto(
                x.Id,
                x.Name,
                x.Code,
                x.IsActive,
                x.Employees.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
