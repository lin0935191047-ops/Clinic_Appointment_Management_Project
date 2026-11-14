using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment;

public record ConfirmAppointmentResponse(
    Guid AppointmentId,
    string BookingCode,
    AppointmentStatus Status,
    DateTime StartAt,
    DateTime EndAt,
    DateTime ConfirmedAt,
    string? Note
);
