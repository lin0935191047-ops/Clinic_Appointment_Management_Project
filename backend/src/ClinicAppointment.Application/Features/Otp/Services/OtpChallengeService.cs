using System.Security.Cryptography;
using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Features.Otp.Services;

public class OtpChallengeService : IOtpChallengeService
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsSender _smsSender;
    private readonly IAbuseProtectionService _abuseProtectionService;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly ILogger<OtpChallengeService> _logger;

    public const string GenericAcknowledgmentMessage = 
        "Nếu thông tin mã đặt lịch và số điện thoại trùng khớp với hồ sơ, mã xác thực OTP đã được gửi đến số điện thoại của bạn.";

    public OtpChallengeService(
        IApplicationDbContext context,
        ISmsSender smsSender,
        IAbuseProtectionService abuseProtectionService,
        IIdentifierMaskingService maskingService,
        ILogger<OtpChallengeService> logger)
    {
        _context = context;
        _smsSender = smsSender;
        _abuseProtectionService = abuseProtectionService;
        _maskingService = maskingService;
        _logger = logger;
    }

    public async Task<RequestOtpResult> RequestOtpAsync(
        string bookingCode, 
        string phoneNumber, 
        OtpPurpose purpose, 
        string clientIp, 
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = (phoneNumber ?? string.Empty).Trim();
        var normalizedBooking = (bookingCode ?? string.Empty).Trim().ToUpperInvariant();

        // 1. Kiểm tra Abuse Protection / Rate limiting
        if (_abuseProtectionService.IsBookingRateLimitExceeded(clientIp, normalizedPhone))
        {
            throw new RateLimitExceededException("Quý khách đã yêu cầu mã OTP quá nhiều lần trong thời gian ngắn. Vui lòng chờ ít phút trước khi thử lại.");
        }

        // 2. Tra cứu cuộc hẹn và hồ sơ bệnh nhân
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.BookingCode == normalizedBooking, cancellationToken);

        // 3. Chống rò rỉ thông tin (Anti-enumeration): Nếu không tìm thấy hoặc số điện thoại không khớp
        if (appointment == null || appointment.Patient == null || appointment.Patient.PhoneNumber != normalizedPhone)
        {
            _logger.LogWarning("Yêu cầu OTP thất bại: Thông tin BookingCode hoặc Số điện thoại không khớp. (Anti-enumeration response)");
            // Trả về dummy ChallengeId ngẫu nhiên để client không phân biệt được booking có tồn tại hay không
            return new RequestOtpResult(true, GenericAcknowledgmentMessage, Guid.NewGuid(), 60);
        }

        // 4. Kiểm tra Resend Cooldown (60 giây giữa các lần yêu cầu cùng mục đích)
        var latestChallenge = await _context.OtpChallenges
            .Where(c => c.AppointmentId == appointment.Id && c.Purpose == purpose && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestChallenge != null)
        {
            var elapsedSeconds = (DateTime.UtcNow - latestChallenge.CreatedAt).TotalSeconds;
            if (elapsedSeconds < 60)
            {
                var remaining = 60 - (int)elapsedSeconds;
                return new RequestOtpResult(
                    false,
                    $"Vui lòng đợi {remaining} giây trước khi yêu cầu gửi lại mã OTP.",
                    latestChallenge.Id,
                    remaining);
            }
        }

        // 5. Sinh mã OTP ngẫu nhiên 6 chữ số CSPRNG (Cryptographically Secure Random Number Generator)
        var rawOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        // 6. Tạo OtpChallenge với TTL 5 phút, tối đa 5 lần thử; chỉ lưu HMAC Hash, KHÔNG lưu OTP thô
        var challenge = OtpChallenge.Create(
            appointment.Id,
            appointment.PatientId,
            normalizedPhone,
            purpose,
            rawOtp,
            TimeSpan.FromMinutes(5),
            maxAttempts: 5);

        _context.OtpChallenges.Add(challenge);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Gửi SMS thông báo qua adapter ISmsSender
        await _smsSender.SendOtpAsync(normalizedPhone, rawOtp, purpose, cancellationToken);

        // 8. Ghi nhận lượt yêu cầu để kiểm soát rate limit
        _abuseProtectionService.RecordBookingAttempt(clientIp, normalizedPhone);

        var maskedPhone = _maskingService.MaskPhone(normalizedPhone);
        _logger.LogInformation("Phát hành OTP Challenge {ChallengeId} thành công cho số {MaskedPhone}, mục đích {Purpose}.", 
            challenge.Id, maskedPhone, purpose);

        return new RequestOtpResult(true, GenericAcknowledgmentMessage, challenge.Id, 60);
    }

    public async Task<VerifyOtpResult> VerifyOtpAsync(
        Guid challengeId, 
        string otp, 
        OtpPurpose expectedPurpose, 
        CancellationToken cancellationToken = default)
    {
        if (challengeId == Guid.Empty || string.IsNullOrWhiteSpace(otp))
        {
            return new VerifyOtpResult(false, "Thông tin xác thực OTP không hợp lệ.", challengeId, null, null, null);
        }

        var challenge = await _context.OtpChallenges
            .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken);

        if (challenge == null)
        {
            return new VerifyOtpResult(false, "Yêu cầu xác thực OTP không tồn tại hoặc đã bị hủy.", challengeId, null, null, null);
        }

        // Kiểm tra ràng buộc mục đích (Purpose Binding)
        if (challenge.Purpose != expectedPurpose)
        {
            _logger.LogWarning("Mã OTP Challenge {ChallengeId} có mục đích {ActualPurpose} nhưng bị gửi yêu cầu verify cho {ExpectedPurpose}.",
                challenge.Id, challenge.Purpose, expectedPurpose);

            return new VerifyOtpResult(
                false, 
                "Mã OTP này không dành cho mục đích được yêu cầu.", 
                challenge.Id, 
                challenge.AppointmentId, 
                challenge.PatientId, 
                challenge.Purpose);
        }

        // Tiến hành xác thực an toàn
        var isValid = challenge.Verify(otp, DateTime.UtcNow, out var errorMessage);

        await _context.SaveChangesAsync(cancellationToken);

        if (!isValid)
        {
            _logger.LogWarning("Xác thực OTP Challenge {ChallengeId} thất bại: {Error}. Lần thử: {Attempts}/{Max}",
                challenge.Id, errorMessage, challenge.AttemptsCount, challenge.MaxAttempts);

            return new VerifyOtpResult(
                false, 
                errorMessage, 
                challenge.Id, 
                challenge.AppointmentId, 
                challenge.PatientId, 
                challenge.Purpose);
        }

        _logger.LogInformation("Xác thực OTP Challenge {ChallengeId} thành công cho Appointment {AppointmentId}, mục đích {Purpose}.",
            challenge.Id, challenge.AppointmentId, challenge.Purpose);

        return new VerifyOtpResult(
            true, 
            null, 
            challenge.Id, 
            challenge.AppointmentId, 
            challenge.PatientId, 
            challenge.Purpose);
    }
}
