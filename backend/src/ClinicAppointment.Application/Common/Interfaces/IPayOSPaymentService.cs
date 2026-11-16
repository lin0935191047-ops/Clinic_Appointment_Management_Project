namespace ClinicAppointment.Application.Common.Interfaces;

public record CreatePaymentLinkResult(
    bool Success,
    string? CheckoutUrl,
    string? QrCode,
    long? OrderCode,
    string? PaymentLinkId,
    string? Message,
    string? BankName = null,
    string? AccountNumber = null,
    string? AccountName = null
);

public record PaymentStatusResult(
    Guid AppointmentId,
    string BookingCode,
    decimal ServicePrice,
    decimal PaidAmount,
    string Status,
    bool IsPaid
);

public interface IPayOSPaymentService
{
    /// <summary>
    /// Tạo liên kết thanh toán và mã VietQR qua PayOS
    /// </summary>
    Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        Guid appointmentId, 
        string? clientIp = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tiếp nhận và xác thực Webhook tự động từ PayOS khi thanh toán hoàn tất
    /// </summary>
    Task<bool> ProcessWebhookAsync(
        string webhookBody, 
        string? signature, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra trạng thái thanh toán hiện tại của cuộc hẹn
    /// </summary>
    Task<PaymentStatusResult?> GetPaymentStatusAsync(
        Guid appointmentId, 
        CancellationToken cancellationToken = default);
}
