using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.DTOs;

public record AppointmentDetailDto(
    Guid Id,
    string BookingCode,
    Guid DoctorId,
    string DoctorName,
    Guid PatientId,
    string PatientName,
    Guid ServiceId,
    string ServiceName,
    int ServiceDuration,
    decimal ServicePrice,
    decimal PaidAmount,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status,
    string? ReasonForVisit,
    bool IsGuest
);
