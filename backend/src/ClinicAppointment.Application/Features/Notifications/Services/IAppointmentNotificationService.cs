using ClinicAppointment.Application.Common.Notifications;

namespace ClinicAppointment.Application.Features.Notifications.Services;

public interface IAppointmentNotificationService
{
    /// <summary>
    /// Kích hoạt thông báo khi đặt lịch mới thành công (BOOKING_CREATED).
    /// Chống gửi trùng nếu đã có thông báo cùng sự kiện.
    /// </summary>
    Task<NotificationDto?> NotifyBookingCreatedAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt thông báo khi lễ tân xác nhận lịch hẹn (APPOINTMENT_CONFIRMED).
    /// Chống gửi trùng nếu đã có thông báo cùng sự kiện.
    /// </summary>
    Task<NotificationDto?> NotifyAppointmentConfirmedAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt thông báo khi lễ tân duyệt đổi lịch hẹn (RESCHEDULE_APPROVED).
    /// Chống gửi trùng nếu đã có thông báo cùng sự kiện.
    /// </summary>
    Task<NotificationDto?> NotifyRescheduleApprovedAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt thông báo khi lịch hẹn bị hủy (APPOINTMENT_CANCELLED).
    /// Chống gửi trùng nếu đã có thông báo cùng sự kiện.
    /// </summary>
    Task<NotificationDto?> NotifyAppointmentCancelledAsync(
        Guid appointmentId, 
        decimal refundAmount, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt thông báo nhắc lịch hẹn trước giờ khám (APPOINTMENT_REMINDER).
    /// Chống gửi trùng nếu đã có thông báo cùng sự kiện.
    /// </summary>
    Task<NotificationDto?> NotifyAppointmentReminderAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách thông báo và trạng thái phát (Delivery status) theo ca khám.
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetNotificationsByAppointmentIdAsync(
        Guid appointmentId, 
        CancellationToken cancellationToken = default);
}
