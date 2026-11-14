using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Refunds.DTOs;

public record RefundDetailDto(
    Guid Id,
    Guid AppointmentId,
    string BookingCode,
    string PatientFullName,
    string DoctorFullName,
    string ServiceName,
    DateTime AppointmentStartAtUtc,
    decimal PaidServiceAmount,
    decimal CancellationFeeRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount,
    string PolicyVersion,
    RefundStatus Status,
    string StatusDescription,
    bool IsCompleted,
    string? Reason,
    string? ProviderReference,
    Guid? ProcessedByUserId,
    string? FailureReason,
    DateTime? ProcessedAtUtc,
    DateTime CreatedAtUtc
);
