using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.TimeOffs.DTOs;

public record AppointmentConflictDto(
    Guid AppointmentId,
    string BookingCode,
    string PatientName,
    DateTime StartAt,
    DateTime EndAt,
    string Status
);

public record DoctorTimeOffDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    string DoctorCode,
    DateTime StartAt,
    DateTime EndAt,
    string? Reason,
    TimeOffStatus Status,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    List<AppointmentConflictDto>? AffectedAppointments = null
);

public record CreateDoctorTimeOffRequest(
    Guid DoctorId,
    DateTime StartAt,
    DateTime EndAt,
    string? Reason = null,
    bool AllowWithExistingAppointments = false
);

public record DoctorScheduleBlockDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    string DoctorCode,
    DateTime StartAt,
    DateTime EndAt,
    ScheduleBlockType BlockType,
    string? Reason,
    bool IsActive,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    List<AppointmentConflictDto>? AffectedAppointments = null
);

public record CreateDoctorScheduleBlockRequest(
    Guid DoctorId,
    DateTime StartAt,
    DateTime EndAt,
    ScheduleBlockType BlockType,
    string? Reason = null,
    bool AllowWithExistingAppointments = false
);
