namespace ClinicAppointment.Application.Features.Appointments.Services;

public record CancellationPolicyEvaluation(
    bool CanCancel,
    string? DenialReason,
    double MinutesBeforeAppointment,
    decimal CancellationFeeRate,
    decimal RefundRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount,
    DateTime ServerAcceptedAtUtc
);

public interface ICancellationPolicyService
{
    /// <summary>
    /// Đánh giá chính sách hủy và tính toán số tiền hoàn dựa trên mốc 60 phút và Server clock (BR-010, FR-046, FR-047).
    /// </summary>
    CancellationPolicyEvaluation Evaluate(
        DateTime appointmentStartAtUtc, 
        decimal paidAmount, 
        DateTime? serverClockUtc = null);
}
