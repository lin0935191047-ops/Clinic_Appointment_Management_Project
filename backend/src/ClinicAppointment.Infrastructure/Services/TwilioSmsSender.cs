using System.Net.Http.Headers;
using System.Text;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services;

/// <summary>
/// Dịch vụ gửi SMS OTP thật qua Twilio SMS API.
/// Tuân thủ nghiêm ngặt AGENTS.md: TUYỆT ĐỐI KHÔNG ghi log mã raw OTP.
/// </summary>
public class TwilioSmsSender : ISmsSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSmsSender> _logger;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly HttpClient _httpClient;

    public TwilioSmsSender(
        IConfiguration configuration,
        ILogger<TwilioSmsSender> logger,
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

        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromPhoneNumber"];

        // Nếu chưa cấu hình credentials thật, ghi log cảnh báo thân thiện và giả lập thành công để không gián đoạn
        if (string.IsNullOrWhiteSpace(accountSid) ||
            string.IsNullOrWhiteSpace(authToken) ||
            string.IsNullOrWhiteSpace(fromNumber) ||
            accountSid.Contains("YOUR_") ||
            authToken.Contains("YOUR_"))
        {
            _logger.LogWarning(
                "[Twilio SMS] Chưa cấu hình thông tin Twilio (AccountSid/AuthToken). Để gửi SMS thật đến {MaskedPhone}, vui lòng cấu hình mục Twilio trong appsettings.json. Hệ thống tiếp tục với chế độ thử nghiệm an toàn.",
                maskedPhone);
            return true;
        }

        try
        {
            var formattedTo = FormatToE164(phoneNumber);
            var messageBody = $"[Phong Kham] Ma OTP cua ban la: {otp}. Ma co hieu luc trong 5 phut. Khong chia se ma nay cho bat ky ai.";

            var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", formattedTo),
                new KeyValuePair<string, string>("From", fromNumber),
                new KeyValuePair<string, string>("Body", messageBody)
            });

            request.Content = formContent;

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[Twilio SMS] Đã gửi mã xác thực SMS OTP thành công tới số điện thoại {MaskedPhone} (Mục đích: {Purpose}).",
                    maskedPhone, purpose);
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "[Twilio SMS] Gửi SMS thất bại tới {MaskedPhone}. StatusCode: {StatusCode}. Phản hồi: {ErrorBody}",
                    maskedPhone, response.StatusCode, errorBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Twilio SMS] Lỗi ngoại lệ khi gửi SMS OTP tới {MaskedPhone}.", maskedPhone);
            return false;
        }
    }

    /// <summary>
    /// Chuyển đổi định dạng số điện thoại Việt Nam sang chuẩn quốc tế E.164 (VD: 0935191047 -> +84935191047)
    /// </summary>
    private static string FormatToE164(string phone)
    {
        var cleaned = new string(phone.Where(char.IsDigit).ToArray());
        if (cleaned.StartsWith("0"))
        {
            return "+84" + cleaned[1..];
        }
        if (cleaned.StartsWith("84"))
        {
            return "+" + cleaned;
        }
        if (phone.StartsWith("+"))
        {
            return phone;
        }
        return "+84" + cleaned;
    }
}
