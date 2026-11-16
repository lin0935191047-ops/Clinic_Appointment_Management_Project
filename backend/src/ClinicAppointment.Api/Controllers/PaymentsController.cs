using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPayOSPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPayOSPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo mã VietQR / liên kết thanh toán trực tuyến qua PayOS cho cuộc hẹn
    /// </summary>
    [HttpPost("payos/create-link")]
    public async Task<IActionResult> CreatePaymentLink(
        [FromBody] CreatePaymentLinkApiRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId == Guid.Empty)
        {
            return BadRequest(ApiResponse<object>.Fail("Mã cuộc hẹn không hợp lệ.", StatusCodes.Status400BadRequest));
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _paymentService.CreatePaymentLinkAsync(request.AppointmentId, clientIp, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Tạo liên kết thanh toán thất bại.", StatusCodes.Status400BadRequest));
        }

        return Ok(ApiResponse<CreatePaymentLinkResult>.Ok(result, result.Message));
    }

    /// <summary>
    /// Kiểm tra trạng thái thanh toán của cuộc hẹn (Hỗ trợ polling từ Frontend)
    /// </summary>
    [HttpGet("status/{appointmentId:guid}")]
    public async Task<IActionResult> GetStatus(
        [FromRoute] Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var status = await _paymentService.GetPaymentStatusAsync(appointmentId, cancellationToken);
        if (status == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy cuộc hẹn.", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<PaymentStatusResult>.Ok(status));
    }

    /// <summary>
    /// Webhook tiếp nhận thông báo thanh toán thành công tự động từ PayOS
    /// </summary>
    [HttpPost("payos/webhook")]
    public async Task<IActionResult> PayOSWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["x-payos-signature"].FirstOrDefault();

        _logger.LogInformation("[PayOS Webhook] Nhận request: {Body}", body);

        // Hỗ trợ trường hợp PayOS gửi request xác thực Webhook URL khi đăng ký trong dashboard
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("webhookUrl", out _))
            {
                _logger.LogInformation("[PayOS Webhook] Xác nhận cấu hình Webhook URL thành công.");
                return Ok(new { code = "00", message = "Webhook verified successfully." });
            }
        }
        catch { }

        var success = await _paymentService.ProcessWebhookAsync(body, signature, cancellationToken);
        if (success)
        {
            return Ok(new { code = "00", message = "Thanh toán đã được xử lý thành công." });
        }

        return Ok(new { code = "01", message = "Ghi nhận webhook hoàn tất." });
    }

    /// <summary>
    /// Nút xác nhận thanh toán thử nghiệm nhanh (Dùng khi chưa cấu hình Webhook công khai)
    /// </summary>
    [HttpPost("mock-confirm/{appointmentId:guid}")]
    public async Task<IActionResult> MockConfirm(
        [FromRoute] Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var status = await _paymentService.GetPaymentStatusAsync(appointmentId, cancellationToken);
        if (status == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy cuộc hẹn.", StatusCodes.Status404NotFound));
        }

        var mockPayload = JsonSerializer.Serialize(new
        {
            code = "00",
            data = new
            {
                orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                amount = status.ServicePrice,
                description = $"TT {status.BookingCode}"
            }
        });

        await _paymentService.ProcessWebhookAsync(mockPayload, null, cancellationToken);

        var updatedStatus = await _paymentService.GetPaymentStatusAsync(appointmentId, cancellationToken);
        return Ok(ApiResponse<PaymentStatusResult>.Ok(updatedStatus!, "Xác nhận thanh toán thành công!"));
    }
}

public record CreatePaymentLinkApiRequest(Guid AppointmentId);
