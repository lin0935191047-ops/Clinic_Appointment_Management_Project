using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services;

/// <summary>
/// Dịch vụ gửi SMS OTP qua cổng SpeedSMS Gateway (speedsms.vn).
/// Tuân thủ nghiêm ngặt AGENTS.md: TUYỆT ĐỐI KHÔNG ghi log mã raw OTP hoặc Access Token.
/// </summary>
public class SpeedSmsSender : ISmsSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpeedSmsSender> _logger;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly HttpClient _httpClient;

    private const string ApiUrl = "https://api.speedsms.vn/index.php/sms/send";

    public SpeedSmsSender(
        IConfiguration configuration,
        ILogger<SpeedSmsSender> logger,
        IIdentifierMaskingService maskingService,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _maskingService = maskingService;
        _httpClient = httpClient;
    }

    public async Task<bool> SendOtpAsync(
        string phoneNumber,
        string otp,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var maskedPhone = _maskingService.MaskPhone(phoneNumber);

        var accessToken = _configuration["SpeedSMS:AccessToken"];
        var smsType = _configuration.GetValue<int>("SpeedSMS:SmsType", 2); // 2: CSKH / OTP
        var sender = _configuration["SpeedSMS:Sender"] ?? "";

        // Nếu chưa cấu hình Access Token thật, ghi log cảnh báo thân thiện và giả lập thành công để không gián đoạn
        if (string.IsNullOrWhiteSpace(accessToken) || accessToken.Contains("YOUR_"))
        {
            _logger.LogWarning(
                "[SpeedSMS Gateway] Chưa cấu hình Access Token (SpeedSMS:AccessToken). Để gửi SMS thật đến {MaskedPhone}, vui lòng nhập AccessToken trong appsettings.json. Hệ thống tiếp tục với chế độ thử nghiệm an toàn.",
                maskedPhone);
            return true;
        }

        try
        {
            var normalizedPhone = NormalizeToVietnamesePhone(phoneNumber);
            var messageContent = $"[Phong Kham] Ma xac thuc OTP cua ban la: {otp}. Ma co hieu luc trong 5 phut. Vui long khong chia se ma nay cho bat ky ai.";

            // HTTP Basic Authentication: Username là Access Token, Password là 'x'
            var authBytes = Encoding.ASCII.GetBytes($"{accessToken}:x");
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var payload = new Dictionary<string, object>
            {
                ["to"] = new[] { normalizedPhone },
                ["content"] = messageContent,
                ["type"] = smsType
            };

            if (!string.IsNullOrWhiteSpace(sender))
            {
                payload["sender"] = sender.Trim();
            }

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;

                if (status == "success")
                {
                    _logger.LogInformation(
                        "[SpeedSMS Gateway] Đã gửi tin nhắn SMS OTP thành công tới số {MaskedPhone} (Mục đích: {Purpose}).",
                        maskedPhone, purpose);
                    return true;
                }
                else
                {
                    var message = root.TryGetProperty("message", out var m) ? m.GetString() : "Lỗi không xác định từ SpeedSMS";
                    var code = root.TryGetProperty("code", out var c) ? c.ToString() : "";
                    _logger.LogError(
                        "[SpeedSMS Gateway] Gửi SMS thất bại tới {MaskedPhone}. Code: {Code}, Message: {Message}",
                        maskedPhone, code, message);
                    return false;
                }
            }
            else
            {
                _logger.LogError(
                    "[SpeedSMS Gateway] Lỗi HTTP kết nối tới SpeedSMS: StatusCode {StatusCode}, Response: {Response}",
                    response.StatusCode, responseString);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SpeedSMS Gateway] Ngoại lệ khi gửi SMS OTP tới số {MaskedPhone}.", maskedPhone);
            return false;
        }
    }

    /// <summary>
    /// Chuẩn hóa số điện thoại về định dạng số di động Việt Nam (Ví dụ: +84935191047 -> 0935191047)
    /// </summary>
    private static string NormalizeToVietnamesePhone(string phone)
    {
        var cleaned = new string(phone.Where(char.IsDigit).ToArray());
        if (cleaned.StartsWith("84") && cleaned.Length >= 10)
        {
            return "0" + cleaned[2..];
        }
        if (!cleaned.StartsWith("0") && cleaned.Length == 9)
        {
            return "0" + cleaned;
        }
        return cleaned;
    }
}
