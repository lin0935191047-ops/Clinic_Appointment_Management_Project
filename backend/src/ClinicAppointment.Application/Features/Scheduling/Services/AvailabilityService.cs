using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Scheduling.DTOs;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Scheduling.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IApplicationDbContext _context;

    public AvailabilityService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorAvailabilityDto> GetDoctorAvailabilityAsync(
        Guid doctorId,
        DateOnly date,
        Guid? serviceId = null,
        bool onlyAvailable = false,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra Bác sĩ tồn tại và đang hoạt động
        var doctor = await _context.Doctors
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);

        if (doctor == null)
            throw new KeyNotFoundException($"Không tìm thấy bác sĩ với định danh '{doctorId}'.");

        if (!doctor.IsActive)
            throw new InvalidOperationException($"Bác sĩ '{doctor.FullName}' hiện đang tạm ngừng hoạt động.");

        if (doctor.Specialty != null && !doctor.Specialty.IsActive)
            throw new InvalidOperationException($"Chuyên khoa '{doctor.Specialty.Name}' của bác sĩ hiện đang tạm ngừng hoạt động.");

        // 2. Xác định thời lượng khám theo dịch vụ
        int durationMinutes = 30;
        string? serviceName = null;

        if (serviceId.HasValue)
        {
            var service = await _context.Services
                .Include(s => s.Specialty)
                .FirstOrDefaultAsync(s => s.Id == serviceId.Value, cancellationToken);

            if (service == null)
                throw new KeyNotFoundException($"Không tìm thấy dịch vụ khám với định danh '{serviceId.Value}'.");

            if (!service.IsActive)
                throw new InvalidOperationException($"Dịch vụ khám '{service.Name}' hiện đang tạm ngừng hoạt động.");

            durationMinutes = service.DurationMinutes;
            serviceName = service.Name;
        }

        // 3. Lấy ca làm việc của bác sĩ tại ngày khám (FR-052)
        var isoDayOfWeek = TimeZoneHelper.GetIsoDayOfWeek(date);

        var schedules = await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId
                     && s.DayOfWeek == isoDayOfWeek
                     && s.IsActive
                     && s.EffectiveFrom <= date
                     && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= date))
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return new DoctorAvailabilityDto(
                DoctorId: doctor.Id,
                DoctorName: doctor.FullName,
                DoctorCode: doctor.DoctorCode,
                Date: date,
                ServiceId: serviceId,
                ServiceName: serviceName,
                DurationMinutes: durationMinutes,
                Slots: new List<TimeSlotDto>()
            );
        }

        // 4. Lấy khoảng thời gian UTC của toàn bộ ngày khám
        var (startDayUtc, endDayUtc) = TimeZoneHelper.GetDayUtcRange(date);

        // 5. Lấy các khoảng bận trong ngày (TimeOffs, ScheduleBlocks, Appointments)
        var timeOffs = await _context.DoctorTimeOffs
            .Where(t => t.DoctorId == doctorId
                     && t.Status == TimeOffStatus.Active
                     && t.StartAt < endDayUtc
                     && startDayUtc < t.EndAt)
            .ToListAsync(cancellationToken);

        var scheduleBlocks = await _context.DoctorScheduleBlocks
            .Where(b => b.DoctorId == doctorId
                     && b.IsActive
                     && b.StartAt < endDayUtc
                     && startDayUtc < b.EndAt)
            .ToListAsync(cancellationToken);

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
                     && a.StartAt < endDayUtc
                     && startDayUtc < a.EndAt)
            .ToListAsync(cancellationToken);

        // 6. Sinh các Slot theo ca làm việc và kiểm tra xung đột
        var slots = new List<TimeSlotDto>();
        var nowUtc = DateTime.UtcNow;

        foreach (var schedule in schedules)
        {
            var currentStart = schedule.StartTime;

            while (true)
            {
                var currentEnd = currentStart.AddMinutes(durationMinutes);

                // Nếu slot kết thúc sau giờ kết thúc ca làm việc, hoặc bị tràn qua nửa đêm -> Dừng ca này
                if (currentEnd <= currentStart || currentEnd > schedule.EndTime)
                    break;

                var slotStartUtc = TimeZoneHelper.ToUtc(date, currentStart);
                var slotEndUtc = TimeZoneHelper.ToUtc(date, currentEnd);

                bool isAvailable = true;
                string? reason = null;

                // Kiểm tra slot trong quá khứ
                if (slotStartUtc <= nowUtc)
                {
                    isAvailable = false;
                    reason = "Khung giờ đã qua";
                }
                // Kiểm tra ngày nghỉ (TimeOff)
                else if (timeOffs.Any(t => t.StartAt < slotEndUtc && slotStartUtc < t.EndAt))
                {
                    isAvailable = false;
                    reason = "Bác sĩ có lịch nghỉ";
                }
                // Kiểm tra khung giờ bận (ScheduleBlock)
                else if (scheduleBlocks.Any(b => b.StartAt < slotEndUtc && slotStartUtc < b.EndAt))
                {
                    isAvailable = false;
                    reason = "Khung giờ bận";
                }
                // Kiểm tra cuộc hẹn đã được đặt trước (Appointment active)
                else if (appointments.Any(a => a.StartAt < slotEndUtc && slotStartUtc < a.EndAt))
                {
                    isAvailable = false;
                    reason = "Đã có ca khám";
                }

                if (!onlyAvailable || isAvailable)
                {
                    // Bảo vệ quyền riêng tư: Tuyệt đối không trả về bất kỳ PII nào của người đã đặt
                    slots.Add(new TimeSlotDto(
                        StartTime: currentStart,
                        EndTime: currentEnd,
                        StartAtUtc: slotStartUtc,
                        EndAtUtc: slotEndUtc,
                        DurationMinutes: durationMinutes,
                        IsAvailable: isAvailable,
                        ReasonUnavailable: reason
                    ));
                }

                currentStart = currentEnd;
            }
        }

        return new DoctorAvailabilityDto(
            DoctorId: doctor.Id,
            DoctorName: doctor.FullName,
            DoctorCode: doctor.DoctorCode,
            Date: date,
            ServiceId: serviceId,
            ServiceName: serviceName,
            DurationMinutes: durationMinutes,
            Slots: slots
        );
    }
}
