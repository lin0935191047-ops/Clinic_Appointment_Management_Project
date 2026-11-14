using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Common.Interfaces;

public record IssueGrantResult(
    bool IsSuccess,
    string? ErrorMessage,
    string? GrantToken,
    Guid? GrantId,
    Guid? AppointmentId,
    OtpPurpose? Purpose,
    DateTime? ExpiresAt);

public record ValidateGrantResult(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? GrantId,
    Guid? AppointmentId,
    OtpPurpose? Purpose);

public interface IOtpGrantService
{
    /// <summary>
    /// Phát hành Grant token sau khi xác thực OTP thành công cho một ChallengeId.
    /// </summary>
    Task<IssueGrantResult> IssueGrantAsync(
        Guid challengeId, 
        TimeSpan? ttl = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra tính hợp lệ của Grant token đối với Cuộc hẹn và Mục đích cụ thể.
    /// Nếu consume = true, grant sẽ được đánh dấu đã sử dụng (One-time use).
    /// </summary>
    Task<ValidateGrantResult> ValidateAndConsumeGrantAsync(
        string rawGrantToken, 
        Guid appointmentId, 
        OtpPurpose expectedPurpose, 
        bool consume = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi Grant token.
    /// </summary>
    Task<bool> RevokeGrantAsync(
        string rawGrantToken, 
        CancellationToken cancellationToken = default);
}
