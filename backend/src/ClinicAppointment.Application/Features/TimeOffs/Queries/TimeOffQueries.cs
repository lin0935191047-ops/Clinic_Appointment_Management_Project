using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.TimeOffs.DTOs;
using ClinicAppointment.Application.Features.TimeOffs.Services;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.TimeOffs.Queries;

public record GetDoctorTimeOffsQuery(
    Guid DoctorId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool OnlyActive = true
) : IRequest<List<DoctorTimeOffDto>>;

public record GetDoctorScheduleBlocksQuery(
    Guid DoctorId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool OnlyActive = true
) : IRequest<List<DoctorScheduleBlockDto>>;

public record CheckDoctorTimeOffConflictQuery(
    Guid DoctorId,
    DateTime StartAt,
    DateTime EndAt
) : IRequest<CheckDoctorTimeOffConflictResult>;

public record CheckDoctorTimeOffConflictResult(
    bool HasConflict,
    List<AppointmentConflictDto> AffectedAppointments
);

public class TimeOffQueryHandlers :
    IRequestHandler<GetDoctorTimeOffsQuery, List<DoctorTimeOffDto>>,
    IRequestHandler<GetDoctorScheduleBlocksQuery, List<DoctorScheduleBlockDto>>,
    IRequestHandler<CheckDoctorTimeOffConflictQuery, CheckDoctorTimeOffConflictResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDoctorTimeOffService _timeOffService;

    public TimeOffQueryHandlers(
        IApplicationDbContext context,
        IDoctorTimeOffService timeOffService)
    {
        _context = context;
        _timeOffService = timeOffService;
    }

    public async Task<List<DoctorTimeOffDto>> Handle(GetDoctorTimeOffsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DoctorTimeOffs
            .Include(t => t.Doctor)
            .Where(t => t.DoctorId == request.DoctorId)
            .AsQueryable();

        if (request.OnlyActive)
        {
            query = query.Where(t => t.Status == TimeOffStatus.Active);
        }

        if (request.FromDate.HasValue)
        {
            var utcFrom = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.EndAt >= utcFrom);
        }

        if (request.ToDate.HasValue)
        {
            var utcTo = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.StartAt <= utcTo);
        }

        var list = await query
            .OrderBy(t => t.StartAt)
            .ToListAsync(cancellationToken);

        return list.Select(t => new DoctorTimeOffDto(
            Id: t.Id,
            DoctorId: t.DoctorId,
            DoctorName: t.Doctor?.FullName ?? string.Empty,
            DoctorCode: t.Doctor?.DoctorCode ?? string.Empty,
            StartAt: t.StartAt,
            EndAt: t.EndAt,
            Reason: t.Reason,
            Status: t.Status,
            CreatedByUserId: t.CreatedByUserId,
            CreatedAt: t.CreatedAt
        )).ToList();
    }

    public async Task<List<DoctorScheduleBlockDto>> Handle(GetDoctorScheduleBlocksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DoctorScheduleBlocks
            .Include(b => b.Doctor)
            .Where(b => b.DoctorId == request.DoctorId)
            .AsQueryable();

        if (request.OnlyActive)
        {
            query = query.Where(b => b.IsActive);
        }

        if (request.FromDate.HasValue)
        {
            var utcFrom = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(b => b.EndAt >= utcFrom);
        }

        if (request.ToDate.HasValue)
        {
            var utcTo = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(b => b.StartAt <= utcTo);
        }

        var list = await query
            .OrderBy(b => b.StartAt)
            .ToListAsync(cancellationToken);

        return list.Select(b => new DoctorScheduleBlockDto(
            Id: b.Id,
            DoctorId: b.DoctorId,
            DoctorName: b.Doctor?.FullName ?? string.Empty,
            DoctorCode: b.Doctor?.DoctorCode ?? string.Empty,
            StartAt: b.StartAt,
            EndAt: b.EndAt,
            BlockType: b.BlockType,
            Reason: b.Reason,
            IsActive: b.IsActive,
            CreatedByUserId: b.CreatedByUserId,
            CreatedAt: b.CreatedAt
        )).ToList();
    }

    public async Task<CheckDoctorTimeOffConflictResult> Handle(CheckDoctorTimeOffConflictQuery request, CancellationToken cancellationToken)
    {
        var hasConflict = await _timeOffService.HasTimeOffOrBlockConflictAsync(
            request.DoctorId, request.StartAt, request.EndAt, cancellationToken);

        var affected = await _timeOffService.GetAffectedAppointmentsAsync(
            request.DoctorId, request.StartAt, request.EndAt, cancellationToken);

        return new CheckDoctorTimeOffConflictResult(hasConflict, affected);
    }
}
