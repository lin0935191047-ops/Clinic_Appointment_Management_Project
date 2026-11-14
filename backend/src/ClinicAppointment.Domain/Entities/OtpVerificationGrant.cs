using System.Security.Cryptography;
using System.Text;
using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

public class OtpVerificationGrant : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Guid AppointmentId { get; set; }
    public OtpPurpose Purpose { get; set; }
    public byte[] GrantTokenHash { get; set; } = Array.Empty<byte>();
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    // Navigation properties
    public virtual OtpChallenge? Challenge { get; set; }
    public virtual Appointment? Appointment { get; set; }

    public static byte[] ComputeTokenHash(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes((rawToken ?? string.Empty).Trim());
        return SHA256.HashData(bytes);
    }

    /// <summary>
    /// Phát hành Scoped Access Grant tạm thời sau khi xác thực OTP thành công.
    /// Token là opaque cryptographic token (CSPRNG), TUYỆT ĐỐI không phải là User JWT session.
    /// </summary>
    public static (OtpVerificationGrant Grant, string RawToken) Issue(
        Guid challengeId,
        Guid appointmentId,
        OtpPurpose purpose,
        TimeSpan? ttl = null)
    {
        if (challengeId == Guid.Empty)
            throw new ArgumentException("ChallengeId không hợp lệ.", nameof(challengeId));

        if (appointmentId == Guid.Empty)
            throw new ArgumentException("AppointmentId không hợp lệ.", nameof(appointmentId));

        // Sinh token ngẫu nhiên an toàn mật mã CSPRNG 256-bit
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = $"grnt_{Convert.ToHexString(tokenBytes).ToLowerInvariant()}";

        var grant = new OtpVerificationGrant
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            AppointmentId = appointmentId,
            Purpose = purpose,
            GrantTokenHash = ComputeTokenHash(rawToken),
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(15)),
            ConsumedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        return (grant, rawToken);
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của Grant đối với cuộc hẹn và mục đích cụ thể.
    /// </summary>
    public bool IsValidFor(Guid appointmentId, OtpPurpose purpose, DateTime currentUtc, out string? errorMessage)
    {
        errorMessage = null;

        if (AppointmentId != appointmentId)
        {
            errorMessage = "Quyền truy cập (Grant) không thuộc về cuộc hẹn này.";
            return false;
        }

        if (Purpose != purpose)
        {
            errorMessage = $"Quyền truy cập (Grant) được cấp cho mục đích {Purpose}, không thể dùng cho mục đích {purpose}.";
            return false;
        }

        if (currentUtc > ExpiresAt)
        {
            errorMessage = "Quyền truy cập tạm thời (Grant) đã hết hạn. Vui lòng xác thực lại OTP.";
            return false;
        }

        if (ConsumedAt.HasValue)
        {
            errorMessage = "Quyền truy cập (Grant) này đã được sử dụng trước đó (One-time only).";
            return false;
        }

        return true;
    }

    public void Consume(DateTime currentUtc)
    {
        if (ConsumedAt.HasValue)
            throw new InvalidOperationException("Quyền truy cập này đã được sử dụng trước đó.");

        ConsumedAt = currentUtc;
        UpdatedAt = currentUtc;
    }

    public void Revoke(DateTime currentUtc)
    {
        ConsumedAt = currentUtc;
        UpdatedAt = currentUtc;
    }
}
