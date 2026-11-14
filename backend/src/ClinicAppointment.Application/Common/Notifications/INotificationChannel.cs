using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Common.Notifications;

/// <summary>
/// Trừu tượng hóa kênh gửi thông báo (SMS, Email, InApp).
/// Tách rời hoàn toàn khỏi nhà cung cấp cụ thể (Provider is TBD).
/// </summary>
public interface INotificationChannel
{
    NotificationChannel Channel { get; }
    Task<NotificationSendResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ngoại lệ ném ra khi nội dung thông báo vi phạm quy chuẩn an toàn bảo mật y tế
/// </summary>
public class NotificationSafetyViolationException : InvalidOperationException
{
    public NotificationSafetyViolationException(string message) : base(message) { }
}

/// <summary>
/// Bộ kiểm soát an toàn nội dung thông báo:
/// Tuyệt đối không để lọt chẩn đoán bệnh án, triệu chứng lâm sàng, số CCCD thô, hay mật khẩu vào tin nhắn.
/// </summary>
public interface INotificationSafetyGuard
{
    void ValidateMessageSafety(NotificationMessage message);
}
