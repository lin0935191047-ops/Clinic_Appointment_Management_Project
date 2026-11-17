using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services;

/// <summary>
/// Bộ điều phối gửi SMS OTP thông minh (Smart SMS Dispatcher):
/// Tự động phát hiện và chuyển tiếp tới cổng SMS thật (Twilio hoặc SpeedSMS)
/// dựa trên thông tin cấu hình có sẵn trong appsettings hoặc Environment Variables.
/// </summary>
public class SmartSmsSender : ISmsSender
{
    private readonly TwilioSmsSender _twilioSender;
    private readonly SpeedSmsSender _speedSmsSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmartSmsSender> _logger;

    public SmartSmsSender(
        TwilioSmsSender twilioSender,
        SpeedSmsSender speedSmsSender,
        IConfiguration configuration,
        ILogger<SmartSmsSender> logger)
    {
        _twilioSender = twilioSender;
        _speedSmsSender = speedSmsSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendOtpAsync(
        string phoneNumber,
        string otp,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra Twilio nếu đã cấu hình tài khoản thật
        var twilioSid = _configuration["Twilio:AccountSid"];
        var twilioToken = _configuration["Twilio:AuthToken"];
        var twilioFrom = _configuration["Twilio:FromPhoneNumber"];

        bool hasTwilio = !string.IsNullOrWhiteSpace(twilioSid) &&
                         !string.IsNullOrWhiteSpace(twilioToken) &&
                         !string.IsNullOrWhiteSpace(twilioFrom) &&
                         !twilioSid.Contains("YOUR_") &&
                         !twilioToken.Contains("YOUR_");

        if (hasTwilio)
        {
            _logger.LogInformation("[SmartSmsSender] Phát hiện cấu hình Twilio. Đang chuyển tiếp gửi SMS OTP qua Twilio Gateway...");
            return await _twilioSender.SendOtpAsync(phoneNumber, otp, purpose, cancellationToken);
        }

        // 2. Kiểm tra SpeedSMS Gateway nếu đã cấu hình token thật
        var speedToken = _configuration["SpeedSMS:AccessToken"];
        bool hasSpeedSms = !string.IsNullOrWhiteSpace(speedToken) && !speedToken.Contains("YOUR_");

        if (hasSpeedSms)
        {
            _logger.LogInformation("[SmartSmsSender] Phát hiện cấu hình SpeedSMS. Đang chuyển tiếp gửi SMS OTP qua SpeedSMS Gateway...");
            return await _speedSmsSender.SendOtpAsync(phoneNumber, otp, purpose, cancellationToken);
        }

        // 3. Nếu chưa cấu hình credentials thật cho cả 2 cổng, ghi log cảnh báo thân thiện
        _logger.LogWarning(
            "[SmartSmsSender] Chưa phát hiện API Key SMS thật từ Twilio hoặc SpeedSMS. " +
            "Vui lòng thiết lập biến môi trường Twilio (Twilio__AccountSid, Twilio__AuthToken, Twilio__FromPhoneNumber) " +
            "hoặc SpeedSMS (SpeedSMS__AccessToken) trên Render Dashboard để kích hoạt gửi SMS vật lý tới điện thoại.");

        // Fallback an toàn qua SpeedSmsSender để hệ thống hoạt động ổn định và ghi nhận cảnh báo
        return await _speedSmsSender.SendOtpAsync(phoneNumber, otp, purpose, cancellationToken);
    }
}
