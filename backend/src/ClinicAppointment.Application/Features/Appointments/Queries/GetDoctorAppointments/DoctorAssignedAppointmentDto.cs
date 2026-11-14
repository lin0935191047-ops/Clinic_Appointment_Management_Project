using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Queries.GetDoctorAppointments;

/// <summary>
/// DTO thông tin ca khám dành cho Bác sĩ điều trị (Doctor Assigned Appointment)
/// Tối thiểu hóa thông tin bệnh nhân, bảo vệ bí mật y tế và chống lộ lọt PII.
/// </summary>
public record DoctorAssignedAppointmentDto(
    Guid AppointmentId,
    string BookingCode,
    int QueueNumber,
    Guid DoctorId,
    string DoctorName,
    Guid PatientId,
    string PatientName,
    string PatientCode,
    DateOnly? DateOfBirth,
    int? BirthYear,
    string? Gender,
    string MaskedPhoneNumber,
    Guid ServiceId,
    string ServiceName,
    int ServiceDurationMinutes,
    decimal ServicePrice,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status,
    string? ReasonForVisit,
    DateTime? CheckedInAt,
    bool IsGuest
);
