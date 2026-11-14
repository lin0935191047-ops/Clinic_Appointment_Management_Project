using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.TimeOffs.DTOs;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.TimeOffs.Services;

public class DoctorTimeOffService : IDoctorTimeOffService
{
    private readonly IApplicationDbContext _context;

    public DoctorTimeOffService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasTimeOffOrBlockConflictAsync(
        Guid doctorId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default)
    {
        var utcStart = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);

        // 1. Kiểm tra ngày nghỉ (TimeOffs) đang hoạt động
        var hasTimeOff = await _context.DoctorTimeOffs
            .AnyAsync(t => t.DoctorId == doctorId 
                        && t.Status == TimeOffStatus.Active
                        && t.StartAt < utcEnd 
                        && utcStart < t.EndAt, cancellationToken);

        if (hasTimeOff)
            return true;

        // 2. Kiểm tra khung giờ bận (ScheduleBlocks) đang hoạt động
        var hasBlock = await _context.DoctorScheduleBlocks
            .AnyAsync(b => b.DoctorId == doctorId
                        && b.IsActive
                        && b.StartAt < utcEnd
                        && utcStart < b.EndAt, cancellationToken);

        return hasBlock;
    }

    public async Task<List<AppointmentConflictDto>> GetAffectedAppointmentsAsync(
        Guid doctorId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default)
    {
        var utcStart = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);

        // Các trạng thái cuộc hẹn đang hoạt động (chưa hủy/hoàn thành)
        var activeStatuses = new[]
        {
            AppointmentStatus.PendingConfirmation,
            AppointmentStatus.Confirmed,
            AppointmentStatus.CheckedIn,
            AppointmentStatus.InProgress
        };

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId
                     && activeStatuses.Contains(a.Status)
                     && a.StartAt < utcEnd
                     && utcStart < a.EndAt)
            .OrderBy(a => a.StartAt)
            .ToListAsync(cancellationToken);

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var patientNames = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);

        return appointments.Select(a => new AppointmentConflictDto(
            AppointmentId: a.Id,
            BookingCode: a.BookingCode,
            PatientName: patientNames.TryGetValue(a.PatientId, out var name) ? name : a.BookingCode,
            StartAt: a.StartAt,
            EndAt: a.EndAt,
            Status: a.Status.ToString()
        )).ToList();
    }
}
