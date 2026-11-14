namespace ClinicAppointment.Domain.Services;

public record RefundCalculationResult(
    double MinutesBeforeAppointment,
    decimal CancellationFeeRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount);

public static class RefundCalculator
{
    public static RefundCalculationResult Calculate(
        DateTime startAtUtc, 
        DateTime cancellationAcceptedAtUtc, 
        decimal paidServiceAmount)
    {
        var minutesBefore = (startAtUtc - cancellationAcceptedAtUtc).TotalMinutes;

        if (minutesBefore <= 0)
        {
            throw new InvalidOperationException("Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ.");
        }

        // Quy tắc Baseline SRS 1.2: Từ 60 phút trở lên (bao gồm đúng mốc 60 phút) hoàn 100%, dưới 60 phút hoàn 50%
        decimal feeRate = minutesBefore >= 60.0 ? 0.0m : 0.50m;
        decimal feeAmount = paidServiceAmount * feeRate;
        decimal refundAmount = paidServiceAmount - feeAmount;

        return new RefundCalculationResult(
            minutesBefore,
            feeRate,
            feeAmount,
            refundAmount);
    }
}

