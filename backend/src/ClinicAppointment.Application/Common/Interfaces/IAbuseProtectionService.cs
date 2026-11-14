namespace ClinicAppointment.Application.Common.Interfaces;

public interface IAbuseProtectionService
{
    /// <summary>
    /// Kiểm tra xem địa chỉ IP hoặc Số điện thoại có vượt quá giới hạn đặt lịch hay không.
    /// </summary>
    bool IsBookingRateLimitExceeded(string clientIp, string phoneNumber);

    /// <summary>
    /// Ghi nhận một lần yêu cầu đặt lịch từ IP và Số điện thoại.
    /// </summary>
    void RecordBookingAttempt(string clientIp, string phoneNumber);

    /// <summary>
    /// Đặt lại giới hạn (phục vụ kiểm thử hoặc giải tỏa quản trị).
    /// </summary>
    void ResetRateLimit(string clientIp, string phoneNumber);
}
