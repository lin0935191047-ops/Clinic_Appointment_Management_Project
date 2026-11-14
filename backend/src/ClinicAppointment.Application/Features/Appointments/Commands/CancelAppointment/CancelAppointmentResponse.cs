using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;

public record CancelAppointmentResponse(
    Guid AppointmentId,
    string BookingCode,
    AppointmentStatus Status,
    DateTime CancellationAcceptedAtUtc,
    string? CancellationReason,
    decimal PaidAmount,
    decimal CancellationFeeRate,
    decimal RefundRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount,
    RefundStatus RefundStatus,
    string Message,
    string PolicyVersion = "v1.0",
    bool IsRefundCompleted = false
);
