using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services;

public class PayOSPaymentService : IPayOSPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayOSPaymentService> _logger;
    private readonly HttpClient _httpClient;

    public PayOSPaymentService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PayOSPaymentService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        Guid appointmentId, 
        string? clientIp = null, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
        {
            return new CreatePaymentLinkResult(false, null, null, null, null, "Không tìm thấy thông tin cuộc hẹn.");
        }

        if (appointment.PaidAmount >= appointment.ServicePriceSnapshot && appointment.ServicePriceSnapshot > 0)
        {
            return new CreatePaymentLinkResult(false, null, null, null, null, "Cuộc hẹn này đã được thanh toán đầy đủ trước đó.");
        }

        var clientId = _configuration["PayOS:ClientId"];
        var apiKey = _configuration["PayOS:ApiKey"];
        var checksumKey = _configuration["PayOS:ChecksumKey"];
        var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:5173/lookup?code=" + appointment.BookingCode;
        var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/lookup?code=" + appointment.BookingCode;

        var amount = (long)appointment.ServicePriceSnapshot;
        if (amount <= 0)
        {
            amount = 150000; // Giá trị khám mặc định nếu snapshot bằng 0
        }

        // Sinh orderCode duy nhất dạng số nguyên dương (PayOS yêu cầu orderCode <= 9007199254740991)
        var orderCode = long.Parse(DateTime.UtcNow.ToString("yyMMddHHmm") + (new Random().Next(100, 999)));
        
        // Description tối đa 25 ký tự theo quy chuẩn PayOS
        var description = $"TT {appointment.BookingCode}";
        if (description.Length > 25)
        {
            description = description[..25];
        }

        var bankId = _configuration["BankQR:BankId"] ?? "TCB";
        var accountNumber = _configuration["BankQR:AccountNumber"] ?? "19038779759014";
        var accountName = _configuration["BankQR:AccountName"] ?? "LE HUYNH PHUONG ANH";
        var template = _configuration["BankQR:Template"] ?? "compact2";
        var staticQrUrl = _configuration["BankQR:StaticQrUrl"] ?? "/images/qr/techcombank-qr-clean.png";

        // Nếu chưa cấu hình PayOS thật, tạo VietQR ngân hàng trực tiếp theo cấu hình BankQR của phòng khám
        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(checksumKey) ||
            clientId.Contains("YOUR_"))
        {
            _logger.LogInformation(
                "[VietQR Ngân hàng] Tạo mã VietQR cho lịch {BookingCode} tới TK {BankId} - {AccountNumber} ({AccountName}) với số tiền {Amount} VND.",
                appointment.BookingCode, bankId, accountNumber, accountName, amount);

            var vietQrImage = !string.IsNullOrWhiteSpace(staticQrUrl)
                ? staticQrUrl
                : $"https://img.vietqr.io/image/{bankId}-{accountNumber}-{template}.png?amount={amount}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString(accountName)}";
            var mockCheckoutUrl = $"/checkout-demo?bookingCode={appointment.BookingCode}&amount={amount}&orderCode={orderCode}";

            return new CreatePaymentLinkResult(
                true,
                mockCheckoutUrl,
                vietQrImage,
                orderCode,
                $"VIETQR-DIRECT-{orderCode}",
                $"Mã VietQR Techcombank ({accountName} - {accountNumber}) đã sẵn sàng.",
                "Techcombank",
                accountNumber,
                accountName
            );
        }

        try
        {
            // Tính signature theo chuẩn PayOS: "amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}"
            var rawData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            var signature = ComputeHmacSha256(rawData, checksumKey);

            var payload = new
            {
                orderCode = orderCode,
                amount = amount,
                description = description,
                cancelUrl = cancelUrl,
                returnUrl = returnUrl,
                signature = signature
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
            request.Headers.Add("x-client-id", clientId);
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[PayOS] Gọi API tạo link thanh toán thất bại. Code: {Status}, Content: {Content}", response.StatusCode, content);
                return new CreatePaymentLinkResult(false, null, null, null, null, "Không thể kết nối tới cổng thanh toán PayOS: " + content);
            }

            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            var code = root.GetProperty("code").GetString();
            if (code != "00")
            {
                var desc = root.TryGetProperty("desc", out var d) ? d.GetString() : "Lỗi từ PayOS";
                return new CreatePaymentLinkResult(false, null, null, null, null, desc);
            }

            var data = root.GetProperty("data");
            var checkoutUrl = data.GetProperty("checkoutUrl").GetString();
            var qrCode = data.GetProperty("qrCode").GetString();
            var paymentLinkId = data.GetProperty("paymentLinkId").GetString();

            _logger.LogInformation("[PayOS] Khởi tạo link thanh toán VietQR thành công cho lịch hẹn {BookingCode}, OrderCode: {OrderCode}", appointment.BookingCode, orderCode);

            return new CreatePaymentLinkResult(
                true,
                checkoutUrl,
                qrCode,
                orderCode,
                paymentLinkId,
                "Khởi tạo liên kết thanh toán PayOS thành công."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOS] Ngoại lệ khi tạo liên kết thanh toán cho lịch {BookingCode}", appointment.BookingCode);
            return new CreatePaymentLinkResult(false, null, null, null, null, "Lỗi kết nối cổng thanh toán: " + ex.Message);
        }
    }

    public async Task<bool> ProcessWebhookAsync(
        string webhookBody, 
        string? signature, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(webhookBody);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                _logger.LogWarning("[PayOS Webhook] Body không chứa trường 'data'.");
                return false;
            }

            var code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            if (code != "00")
            {
                _logger.LogWarning("[PayOS Webhook] Giao dịch không thành công. Code: {Code}", code);
                return false;
            }

            var orderCode = dataElement.GetProperty("orderCode").GetInt64();
            var amount = dataElement.GetProperty("amount").GetDecimal();
            var description = dataElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";

            _logger.LogInformation("[PayOS Webhook] Nhận thông báo thanh toán thành công. OrderCode: {OrderCode}, Amount: {Amount}, Desc: {Desc}",
                orderCode, amount, description);

            // Tìm kiếm cuộc hẹn tương ứng qua BookingCode trong description
            // Description có dạng: "TT APT-20261114-XXXXXX"
            Appointment? appointment = null;
            if (description.Contains("APT-"))
            {
                var idx = description.IndexOf("APT-");
                var codeCandidate = description.Substring(idx).Split(' ')[0].Trim();
                appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.BookingCode == codeCandidate, cancellationToken);
            }

            if (appointment == null)
            {
                // Nếu không thấy qua description, tìm qua cuộc hẹn chờ thanh toán có giá tương ứng gần nhất
                appointment = await _context.Appointments
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync(a => a.PaidAmount == 0 && a.ServicePriceSnapshot == amount, cancellationToken);
            }

            if (appointment == null)
            {
                _logger.LogWarning("[PayOS Webhook] Không tìm thấy cuộc hẹn khớp với nội dung chuyển khoản: {Desc}", description);
                return false;
            }

            // Cập nhật trạng thái cuộc hẹn
            appointment.PaidAmount += amount;
            if (appointment.Status == AppointmentStatus.PendingConfirmation)
            {
                appointment.Status = AppointmentStatus.Confirmed;
            }
            appointment.UpdatedAt = DateTime.UtcNow;

            // Ghi nhận AuditLog
            var auditLog = AuditLog.Create(
                "SYSTEM",
                null,
                "PAYOS_WEBHOOK_PAID",
                "Appointment",
                appointment.Id,
                $"Thanh toan truc tuyen thanh cong qua PayOS VietQR. So tien: {amount:N0} VND. Ma GD: {orderCode}."
            );
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[PayOS Webhook] Đã cập nhật trạng thái đã thanh toán cho cuộc hẹn {BookingCode}. Tổng đã trả: {PaidAmount} VND.",
                appointment.BookingCode, appointment.PaidAmount);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayOS Webhook] Lỗi khi xử lý dữ liệu webhook.");
            return false;
        }
    }

    public async Task<PaymentStatusResult?> GetPaymentStatusAsync(
        Guid appointmentId, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null) return null;

        var isPaid = appointment.PaidAmount >= appointment.ServicePriceSnapshot && appointment.ServicePriceSnapshot > 0;

        return new PaymentStatusResult(
            appointment.Id,
            appointment.BookingCode,
            appointment.ServicePriceSnapshot,
            appointment.PaidAmount,
            appointment.Status.ToString(),
            isPaid
        );
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
