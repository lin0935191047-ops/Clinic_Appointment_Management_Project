namespace ClinicAppointment.Application.Features.Schedules.DTOs;

public record DoctorScheduleDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    string DoctorCode,
    short DayOfWeek,
    string DayOfWeekName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotDurationMinutes,
    bool IsActive
);

public record CreateDoctorScheduleRequest(
    Guid DoctorId,
    short DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    int SlotDurationMinutes = 30,
    bool IsActive = true
);

public record UpdateDoctorScheduleRequest(
    short DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    int SlotDurationMinutes = 30,
    bool? IsActive = null
);

public record UpdateDoctorScheduleStatusRequest(
    bool IsActive
);
