using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Common.Interfaces;

public record RequestOtpResult(
    bool IsSuccess,
    string Message,
    Guid ChallengeId,
    int CooldownSeconds = 60);

public record VerifyOtpResult(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? ChallengeId,
    Guid? AppointmentId,
    Guid? PatientId,
    OtpPurpose? Purpose);

public interface IOtpChallengeService
{
    /// <summary>
    /// Phát hành mã xác thực OTP cho cuộc hẹn theo mục đích cụ thể.
    /// Luôn trả về phản hồi chung (Generic Acknowledgment) để chống tấn công dò tìm (Anti-enumeration).
    /// </summary>
    Task<RequestOtpResult> RequestOtpAsync(
        string bookingCode,
        string phoneNumber,
        OtpPurpose purpose,
        string clientIp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác minh mã OTP đối với ChallengeId đã cấp phát.
    /// </summary>
    Task<VerifyOtpResult> VerifyOtpAsync(
        Guid challengeId,
        string otp,
        OtpPurpose expectedPurpose,
        CancellationToken cancellationToken = default);
}
