using ClinicAppointment.Domain.Services;

namespace ClinicAppointment.Application.Features.Appointments.Services;

public class CancellationPolicyService : ICancellationPolicyService
{
    public CancellationPolicyEvaluation Evaluate(
        DateTime appointmentStartAtUtc, 
        decimal paidAmount, 
        DateTime? serverClockUtc = null)
    {
        var nowUtc = serverClockUtc ?? DateTime.UtcNow;
        var minutesBefore = (appointmentStartAtUtc - nowUtc).TotalMinutes;

        // Nếu tại thời điểm bắt đầu ca khám hoặc trong quá khứ -> Không cho phép tự hủy
        if (minutesBefore <= 0)
        {
            return new CancellationPolicyEvaluation(
                CanCancel: false,
                DenialReason: "Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ.",
                MinutesBeforeAppointment: minutesBefore,
                CancellationFeeRate: 1.0m,
                RefundRate: 0.0m,
                CancellationFeeAmount: paidAmount > 0 ? paidAmount : 0m,
                ExpectedRefundAmount: 0m,
                ServerAcceptedAtUtc: nowUtc
            );
        }

        // Tính toán hoàn phí theo mốc 60 phút
        var calc = RefundCalculator.Calculate(appointmentStartAtUtc, nowUtc, paidAmount);

        return new CancellationPolicyEvaluation(
            CanCancel: true,
            DenialReason: null,
            MinutesBeforeAppointment: calc.MinutesBeforeAppointment,
            CancellationFeeRate: calc.CancellationFeeRate,
            RefundRate: calc.RefundRate,
            CancellationFeeAmount: calc.CancellationFeeAmount,
            ExpectedRefundAmount: calc.ExpectedRefundAmount,
            ServerAcceptedAtUtc: nowUtc
        );
    }
}
