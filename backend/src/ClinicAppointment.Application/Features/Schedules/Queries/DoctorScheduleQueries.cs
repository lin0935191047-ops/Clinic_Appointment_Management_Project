using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Schedules.Commands;
using ClinicAppointment.Application.Features.Schedules.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Schedules.Queries;

public record GetDoctorSchedulesQuery(
    Guid DoctorId,
    DateOnly? EffectiveDate = null,
    bool? IsActive = null
) : IRequest<List<DoctorScheduleDto>>;

public record GetDoctorScheduleByIdQuery(Guid Id) : IRequest<DoctorScheduleDto?>;

public record GetPublicDoctorSchedulesQuery(Guid DoctorId) : IRequest<List<DoctorScheduleDto>>;

public class DoctorScheduleQueryHandlers :
    IRequestHandler<GetDoctorSchedulesQuery, List<DoctorScheduleDto>>,
    IRequestHandler<GetDoctorScheduleByIdQuery, DoctorScheduleDto?>,
    IRequestHandler<GetPublicDoctorSchedulesQuery, List<DoctorScheduleDto>>
{
    private readonly IApplicationDbContext _context;

    public DoctorScheduleQueryHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorScheduleDto>> Handle(GetDoctorSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DoctorSchedules
            .Include(s => s.Doctor)
            .Where(s => s.DoctorId == request.DoctorId)
            .AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        if (request.EffectiveDate.HasValue)
        {
            var date = request.EffectiveDate.Value;
            query = query.Where(s => s.EffectiveFrom <= date && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= date));
        }

        var list = await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return list.Select(s => new DoctorScheduleDto(
            Id: s.Id,
            DoctorId: s.DoctorId,
            DoctorName: s.Doctor?.FullName ?? string.Empty,
            DoctorCode: s.Doctor?.DoctorCode ?? string.Empty,
            DayOfWeek: s.DayOfWeek,
            DayOfWeekName: DoctorScheduleCommandHandlers.GetDayOfWeekName(s.DayOfWeek),
            StartTime: s.StartTime,
            EndTime: s.EndTime,
            EffectiveFrom: s.EffectiveFrom,
            EffectiveTo: s.EffectiveTo,
            SlotDurationMinutes: s.SlotDurationMinutes,
            IsActive: s.IsActive
        )).ToList();
    }

    public async Task<DoctorScheduleDto?> Handle(GetDoctorScheduleByIdQuery request, CancellationToken cancellationToken)
    {
        var s = await _context.DoctorSchedules
            .Include(x => x.Doctor)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (s == null)
            return null;

        return new DoctorScheduleDto(
            Id: s.Id,
            DoctorId: s.DoctorId,
            DoctorName: s.Doctor?.FullName ?? string.Empty,
            DoctorCode: s.Doctor?.DoctorCode ?? string.Empty,
            DayOfWeek: s.DayOfWeek,
            DayOfWeekName: DoctorScheduleCommandHandlers.GetDayOfWeekName(s.DayOfWeek),
            StartTime: s.StartTime,
            EndTime: s.EndTime,
            EffectiveFrom: s.EffectiveFrom,
            EffectiveTo: s.EffectiveTo,
            SlotDurationMinutes: s.SlotDurationMinutes,
            IsActive: s.IsActive
        );
    }

    public async Task<List<DoctorScheduleDto>> Handle(GetPublicDoctorSchedulesQuery request, CancellationToken cancellationToken)
    {
        // Public view: Bác sĩ phải active, chuyên khoa của bác sĩ phải active, và schedule phải active
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var list = await _context.DoctorSchedules
            .Include(s => s.Doctor)
            .ThenInclude(d => d!.Specialty)
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive)
            .Where(s => s.Doctor != null && s.Doctor.IsActive)
            .Where(s => s.Doctor!.Specialty != null && s.Doctor.Specialty.IsActive)
            .Where(s => !s.EffectiveTo.HasValue || s.EffectiveTo.Value >= today)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return list.Select(s => new DoctorScheduleDto(
            Id: s.Id,
            DoctorId: s.DoctorId,
            DoctorName: s.Doctor?.FullName ?? string.Empty,
            DoctorCode: s.Doctor?.DoctorCode ?? string.Empty,
            DayOfWeek: s.DayOfWeek,
            DayOfWeekName: DoctorScheduleCommandHandlers.GetDayOfWeekName(s.DayOfWeek),
            StartTime: s.StartTime,
            EndTime: s.EndTime,
            EffectiveFrom: s.EffectiveFrom,
            EffectiveTo: s.EffectiveTo,
            SlotDurationMinutes: s.SlotDurationMinutes,
            IsActive: s.IsActive
        )).ToList();
    }
}
