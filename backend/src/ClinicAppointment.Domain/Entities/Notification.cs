using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

/// <summary>
/// Thực thể lưu trữ thông báo (SMS, Email, InApp) theo mẫu Outbox Pattern.
/// Tuân thủ quy định bảo mật: Không lưu thô PII, không chứa chẩn đoán y tế.
/// </summary>
public class Notification : BaseEntity
{
    public Guid? PatientId { get; set; }
    public Guid? AppointmentId { get; set; }

    /// <summary>
    /// Kênh gửi: Sms, Email, InApp
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Mã mẫu thông báo (ví dụ: BOOKING_CONFIRMATION, APPOINTMENT_REMINDER, RESCHEDULE_APPROVED, CANCELLATION_NOTICE)
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ nhận (số điện thoại / email) đã mã hóa AES-256-GCM
    /// </summary>
    public byte[]? RecipientCiphertext { get; set; }

    /// <summary>
    /// Địa chỉ nhận dạng che mặt nạ (ví dụ: 090***5678, u***@clinic.vn)
    /// </summary>
    public string? RecipientMasked { get; set; }

    /// <summary>
    /// Dữ liệu nội dung thông báo an toàn dưới dạng JSON (Metadata tối thiểu không chứa chẩn đoán y tế)
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Trạng thái gửi: Pending, Sent, Failed
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Mã định danh tin nhắn từ nhà cung cấp dịch vụ (Provider Message Id)
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// Thời điểm gửi thành công (UTC)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Lý do thất bại (nếu có lỗi xảy ra)
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Số lần đã thử gửi lại
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Số lần thử lại tối đa
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    // Navigation properties
    public virtual Patient? Patient { get; set; }
    public virtual Appointment? Appointment { get; set; }

    public bool CanRetry => Status == NotificationStatus.Failed && RetryCount < MaxRetries;

    public static Notification Create(
        NotificationChannel channel,
        string templateCode,
        string recipientMasked,
        byte[]? recipientCiphertext = null,
        string? payloadJson = null,
        Guid? appointmentId = null,
        Guid? patientId = null,
        int maxRetries = 3,
        DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(templateCode))
            throw new ArgumentException("TemplateCode không được để trống.", nameof(templateCode));

        var now = createdAtUtc ?? DateTime.UtcNow;

        return new Notification
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            TemplateCode = templateCode.Trim(),
            RecipientMasked = recipientMasked,
            RecipientCiphertext = recipientCiphertext,
            PayloadJson = payloadJson,
            AppointmentId = appointmentId,
            PatientId = patientId,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            MaxRetries = maxRetries > 0 ? maxRetries : 3,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSent(string providerMessageId, DateTime? sentAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(providerMessageId))
            throw new ArgumentException("ProviderMessageId không được để trống khi đánh dấu thành công.", nameof(providerMessageId));

        var now = sentAtUtc ?? DateTime.UtcNow;

        Status = NotificationStatus.Sent;
        ProviderMessageId = providerMessageId.Trim();
        SentAt = now;
        FailureReason = null;
        UpdatedAt = now;
    }

    public void MarkFailed(string failureReason, DateTime? failureAtUtc = null)
    {
        var now = failureAtUtc ?? DateTime.UtcNow;

        Status = NotificationStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Lỗi không xác định khi gửi thông báo." : failureReason.Trim();
        RetryCount++;
        UpdatedAt = now;
    }

    public void ResetForRetry(DateTime? retryAtUtc = null)
    {
        if (!CanRetry)
            throw new InvalidOperationException($"Không thể thử lại thông báo {Id}. Trạng thái hiện tại: {Status}, số lần thử: {RetryCount}/{MaxRetries}.");

        var now = retryAtUtc ?? DateTime.UtcNow;

        Status = NotificationStatus.Pending;
        UpdatedAt = now;
    }
}
