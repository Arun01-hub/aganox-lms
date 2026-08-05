using Aganox.Lms.Application.Common.Interfaces;
using Aganox.Lms.Application.Common.Models;
using Aganox.Lms.Application.Features.LeaveConfiguration.Dtos;
using Aganox.Lms.Domain.Entities.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aganox.Lms.Application.Features.LeaveConfiguration.Holidays;

public sealed record GetHolidaysQuery(Guid? CalendarId, int? Year) : IRequest<IReadOnlyList<HolidayDto>>;
public sealed record GetHolidayByIdQuery(Guid Id) : IRequest<HolidayDto?>;
public sealed record CreateHolidayCommand(Guid LeaveCalendarId, DateOnly HolidayDate, string Name, bool IsOptional)
    : IRequest<Result<HolidayDto>>;
public sealed record UpdateHolidayCommand(Guid Id, DateOnly HolidayDate, string Name, bool IsOptional)
    : IRequest<Result<HolidayDto>>;
public sealed record DeleteHolidayCommand(Guid Id) : IRequest<Result>;

public sealed class GetHolidayByIdQueryHandler : IRequestHandler<GetHolidayByIdQuery, HolidayDto?>
{
    private readonly IAppDbContext _db;
    public GetHolidayByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<HolidayDto?> Handle(GetHolidayByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _db.Holidays.AsNoTracking().Include(x => x.LeaveCalendar)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        return entity is null ? null : new HolidayDto(
            entity.Id, entity.LeaveCalendarId, entity.LeaveCalendar!.Name,
            entity.HolidayDate, entity.Name, entity.IsOptional);
    }
}

public sealed class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayCommandValidator()
    {
        RuleFor(x => x.LeaveCalendarId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, IReadOnlyList<HolidayDto>>
{
    private readonly IAppDbContext _db;
    public GetHolidaysQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<HolidayDto>> Handle(GetHolidaysQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Holidays.AsNoTracking().Include(x => x.LeaveCalendar).AsQueryable();
        if (request.CalendarId.HasValue) query = query.Where(x => x.LeaveCalendarId == request.CalendarId.Value);
        if (request.Year.HasValue) query = query.Where(x => x.HolidayDate.Year == request.Year.Value);
        return await query.OrderBy(x => x.HolidayDate)
            .Select(x => new HolidayDto(x.Id, x.LeaveCalendarId, x.LeaveCalendar!.Name, x.HolidayDate, x.Name, x.IsOptional))
            .ToListAsync(cancellationToken);
    }
}

public sealed class CreateHolidayCommandHandler : IRequestHandler<CreateHolidayCommand, Result<HolidayDto>>
{
    private readonly IAppDbContext _db;
    public CreateHolidayCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<HolidayDto>> Handle(CreateHolidayCommand request, CancellationToken cancellationToken)
    {
        var calendar = await _db.LeaveCalendars.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LeaveCalendarId, cancellationToken);
        if (calendar is null) return Result<HolidayDto>.Failure("Calendar not found.");

        if (await _db.Holidays.AnyAsync(x =>
                x.LeaveCalendarId == request.LeaveCalendarId && x.HolidayDate == request.HolidayDate, cancellationToken))
            return Result<HolidayDto>.Failure("A holiday already exists on this date.");

        var entity = new Holiday
        {
            LeaveCalendarId = request.LeaveCalendarId,
            HolidayDate = request.HolidayDate,
            Name = request.Name.Trim(),
            IsOptional = request.IsOptional
        };
        _db.Holidays.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<HolidayDto>.Success(new HolidayDto(
            entity.Id, entity.LeaveCalendarId, calendar.Name, entity.HolidayDate, entity.Name, entity.IsOptional));
    }
}

public sealed class UpdateHolidayCommandHandler : IRequestHandler<UpdateHolidayCommand, Result<HolidayDto>>
{
    private readonly IAppDbContext _db;
    public UpdateHolidayCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<HolidayDto>> Handle(UpdateHolidayCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Holidays.Include(x => x.LeaveCalendar)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result<HolidayDto>.Failure("Holiday not found.");

        if (await _db.Holidays.AnyAsync(x =>
                x.LeaveCalendarId == entity.LeaveCalendarId &&
                x.HolidayDate == request.HolidayDate &&
                x.Id != request.Id, cancellationToken))
            return Result<HolidayDto>.Failure("A holiday already exists on this date.");

        entity.HolidayDate = request.HolidayDate;
        entity.Name = request.Name.Trim();
        entity.IsOptional = request.IsOptional;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<HolidayDto>.Success(new HolidayDto(
            entity.Id, entity.LeaveCalendarId, entity.LeaveCalendar!.Name,
            entity.HolidayDate, entity.Name, entity.IsOptional));
    }
}

public sealed class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, Result>
{
    private readonly IAppDbContext _db;
    public DeleteHolidayCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Holidays.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity is null) return Result.Failure("Holiday not found.");
        _db.Holidays.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
