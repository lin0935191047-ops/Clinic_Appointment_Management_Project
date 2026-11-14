using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Auth.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Auth.Commands;

public record RegisterPatientCommand(RegisterPatientRequest Request) : IRequest<AuthResponseDto>;
public record LoginCommand(LoginRequest Request) : IRequest<AuthResponseDto>;
public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthResponseDto>;
public record LogoutCommand(string RefreshToken) : IRequest<bool>;
public record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request) : IRequest<bool>;
public record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<string>;
public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<bool>;
public record GetCurrentUserQuery(Guid UserId) : IRequest<UserSummaryDto>;

public class AuthCommandHandlers :
    IRequestHandler<RegisterPatientCommand, AuthResponseDto>,
    IRequestHandler<LoginCommand, AuthResponseDto>,
    IRequestHandler<RefreshTokenCommand, AuthResponseDto>,
    IRequestHandler<LogoutCommand, bool>,
    IRequestHandler<ChangePasswordCommand, bool>,
    IRequestHandler<ForgotPasswordCommand, string>,
    IRequestHandler<ResetPasswordCommand, bool>,
    IRequestHandler<GetCurrentUserQuery, UserSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;
    private readonly IPatientSecurityService _patientSecurityService;
    private readonly IPatientDuplicateDetectionService _duplicateDetector;

    public AuthCommandHandlers(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService,
        IPatientSecurityService patientSecurityService,
        IPatientDuplicateDetectionService duplicateDetector)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _patientSecurityService = patientSecurityService;
        _duplicateDetector = duplicateDetector;
    }

    public async Task<AuthResponseDto> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var normalizedEmail = req.Email.Trim().ToUpperInvariant();

        var existingUser = await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser)
            throw new InvalidOperationException($"Email '{req.Email}' đã được đăng ký trong hệ thống.");

        // Tạo User
        var passwordHash = _passwordHasher.HashPassword(req.Password);
        var user = User.Create(req.Email, passwordHash);
        _context.Users.Add(user);

        // Gán Role PATIENT
        var patientRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == Role.PatientCode, cancellationToken);

        if (patientRole == null)
        {
            patientRole = Role.Create(Role.PatientCode, "Bệnh nhân");
            _context.Roles.Add(patientRole);
        }

        user.UserRoles.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = patientRole.Id,
            AssignedAt = DateTime.UtcNow
        });

        // Tạo Patient Profile liên kết với User
        var patient = Patient.Create(
            req.FullName,
            req.PhoneNumber,
            req.DateOfBirth,
            req.BirthYear,
            user.Id,
            req.Email,
            req.Gender,
            req.Address
        );

        // Rà soát định danh mạnh nếu có NationalId
        if (!string.IsNullOrWhiteSpace(req.NationalId))
        {
            _patientSecurityService.AttachNationalId(patient, req.NationalId);
            await _patientSecurityService.CheckAndFlagDuplicateIdentifierAsync(patient, "NATIONAL_ID", req.NationalId, cancellationToken);
        }

        // Rà soát ứng viên trùng lặp (Strictly NO Auto-merge)
        var candidates = await _context.Patients
            .Where(p => p.Id != patient.Id && p.RecordStatus != "MERGED")
            .ToListAsync(cancellationToken);

        var matches = _duplicateDetector.EvaluateCandidates(patient, candidates);
        var bestMatch = matches.FirstOrDefault(m => m.RequiresManualReview);
        if (bestMatch != null)
        {
            patient.IsPossibleDuplicate = true;
            patient.RecordStatus = "REVIEW_REQUIRED";

            var reviewCase = PatientIdentityReviewCase.Create(
                patient.Id,
                bestMatch.ExistingPatient.Id,
                bestMatch.Reason
            );
            _context.PatientIdentityReviewCases.Add(reviewCase);
        }

        _context.Patients.Add(patient);

        // Cấp phát JWT & Refresh Token
        var roles = new[] { Role.PatientCode };
        var accessToken = _jwtService.GenerateAccessToken(user, roles, patient.Id, null);
        var (rawRefreshToken, tokenHash) = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresInSeconds: 3600,
            TokenType: "Bearer",
            User: new UserSummaryDto(
                Id: user.Id,
                Email: user.Email,
                Roles: roles,
                FullName: patient.FullName,
                PatientProfileId: patient.Id,
                DoctorProfileId: null
            )
        );
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var normalizedEmail = req.Email.Trim().ToUpperInvariant();

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.PatientProfile)
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");

        var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, req.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");

        user.LastLoginAt = DateTime.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var patientId = user.PatientProfile?.Id;
        var doctorId = user.DoctorProfile?.Id;
        var fullName = user.PatientProfile?.FullName ?? user.DoctorProfile?.FullName;

        var accessToken = _jwtService.GenerateAccessToken(user, roles, patientId, doctorId);
        var (rawRefreshToken, tokenHash) = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresInSeconds: 3600,
            TokenType: "Bearer",
            User: new UserSummaryDto(
                Id: user.Id,
                Email: user.Email,
                Roles: roles,
                FullName: fullName,
                PatientProfileId: patientId,
                DoctorProfileId: doctorId
            )
        );
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var rawToken = request.Request.RefreshToken;
        var tokenHash = _jwtService.HashRefreshToken(rawToken);

        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(rt => rt.User)
                .ThenInclude(u => u.PatientProfile)
            .Include(rt => rt.User)
                .ThenInclude(u => u.DoctorProfile)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked || existingToken.IsExpired)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");

        var user = existingToken.User;
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản người dùng đã bị vô hiệu hóa.");

        // Refresh Token Rotation: Thu hồi token cũ và phát hành token mới
        existingToken.RevokedAt = DateTime.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var patientId = user.PatientProfile?.Id;
        var doctorId = user.DoctorProfile?.Id;
        var fullName = user.PatientProfile?.FullName ?? user.DoctorProfile?.FullName;

        var newAccessToken = _jwtService.GenerateAccessToken(user, roles, patientId, doctorId);
        var (newRawRefreshToken, newTokenHash) = _jwtService.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        existingToken.ReplacedByTokenId = newRefreshTokenEntity.Id;
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRawRefreshToken,
            ExpiresInSeconds: 3600,
            TokenType: "Bearer",
            User: new UserSummaryDto(
                Id: user.Id,
                Email: user.Email,
                Roles: roles,
                FullName: fullName,
                PatientProfileId: patientId,
                DoctorProfileId: doctorId
            )
        );
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return true;

        var tokenHash = _jwtService.HashRefreshToken(request.RefreshToken);
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token != null && !token.IsRevoked)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null || !user.IsActive)
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");

        var valid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Request.CurrentPassword);
        if (!valid)
            throw new ArgumentException("Mật khẩu hiện tại không chính xác.");

        user.PasswordHash = _passwordHasher.HashPassword(request.Request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Request.Email.Trim().ToUpperInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        // Bảo mật: Nếu email không tồn tại, vẫn trả về thành công giả định để tránh brute-force enumerate email
        if (user == null)
            return "SUCCESS_IF_EXISTS";

        // Sinh reset token 32 ký tự an toàn
        var resetToken = Guid.NewGuid().ToString("N");
        // Lưu tạm vào database hoặc cache; ở mức Clean Architecture v1, trả về token mô phỏng (Provider SMS/Email là TBD)
        return resetToken;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Request.Email.Trim().ToUpperInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive)
            throw new ArgumentException("Yêu cầu đặt lại mật khẩu không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.Request.ResetToken))
            throw new ArgumentException("Mã xác thực không hợp lệ.");

        user.PasswordHash = _passwordHasher.HashPassword(request.Request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserSummaryDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.PatientProfile)
            .Include(u => u.DoctorProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy thông tin người dùng.");

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var fullName = user.PatientProfile?.FullName ?? user.DoctorProfile?.FullName;

        return new UserSummaryDto(
            Id: user.Id,
            Email: user.Email,
            Roles: roles,
            FullName: fullName,
            PatientProfileId: user.PatientProfile?.Id,
            DoctorProfileId: user.DoctorProfile?.Id
        );
    }
}
