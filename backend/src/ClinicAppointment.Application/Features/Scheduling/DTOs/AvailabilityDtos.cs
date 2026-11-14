namespace ClinicAppointment.Application.Features.Scheduling.DTOs;

public record TimeSlotDto(
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int DurationMinutes,
    bool IsAvailable,
    string? ReasonUnavailable = null
);

public record DoctorAvailabilityDto(
    Guid DoctorId,
    string DoctorName,
    string DoctorCode,
    DateOnly Date,
    Guid? ServiceId,
    string? ServiceName,
    int DurationMinutes,
    List<TimeSlotDto> Slots
);

public record GetDoctorAvailabilityRequest(
    Guid DoctorId,
    DateOnly Date,
    Guid? ServiceId = null,
    bool OnlyAvailable = false
);
