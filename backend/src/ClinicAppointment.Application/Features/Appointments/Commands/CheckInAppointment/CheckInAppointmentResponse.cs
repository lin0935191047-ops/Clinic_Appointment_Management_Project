using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment;

/// <summary>
/// DTO phản hồi sau khi check-in ca khám thành công
/// </summary>
public record CheckInAppointmentResponse(
    Guid AppointmentId,
    string BookingCode,
    AppointmentStatus Status,
    DateTime StartAt,
    DateTime EndAt,
    DateTime CheckedInAt,
    string? Note
);
