using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Features.Otp.Services;

public class OtpGrantService : IOtpGrantService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<OtpGrantService> _logger;

    public OtpGrantService(IApplicationDbContext context, ILogger<OtpGrantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IssueGrantResult> IssueGrantAsync(
        Guid challengeId, 
        TimeSpan? ttl = null, 
        CancellationToken cancellationToken = default)
    {
        var challenge = await _context.OtpChallenges
            .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken);

        if (challenge == null)
        {
            return new IssueGrantResult(false, "Không tìm thấy yêu cầu xác thực OTP.", null, null, null, null, null);
        }

        if (!challenge.IsUsed)
        {
            return new IssueGrantResult(false, "Yêu cầu OTP chưa được xác thực thành công.", null, null, null, null, null);
        }

        // Kiểm tra xem Challenge này đã từng được cấp Grant trước đó chưa (1:1 constraint)
        var existingGrant = await _context.OtpVerificationGrants
            .FirstOrDefaultAsync(g => g.ChallengeId == challengeId, cancellationToken);

        if (existingGrant != null)
        {
            return new IssueGrantResult(false, "Mã xác thực OTP này đã được sử dụng để cấp quyền truy cập trước đó.", null, null, null, null, null);
        }

        var (grant, rawToken) = OtpVerificationGrant.Issue(
            challenge.Id,
            challenge.AppointmentId,
            challenge.Purpose,
            ttl ?? TimeSpan.FromMinutes(15));

        _context.OtpVerificationGrants.Add(grant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã cấp Scoped Access Grant {GrantId} thành công cho Appointment {AppointmentId}, Purpose {Purpose}.",
            grant.Id, grant.AppointmentId, grant.Purpose);

        return new IssueGrantResult(
            true, 
            null, 
            rawToken, 
            grant.Id, 
            grant.AppointmentId, 
            grant.Purpose, 
            grant.ExpiresAt);
    }

    public async Task<ValidateGrantResult> ValidateAndConsumeGrantAsync(
        string rawGrantToken, 
        Guid appointmentId, 
        OtpPurpose expectedPurpose, 
        bool consume = true, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawGrantToken))
        {
            return new ValidateGrantResult(false, "Thiếu mã quyền truy cập (Grant token).", null, null, null);
        }

        var tokenHash = OtpVerificationGrant.ComputeTokenHash(rawGrantToken);

        var grant = await _context.OtpVerificationGrants
            .FirstOrDefaultAsync(g => g.GrantTokenHash == tokenHash, cancellationToken);

        if (grant == null)
        {
            return new ValidateGrantResult(false, "Quyền truy cập (Grant token) không tồn tại hoặc không hợp lệ.", null, null, null);
        }

        // Kiểm tra ràng buộc cuộc hẹn, mục đích và thời hạn
        if (!grant.IsValidFor(appointmentId, expectedPurpose, DateTime.UtcNow, out var errorMessage))
        {
            _logger.LogWarning("Kiểm tra Grant {GrantId} thất bại: {Reason}", grant.Id, errorMessage);
            return new ValidateGrantResult(false, errorMessage, grant.Id, grant.AppointmentId, grant.Purpose);
        }

        // Nếu là thao tác một lần (One-time action: Reschedule hoặc Cancel), đánh dấu đã sử dụng (consume)
        if (consume)
        {
            grant.Consume(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Grant {GrantId} đã được tiêu thụ (Consumed) thành công.", grant.Id);
        }

        return new ValidateGrantResult(true, null, grant.Id, grant.AppointmentId, grant.Purpose);
    }

    public async Task<bool> RevokeGrantAsync(string rawGrantToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawGrantToken))
            return false;

        var tokenHash = OtpVerificationGrant.ComputeTokenHash(rawGrantToken);
        var grant = await _context.OtpVerificationGrants
            .FirstOrDefaultAsync(g => g.GrantTokenHash == tokenHash, cancellationToken);

        if (grant == null || grant.ConsumedAt.HasValue)
            return false;

        grant.Revoke(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Grant {GrantId} đã được thu hồi (Revoked).", grant.Id);
        return true;
    }
}
