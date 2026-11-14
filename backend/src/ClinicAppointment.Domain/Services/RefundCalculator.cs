namespace ClinicAppointment.Domain.Services;

public record RefundCalculationResult(
    double MinutesBeforeAppointment,
    decimal CancellationFeeRate,
    decimal RefundRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount);

public static class RefundCalculator
{
    /// <summary>
    /// Kiểm tra xem bệnh nhân có quyền tự hủy lịch hay không theo Server Timestamp (BR-009, BR-010).
    /// Bệnh nhân chỉ được tự hủy trước thời điểm bắt đầu ca khám (StartAt > serverNowUtc).
    /// </summary>
    public static bool CanPatientCancel(DateTime startAtUtc, DateTime serverNowUtc)
    {
        return startAtUtc > serverNowUtc;
    }

    /// <summary>
    /// Tính toán phí hủy và số tiền hoàn dự kiến dựa trên mốc 60 phút và Server Timestamp (BR-010, FR-046, FR-047, FR-049).
    /// - Từ 60 phút trở lên (bao gồm đúng mốc 60 phút): hoàn 100% phí dịch vụ đã thanh toán, phí hủy 0%.
    /// - Dưới 60 phút nhưng trước giờ hẹn: hoàn 50%, phí hủy 50%.
    /// - Tại hoặc sau thời điểm hẹn: từ chối, ném ngoại lệ InvalidOperationException.
    /// - Nếu chưa thanh toán (paidServiceAmount <= 0): hoàn 0, không tự thu phí hủy khi chưa có cổng thanh toán.
    /// </summary>
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

        // Quy tắc Baseline SRS 1.2 & BR-010:
        // minutesBefore >= 60.0: hoàn 100%, phí hủy 0%
        // minutesBefore < 60.0: hoàn 50%, phí hủy 50%
        decimal feeRate = minutesBefore >= 60.0 ? 0.0m : 0.50m;
        decimal refundRate = 1.0m - feeRate;

        decimal feeAmount = 0m;
        decimal refundAmount = 0m;

        if (paidServiceAmount > 0)
        {
            // Làm tròn số nguyên VND chuẩn
            feeAmount = Math.Round(paidServiceAmount * feeRate, 0, MidpointRounding.AwayFromZero);
            refundAmount = paidServiceAmount - feeAmount;
        }

        return new RefundCalculationResult(
            MinutesBeforeAppointment: minutesBefore,
            CancellationFeeRate: feeRate,
            RefundRate: refundRate,
            CancellationFeeAmount: feeAmount,
            ExpectedRefundAmount: refundAmount);
    }
}
