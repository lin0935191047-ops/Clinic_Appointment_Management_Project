using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services;

/// <summary>
/// SMS Sender Adapter (Nhà cung cấp SMS thực tế là TBD).
/// TUYỆT ĐỐI không in mã OTP ra log để bảo đảm an toàn thông tin (No OTP in logs - AGENTS.md invariant).
/// </summary>
public class MockSmsSender : ISmsSender
{
    private readonly ILogger<MockSmsSender> _logger;
    private readonly IIdentifierMaskingService _maskingService;

    // Lưu lại các SMS đã gửi (chỉ dùng cho mục đích kiểm thử/xác nhận gửi thành công, không lưu OTP thô trong production)
    public List<(string MaskedPhone, OtpPurpose Purpose, DateTime SentAt)> SentHistory { get; } = new();

    public MockSmsSender(ILogger<MockSmsSender> logger, IIdentifierMaskingService maskingService)
    {
        _logger = logger;
        _maskingService = maskingService;
    }

    public Task<bool> SendOtpAsync(
        string phoneNumber, 
        string otp, 
        OtpPurpose purpose, 
        CancellationToken cancellationToken = default)
    {
        var maskedPhone = _maskingService.MaskPhone(phoneNumber);

        // Bảo mật: Không in otp ra log!
        _logger.LogInformation(
            "Đã điều phối gửi mã xác thực SMS thành công tới số {MaskedPhone} cho mục đích {Purpose}. (Provider: Mock/TBD)", 
            maskedPhone, purpose);

        lock (SentHistory)
        {
            SentHistory.Add((maskedPhone, purpose, DateTime.UtcNow));
        }

        return Task.FromResult(true);
    }
}
