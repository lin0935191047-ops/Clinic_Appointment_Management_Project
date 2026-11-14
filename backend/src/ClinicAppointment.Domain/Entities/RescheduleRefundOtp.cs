using System.Security.Cryptography;
using System.Text;
using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

public class RescheduleRequest : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid RequestedDoctorId { get; set; }
    public DateTime ProposedStartAt { get; set; }
    public DateTime ProposedEndAt { get; set; }
    public string? Reason { get; set; }
    public RescheduleStatus Status { get; set; } = RescheduleStatus.PendingReview;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public static RescheduleRequest Create(
        Guid appointmentId,
        Guid requestedDoctorId,
        DateTime proposedStartAtUtc,
        DateTime proposedEndAtUtc,
        string? reason = null)
    {
        if (appointmentId == Guid.Empty)
            throw new ArgumentException("Mã cuộc hẹn không hợp lệ.", nameof(appointmentId));
        if (requestedDoctorId == Guid.Empty)
            throw new ArgumentException("Mã bác sĩ yêu cầu không hợp lệ.", nameof(requestedDoctorId));
        if (proposedStartAtUtc >= proposedEndAtUtc)
            throw new ArgumentException("Thời gian bắt đầu đề xuất phải nhỏ hơn thời gian kết thúc đề xuất.", nameof(proposedStartAtUtc));

        return new RescheduleRequest
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            RequestedDoctorId = requestedDoctorId,
            ProposedStartAt = proposedStartAtUtc,
            ProposedEndAt = proposedEndAtUtc,
            Reason = reason?.Trim(),
            Status = RescheduleStatus.PendingReview,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid receptionistUserId, string? reviewNotes = null)
    {
        if (Status != RescheduleStatus.PendingReview)
            throw new InvalidOperationException($"Không thể duyệt yêu cầu đổi lịch ở trạng thái {Status}.");

        Status = RescheduleStatus.Approved;
        ReviewedByUserId = receptionistUserId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNotes = reviewNotes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid receptionistUserId, string reviewNotes)
    {
        if (Status != RescheduleStatus.PendingReview)
            throw new InvalidOperationException($"Không thể từ chối yêu cầu đổi lịch ở trạng thái {Status}.");

        if (string.IsNullOrWhiteSpace(reviewNotes))
            throw new ArgumentException("Lý do từ chối yêu cầu đổi lịch là bắt buộc.", nameof(reviewNotes));

        Status = RescheduleStatus.Rejected;
        ReviewedByUserId = receptionistUserId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNotes = reviewNotes.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}

public class Refund : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public decimal PaidServiceAmount { get; set; }
    public decimal CancellationFeeRate { get; set; }
    public decimal CancellationFeeAmount { get; set; }
    public decimal ExpectedRefundAmount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? Reason { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public virtual Appointment? Appointment { get; set; }
}

public class OtpChallenge : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public short AttemptsCount { get; set; } = 0;
    public short MaxAttempts { get; set; } = 5;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    // Navigation properties
    public virtual Appointment? Appointment { get; set; }
    public virtual Patient? Patient { get; set; }

    /// <summary>
    /// Băm mã OTP bằng HMAC-SHA256 kết hợp ChallengeId làm Salt/Context duy nhất.
    /// Tuyệt đối không bao giờ lưu trữ hoặc ghi log mã OTP thô (No raw OTP at rest or in logs).
    /// </summary>
    public static string ComputeOtpHash(string otp, Guid challengeId)
    {
        var key = Encoding.UTF8.GetBytes(challengeId.ToString("N"));
        var data = Encoding.UTF8.GetBytes((otp ?? string.Empty).Trim());
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(data));
    }

    public static OtpChallenge Create(
        Guid appointmentId,
        Guid patientId,
        string phoneNumber,
        OtpPurpose purpose,
        string rawOtp,
        TimeSpan? ttl = null,
        short maxAttempts = 5)
    {
        if (appointmentId == Guid.Empty)
            throw new ArgumentException("Mã cuộc hẹn liên kết không hợp lệ.", nameof(appointmentId));

        if (patientId == Guid.Empty)
            throw new ArgumentException("Mã bệnh nhân liên kết không hợp lệ.", nameof(patientId));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Số điện thoại nhận OTP không được để trống.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(rawOtp))
            throw new ArgumentException("Mã OTP không được để trống.", nameof(rawOtp));

        var challengeId = Guid.NewGuid();

        return new OtpChallenge
        {
            Id = challengeId,
            AppointmentId = appointmentId,
            PatientId = patientId,
            PhoneNumber = phoneNumber.Trim(),
            Purpose = purpose,
            OtpHash = ComputeOtpHash(rawOtp, challengeId),
            AttemptsCount = 0,
            MaxAttempts = maxAttempts,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(5)),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool Verify(string inputOtp, DateTime currentUtc, out string? errorMessage)
    {
        errorMessage = null;

        if (IsUsed)
        {
            errorMessage = "Mã xác thực OTP này đã được sử dụng trước đó.";
            return false;
        }

        if (currentUtc > ExpiresAt)
        {
            errorMessage = "Mã xác thực OTP đã hết hạn. Vui lòng yêu cầu mã mới.";
            return false;
        }

        if (AttemptsCount >= MaxAttempts)
        {
            errorMessage = $"Mã OTP đã vượt quá giới hạn {MaxAttempts} lần thử và đã bị khóa. Vui lòng yêu cầu mã mới.";
            return false;
        }

        AttemptsCount++;
        UpdatedAt = currentUtc;

        var computedHash = ComputeOtpHash(inputOtp, Id);
        if (computedHash != OtpHash)
        {
            var remaining = MaxAttempts - AttemptsCount;
            errorMessage = remaining > 0
                ? $"Mã xác thực OTP không chính xác. Bạn còn {remaining} lần thử."
                : $"Mã xác thực OTP không chính xác. Bạn đã hết lượt thử. Vui lòng yêu cầu mã mới.";
            return false;
        }

        IsUsed = true;
        return true;
    }
}
