using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Common.Notifications;

public record NotificationDto(
    Guid Id,
    NotificationChannel Channel,
    string TemplateCode,
    string? RecipientMasked,
    NotificationStatus Status,
    string? ProviderMessageId,
    DateTime? SentAt,
    string? FailureReason,
    int RetryCount,
    int MaxRetries,
    Guid? AppointmentId,
    Guid? PatientId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record EnqueueNotificationCommand(
    string Recipient,
    NotificationChannel Channel,
    string TemplateCode,
    string Content,
    string? Subject = null,
    IDictionary<string, object>? Payload = null,
    Guid? AppointmentId = null,
    Guid? PatientId = null,
    int MaxRetries = 3
);

public interface INotificationService
{
    /// <summary>
    /// Xếp hàng thông báo vào Outbox sau khi kiểm tra an toàn bảo mật
    /// </summary>
    Task<Result<NotificationDto>> EnqueueNotificationAsync(
        EnqueueNotificationCommand request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực thi gửi ngay một thông báo cụ thể từ Outbox
    /// </summary>
    Task<NotificationSendResult> DispatchNotificationAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Quét và gửi các thông báo đang chờ (Pending) trong Outbox
    /// </summary>
    Task<int> DispatchPendingNotificationsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thử lại gửi thông báo đã thất bại
    /// </summary>
    Task<Result<NotificationDto>> RetryNotificationAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách thông báo theo ca khám
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetNotificationsByAppointmentIdAsync(
        Guid appointmentId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết thông báo
    /// </summary>
    Task<Result<NotificationDto>> GetNotificationByIdAsync(
        Guid notificationId, 
        CancellationToken cancellationToken = default);
}
