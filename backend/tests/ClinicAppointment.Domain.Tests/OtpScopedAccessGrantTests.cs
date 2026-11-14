using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Otp.DTOs;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class OtpScopedAccessGrantTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient, OtpChallenge Challenge) SeedVerifiedChallenge(
        ApplicationDbContext db, 
        OtpPurpose purpose = OtpPurpose.LOOKUP)
    {
        var patient = Patient.Create("Bệnh Nhân Grant", "0912345678", new DateOnly(1990, 1, 1), 1990);
        db.Patients.Add(patient);

        var user = User.Create("doctor.grant@clinic.vn", "hash");
        var spec = Specialty.Create("CK-GRANT", "Khoa Grant", "khoa-grant");
        db.Users.Add(user);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-GRT", "BS. Bác Sĩ Grant");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-GRT", "Dịch vụ Grant", 30, 200000m);
        db.Services.Add(service);

        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            DateTime.UtcNow.AddDays(1),
            "Khám thử Scoped Grant");

        db.Appointments.Add(appointment);

        // Tạo challenge và đánh dấu đã verify (IsUsed = true)
        var challenge = OtpChallenge.Create(
            appointment.Id,
            patient.Id,
            "0912345678",
            purpose,
            "123456",
            TimeSpan.FromMinutes(5));

        challenge.IsUsed = true;
        db.OtpChallenges.Add(challenge);
        db.SaveChanges();

        return (appointment, patient, challenge);
    }

    [Fact]
    public async Task IssueGrant_WhenChallengeVerified_IssuesCryptographicOpaqueToken_StoresOnlyHash()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        // Act
        var result = await grantService.IssueGrantAsync(challenge.Id, TimeSpan.FromMinutes(15));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.GrantToken.Should().NotBeNullOrWhiteSpace();
        result.GrantToken!.Should().StartWith("grnt_");
        result.AppointmentId.Should().Be(appointment.Id);
        result.Purpose.Should().Be(OtpPurpose.LOOKUP);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Invariant: Tuyệt đối KHÔNG phải là User JWT session (không có 3 phần ngăn cách bởi dấu chấm)
        result.GrantToken!.Split('.').Length.Should().NotBe(3, "Grant token phải là opaque cryptographic token, không phải User JWT");

        // Invariant: Database CHỈ LƯU hash của token, KHÔNG lưu raw token
        var grantInDb = await db.OtpVerificationGrants.FindAsync(result.GrantId);
        grantInDb.Should().NotBeNull();
        grantInDb!.GrantTokenHash.Length.Should().Be(32, "SHA-256 binary hash có độ dài 32 bytes");
        grantInDb.ConsumedAt.Should().BeNull();
    }

    [Fact]
    public async Task IssueGrant_WhenChallengeNotVerified_Fails()
    {
        // Arrange: Challenge chưa verify (IsUsed = false)
        using var db = CreateInMemoryDbContext();
        var (appointment, patient, _) = SeedVerifiedChallenge(db);
        var unverifiedChallenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "654321");
        unverifiedChallenge.IsUsed = false;
        db.OtpChallenges.Add(unverifiedChallenge);
        await db.SaveChangesAsync();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        // Act
        var result = await grantService.IssueGrantAsync(unverifiedChallenge.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Yêu cầu OTP chưa được xác thực thành công");
    }

    [Fact]
    public async Task IssueGrant_WhenChallengeAlreadyGranted_Fails()
    {
        // Arrange: Challenge đã được cấp grant 1 lần
        using var db = CreateInMemoryDbContext();
        var (_, _, challenge) = SeedVerifiedChallenge(db);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        var firstIssue = await grantService.IssueGrantAsync(challenge.Id);
        firstIssue.IsSuccess.Should().BeTrue();

        // Act: Cố tình yêu cầu cấp thêm lần 2 từ cùng challenge
        var secondIssue = await grantService.IssueGrantAsync(challenge.Id);

        // Assert
        secondIssue.IsSuccess.Should().BeFalse();
        secondIssue.ErrorMessage.Should().Contain("đã được sử dụng để cấp quyền truy cập trước đó");
    }

    [Fact]
    public async Task ValidateGrant_WhenValidAndMatchesAppointmentAndPurpose_Succeeds()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        var issueResult = await grantService.IssueGrantAsync(challenge.Id);

        // Act: Validate grant cho mục đích LOOKUP
        var validateResult = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            appointment.Id,
            OtpPurpose.LOOKUP,
            consume: false);

        // Assert
        validateResult.IsSuccess.Should().BeTrue();
        validateResult.ErrorMessage.Should().BeNull();
        validateResult.AppointmentId.Should().Be(appointment.Id);
        validateResult.Purpose.Should().Be(OtpPurpose.LOOKUP);
    }

    [Fact]
    public async Task ValidateGrant_WhenPurposeMismatch_Fails()
    {
        // Arrange: Cấp grant cho LOOKUP, nhưng mang đi CANCEL
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        var issueResult = await grantService.IssueGrantAsync(challenge.Id);

        // Act: Dùng grant LOOKUP để validate cho CANCEL
        var validateResult = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            appointment.Id,
            OtpPurpose.CANCEL,
            consume: false);

        // Assert
        validateResult.IsSuccess.Should().BeFalse();
        validateResult.ErrorMessage.Should().Contain("không thể dùng cho mục đích CANCEL");
    }

    [Fact]
    public async Task ValidateGrant_WhenAppointmentMismatch_Fails()
    {
        // Arrange: Grant thuộc về appointment 1, mang đi kiểm tra cho appointment 2
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        var issueResult = await grantService.IssueGrantAsync(challenge.Id);

        var otherAppointmentId = Guid.NewGuid();

        // Act
        var validateResult = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            otherAppointmentId,
            OtpPurpose.LOOKUP,
            consume: false);

        // Assert
        validateResult.IsSuccess.Should().BeFalse();
        validateResult.ErrorMessage.Should().Contain("không thuộc về cuộc hẹn này");
    }

    [Fact]
    public async Task ValidateGrant_OneTimeUse_Consume_CannotBeReused()
    {
        // Arrange: Grant cho mục đích CANCEL (One-time use)
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.CANCEL);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        var issueResult = await grantService.IssueGrantAsync(challenge.Id);

        // Act 1: Tiêu thụ lần đầu (consume = true)
        var firstUse = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            appointment.Id,
            OtpPurpose.CANCEL,
            consume: true);

        firstUse.IsSuccess.Should().BeTrue();

        // Act 2: Cố tình dùng lại lần 2
        var secondUse = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            appointment.Id,
            OtpPurpose.CANCEL,
            consume: true);

        // Assert
        secondUse.IsSuccess.Should().BeFalse();
        secondUse.ErrorMessage.Should().Contain("đã được sử dụng trước đó (One-time only)");
    }

    [Fact]
    public async Task ValidateGrant_WhenExpired_Fails()
    {
        // Arrange: Grant đã hết hạn
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.RESCHEDULE);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        // Cấp grant với TTL âm (đã hết hạn)
        var issueResult = await grantService.IssueGrantAsync(challenge.Id, TimeSpan.FromMinutes(-1));

        // Act
        var result = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            appointment.Id,
            OtpPurpose.RESCHEDULE);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("đã hết hạn");
    }

    [Fact]
    public async Task RevokeGrant_SuccessfullyRevokes_AndBlocksFurtherUsage()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, challenge) = SeedVerifiedChallenge(db, OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);

        var issueResult = await grantService.IssueGrantAsync(challenge.Id);

        // Act: Thu hồi grant
        var revokeSuccess = await grantService.RevokeGrantAsync(issueResult.GrantToken!);
        revokeSuccess.Should().BeTrue();

        // Thử validate grant đã thu hồi
        var validateResult = await grantService.ValidateAndConsumeGrantAsync(
            issueResult.GrantToken!,
            appointment.Id,
            OtpPurpose.LOOKUP,
            consume: false);

        // Assert
        validateResult.IsSuccess.Should().BeFalse();
        validateResult.ErrorMessage.Should().Contain("đã được sử dụng trước đó");
    }
}
