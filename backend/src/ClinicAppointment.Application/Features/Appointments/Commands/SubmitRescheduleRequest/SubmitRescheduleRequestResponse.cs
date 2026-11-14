using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest;

public record SubmitRescheduleRequestResponse(
    Guid RequestId,
    Guid AppointmentId,
    string BookingCode,
    RescheduleStatus Status,
    DateTime CurrentStartAtUtc,
    DateTime CurrentEndAtUtc,
    DateTime ProposedStartAtUtc,
    DateTime ProposedEndAtUtc,
    Guid RequestedDoctorId,
    string? Reason,
    DateTime CreatedAtUtc,
    string Message
);
