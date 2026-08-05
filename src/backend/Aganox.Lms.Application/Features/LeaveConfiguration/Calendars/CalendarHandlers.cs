using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Domain.Entities.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveConfiguration.Calendars;

public sealed record GetLeaveCalendarsQuery() : IRequest<IReadOnlyList<LeaveCalendarDto>>;
public sealed record GetLeaveCalendarByIdQuery(Guid Id) : IRequest<LeaveCalendarDto?>;
public sealed record CreateLeaveCalendarCommand(string Name, bool IsDefault) : IRequest<Result<LeaveCalendarDto>>;
public sealed record UpdateLeaveCalendarCommand(Guid Id, string Name, bool IsDefault) : IRequest<Result<LeaveCalendarDto>>;
public sealed record DeleteLeaveCalendarCommand(Guid Id) : IRequest<Result>;

public sealed class CreateLeaveCalendarCommandValidator : AbstractValidator<CreateLeaveCalendarCommand>
{
    public CreateLeaveCalendarCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class GetLeaveCalendarsQueryHandler : IRequestHandler<GetLeaveCalendarsQuery, IReadOnlyList<LeaveCalendarDto>>
{
    private readonly IAppDbContext _db;
    public GetLeaveCalendarsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LeaveCalendarDto>> Handle(GetLeaveCalendarsQuery request, CancellationToken cancellationToken) =>
        await _db.LeaveCalendars.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new LeaveCalendarDto(x.Id, x.Name, x.IsDefault, x.Holidays.Count))
            .ToListAsync(cancellationToken);
}

public sealed class GetLeaveCalendarByIdQueryHandler : IRequestHandler<GetLeaveCalendarByIdQuery, LeaveCalendarDto?>
{
    private readonly IAppDbContext _db;
    public GetLeaveCalendarByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LeaveCalendarDto?> Handle(GetLeaveCalendarByIdQuery request, CancellationToken cancellationToken) =>
        await _db.LeaveCalendars.AsNoTracking().Where(x => x.Id == request.Id)
            .Select(x => new LeaveCalendarDto(x.Id, x.Name, x.IsDefault, x.Holidays.Count))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class CreateLeaveCalendarCommandHandler : IRequestHandler<CreateLeaveCalendarCommand, Result<LeaveCalendarDto>>
{
    private readonly IAppDbContext _db;
    public CreateLeaveCalendarCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<LeaveCalendarDto>> Handle(CreateLeaveCalendarCommand request, CancellationToken cancellationToken)
    {
        if (request.IsDefault)
            await ClearDefaultAsync(cancellationToken);

        var entity = new LeaveCalendar { Name = request.Name.Trim(), IsDefault = request.IsDefault };
        _db.LeaveCalendars.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveCalendarDto>.Success(new LeaveCalendarDto(entity.Id, entity.Name, entity.IsDefault, 0));
    }

    private async Task ClearDefaultAsync(CancellationToken cancellationToken)
    {
        var defaults = await _db.LeaveCalendars.Where(x => x.IsDefault).ToListAsync(cancellationToken);
        foreach (var cal in defaults) cal.IsDefault = false;
    }
}

public sealed class UpdateLeaveCalendarCommandHandler : IRequestHandler<UpdateLeaveCalendarCommand, Result<LeaveCalendarDto>>
{
    private readonly IAppDbContext _db;
    public UpdateLeaveCalendarCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<LeaveCalendarDto>> Handle(UpdateLeaveCalendarCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveCalendars.Include(x => x.Holidays)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result<LeaveCalendarDto>.Failure("Calendar not found.");

        if (request.IsDefault && !entity.IsDefault)
        {
            var defaults = await _db.LeaveCalendars.Where(x => x.IsDefault && x.Id != request.Id).ToListAsync(cancellationToken);
            foreach (var cal in defaults) cal.IsDefault = false;
        }

        entity.Name = request.Name.Trim();
        entity.IsDefault = request.IsDefault;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LeaveCalendarDto>.Success(new LeaveCalendarDto(entity.Id, entity.Name, entity.IsDefault, entity.Holidays.Count));
    }
}

public sealed class DeleteLeaveCalendarCommandHandler : IRequestHandler<DeleteLeaveCalendarCommand, Result>
{
    private readonly IAppDbContext _db;
    public DeleteLeaveCalendarCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteLeaveCalendarCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LeaveCalendars.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result.Failure("Calendar not found.");
        if (entity.IsDefault) return Result.Failure("Cannot delete the default calendar.");
        if (await _db.Employees.AnyAsync(x => x.LeaveCalendarId == request.Id, cancellationToken))
            return Result.Failure("Cannot delete a calendar assigned to employees.");

        _db.LeaveCalendars.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
