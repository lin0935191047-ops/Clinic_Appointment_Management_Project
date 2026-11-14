using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicAppointment.Application.Features.Auth.DTOs;
using ClinicAppointment.Application.Features.Auth.Validators;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class AuthAndJwtTests
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly PasswordHasher _passwordHasher;

    public AuthAndJwtTests()
    {
        var configBuilder = new ConfigurationBuilder();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"JwtSettings:SecretKey", "TestClinicAppointmentSuperSecretKey20261114Minimum32Bytes!"},
            {"JwtSettings:Issuer", "TestClinicApi"},
            {"JwtSettings:Audience", "TestClinicClient"},
            {"JwtSettings:AccessTokenExpirationMinutes", "15"},
            {"JwtSettings:RefreshTokenExpirationDays", "7"}
        };
        configBuilder.AddInMemoryCollection(inMemorySettings);
        var configuration = configBuilder.Build();

        _jwtTokenService = new JwtTokenService(configuration);
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainRequiredClaims_AndBeValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "patient@testclinic.vn";
        var passwordHash = _passwordHasher.HashPassword("SecureP@ss123");
        var user = User.Create(email, passwordHash);
        var patientId = Guid.NewGuid();
        var roles = new[] { Role.PatientCode };

        // Act
        var tokenString = _jwtTokenService.GenerateAccessToken(user, roles, patientId, null);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("TestClinicApi");
        jwtToken.Audiences.Should().Contain("TestClinicClient");

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(user.Id.ToString());

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(email);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(Role.PatientCode);

        var patientClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "patient_profile_id");
        patientClaim.Should().NotBeNull();
        patientClaim!.Value.Should().Be(patientId.ToString());
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceCryptographicallySecureToken_AndValidHash()
    {
        // Act
        var (rawToken, tokenHash) = _jwtTokenService.GenerateRefreshToken();

        // Assert
        rawToken.Should().NotBeNullOrWhiteSpace();
        rawToken.Length.Should().BeGreaterThanOrEqualTo(40);
        tokenHash.Should().NotBeEmpty();
        tokenHash.Length.Should().Be(32); // SHA-256 = 32 bytes

        // Hash lại rawToken phải khớp tokenHash
        var verifyHash = _jwtTokenService.HashRefreshToken(rawToken);
        verifyHash.Should().Equal(tokenHash);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldExtractClaims_WithoutThrowing()
    {
        // Arrange
        var user = User.Create("doctor@testclinic.vn", _passwordHasher.HashPassword("Doctor@123"));
        var roles = new[] { Role.DoctorCode };
        var tokenString = _jwtTokenService.GenerateAccessToken(user, roles);

        // Act
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(tokenString);

        // Assert
        principal.Should().NotBeNull();
        var sub = principal!.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        sub.Should().NotBeNull();
        sub!.Value.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_ShouldMatchGeneratedHash()
    {
        // Arrange
        var password = "StrongPassword@2026";
        var hash = _passwordHasher.HashPassword(password);

        // Act & Assert
        _passwordHasher.VerifyPassword(hash, password).Should().BeTrue();
        _passwordHasher.VerifyPassword(hash, "WrongPassword@2026").Should().BeFalse();
        _passwordHasher.VerifyPassword(hash, "").Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_Lifecycle_Properties_ShouldBeCorrect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = new byte[32],
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Assert Active
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
        token.IsActive.Should().BeTrue();

        // Thu hồi (Revoked)
        token.RevokedAt = DateTime.UtcNow;
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();

        // Hết hạn (Expired)
        var expiredToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = new byte[32],
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            RevokedAt = null
        };
        expiredToken.IsExpired.Should().BeTrue();
        expiredToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RegisterPatientRequestValidator_ShouldFail_WhenPasswordLacksRequirements()
    {
        // Arrange
        var validator = new RegisterPatientRequestValidator();
        var requestNoUpper = new RegisterPatientRequest("test@clinic.vn", "password123", "Nguyễn Văn A", "0901234567", DateOnly.Parse("1995-05-15"), 1995);
        var requestNoDigit = new RegisterPatientRequest("test@clinic.vn", "PasswordNoDigit", "Nguyễn Văn A", "0901234567", DateOnly.Parse("1995-05-15"), 1995);
        var requestTooShort = new RegisterPatientRequest("test@clinic.vn", "Pass1", "Nguyễn Văn A", "0901234567", DateOnly.Parse("1995-05-15"), 1995);

        // Act & Assert
        validator.Validate(requestNoUpper).IsValid.Should().BeFalse();
        validator.Validate(requestNoDigit).IsValid.Should().BeFalse();
        validator.Validate(requestTooShort).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterPatientRequestValidator_ShouldFail_WhenMissingBothDobAndBirthYear()
    {
        // Arrange
        var validator = new RegisterPatientRequestValidator();
        var request = new RegisterPatientRequest("test@clinic.vn", "SecureP@ss123", "Nguyễn Văn A", "0901234567", null, null);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Ngày sinh hoặc Năm sinh"));
    }

    [Fact]
    public void RegisterPatientRequestValidator_ShouldFail_WhenDobYearMismatchBirthYear()
    {
        // Arrange
        var validator = new RegisterPatientRequestValidator();
        var request = new RegisterPatientRequest("test@clinic.vn", "SecureP@ss123", "Nguyễn Văn A", "0901234567", DateOnly.Parse("1995-05-15"), 1998);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("không khớp nhau"));
    }

    [Fact]
    public void RegisterPatientRequestValidator_ShouldPass_WhenValidInput()
    {
        // Arrange
        var validator = new RegisterPatientRequestValidator();
        var requestWithDobAndYear = new RegisterPatientRequest("test@clinic.vn", "SecureP@ss123", "Trần Thị Lan", "0912345678", DateOnly.Parse("1990-10-20"), 1990);
        var requestWithOnlyYear = new RegisterPatientRequest("test@clinic.vn", "SecureP@ss123", "Trần Thị Lan", "0912345678", null, 1990);

        // Act & Assert
        validator.Validate(requestWithDobAndYear).IsValid.Should().BeTrue();
        validator.Validate(requestWithOnlyYear).IsValid.Should().BeTrue();
    }

    [Fact]
    public void User_Create_ShouldNormalizeEmail_AndSetDefaults()
    {
        // Arrange & Act
        var user = User.Create("  Patient.Test@example.COM  ", "somehash");

        // Assert
        user.Email.Should().Be("Patient.Test@example.COM");
        user.NormalizedEmail.Should().Be("PATIENT.TEST@EXAMPLE.COM");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
