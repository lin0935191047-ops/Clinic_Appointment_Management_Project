using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Audit.DTOs;

public record CancellationAuditDetailDto(
    Guid LogId,
    string Action,
    string ActorType,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string EntityType,
    Guid? EntityId,
    DateTime OccurredAtUtc,
    string? BookingCode,
    string? ServiceName,
    string? CancellationReason,
    decimal? PaidAmount,
    decimal? CancellationFeeRate,
    decimal? RefundRate,
    decimal? CancellationFeeAmount,
    decimal? ExpectedRefundAmount,
    string? PolicyVersion,
    string? RefundDecision,
    string? RefundStatus,
    string? ProviderReference,
    string? FailureReason,
    string? Note,
    string MetadataRedactedJson
);

public record AppointmentCancellationAuditHistoryDto(
    string BookingCode,
    Guid AppointmentId,
    AppointmentStatus Status,
    DateTime? CancellationAcceptedAtUtc,
    string? CancellationReason,
    string ServiceName,
    decimal PaidAmount,
    List<CancellationAuditDetailDto> AuditTrail
);
