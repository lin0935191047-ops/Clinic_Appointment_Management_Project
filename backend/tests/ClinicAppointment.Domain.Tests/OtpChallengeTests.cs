using System.Security.Cryptography;
using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Otp.DTOs;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class OtpChallengeTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient) SeedAppointmentWithPatient(ApplicationDbContext db, string phone = "0912345678")
    {
        var patient = Patient.Create("Bệnh Nhân OTP", phone, new DateOnly(1990, 1, 1), 1990);
        db.Patients.Add(patient);

        var user = User.Create("doctor.otp@clinic.vn", "hash");
        var spec = Specialty.Create("CK-OTP", "Khoa OTP", "khoa-otp");
        db.Users.Add(user);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-OTP", "BS. Bác Sĩ OTP");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-OTP", "Dịch vụ OTP", 30, 200000m);
        db.Services.Add(service);

        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            DateTime.UtcNow.AddDays(1),
            "Khám thử OTP");

        db.Appointments.Add(appointment);
        db.SaveChanges();

        return (appointment, patient);
    }

    private (OtpChallengeService Service, MockSmsSender SmsSender, IAbuseProtectionService AbuseProtection) CreateOtpService(ApplicationDbContext db)
    {
        var maskingService = new IdentifierMaskingService();
        var smsSender = new MockSmsSender(NullLogger<MockSmsSender>.Instance, maskingService);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var service = new OtpChallengeService(
            db, 
            smsSender, 
            abuseProtection, 
            maskingService, 
            NullLogger<OtpChallengeService>.Instance);

        return (service, smsSender, abuseProtection);
    }

    [Fact]
    public async Task RequestOtp_WhenBookingAndPhoneMatch_CreatesChallenge_StoresOnlyHash_AndSendsSms()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, smsSender, _) = CreateOtpService(db);

        // Act
        var result = await service.RequestOtpAsync(
            appointment.BookingCode,
            "0912345678",
            OtpPurpose.LOOKUP,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(OtpChallengeService.GenericAcknowledgmentMessage);
        result.ChallengeId.Should().NotBeEmpty();

        // 1. Invariant: Database CHỈ LƯU Hash, KHÔNG lưu OTP thô
        var challengeInDb = await db.OtpChallenges.FindAsync(result.ChallengeId);
        challengeInDb.Should().NotBeNull();
        challengeInDb!.AppointmentId.Should().Be(appointment.Id);
        challengeInDb.PatientId.Should().Be(patient.Id);
        challengeInDb.Purpose.Should().Be(OtpPurpose.LOOKUP);
        challengeInDb.OtpHash.Should().NotBeNullOrWhiteSpace();
        challengeInDb.OtpHash.Length.Should().Be(64, "HMAC-SHA256 hex string có độ dài chuẩn 64 ký tự");
        challengeInDb.IsUsed.Should().BeFalse();
        challengeInDb.AttemptsCount.Should().Be(0);

        // 2. Invariant: SMS Sender đã gửi thành công tới số đã mask
        smsSender.SentHistory.Should().HaveCount(1);
        smsSender.SentHistory[0].MaskedPhone.Should().Be("091***5678");
        smsSender.SentHistory[0].Purpose.Should().Be(OtpPurpose.LOOKUP);
    }

    [Fact]
    public async Task RequestOtp_WhenBookingOrPhoneMismatch_ReturnsGenericAcknowledgment_WithoutCreatingChallenge_AntiEnumeration()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, smsSender, _) = CreateOtpService(db);

        // Act: Gửi sai số điện thoại
        var result = await service.RequestOtpAsync(
            appointment.BookingCode,
            "0999999999", // Số không khớp
            OtpPurpose.LOOKUP,
            "127.0.0.1",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue("Luôn trả về generic success để chống dò tìm mã đặt lịch");
        result.Message.Should().Be(OtpChallengeService.GenericAcknowledgmentMessage);

        // Không tạo challenge thật trong DB
        var challengeCount = await db.OtpChallenges.CountAsync();
        challengeCount.Should().Be(0);

        // Không gửi SMS nào
        smsSender.SentHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task RequestOtp_WithinCooldownPeriod_ReturnsCooldownError()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        // Lần 1: Yêu cầu thành công
        var firstResult = await service.RequestOtpAsync(appointment.BookingCode, "0912345678", OtpPurpose.LOOKUP, "127.0.0.1");
        firstResult.IsSuccess.Should().BeTrue();

        // Act: Lần 2 gửi lại ngay lập tức (trong vòng 60 giây)
        var secondResult = await service.RequestOtpAsync(appointment.BookingCode, "0912345678", OtpPurpose.LOOKUP, "127.0.0.1");

        // Assert
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.Message.Should().Contain("Vui lòng đợi");
        secondResult.Message.Should().Contain("giây trước khi yêu cầu gửi lại mã OTP");
    }

    [Fact]
    public async Task VerifyOtp_WithCorrectOtpAndPurpose_SucceedsAndMarksUsed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        // Tạo thủ công challenge với OTP đã biết là "123456"
        var challenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "123456", TimeSpan.FromMinutes(5));
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // Act
        var verifyResult = await service.VerifyOtpAsync(challenge.Id, "123456", OtpPurpose.LOOKUP);

        // Assert
        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.ErrorMessage.Should().BeNull();
        verifyResult.AppointmentId.Should().Be(appointment.Id);
        verifyResult.PatientId.Should().Be(patient.Id);
        verifyResult.Purpose.Should().Be(OtpPurpose.LOOKUP);

        // Kiểm tra database: Đã đánh dấu IsUsed = true
        var updatedChallenge = await db.OtpChallenges.FindAsync(challenge.Id);
        updatedChallenge!.IsUsed.Should().BeTrue();
        updatedChallenge.AttemptsCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyOtp_WithIncorrectOtp_DecrementsRemainingAttempts_Fails()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        var challenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "123456", TimeSpan.FromMinutes(5), maxAttempts: 5);
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // Act: Nhập sai OTP
        var verifyResult = await service.VerifyOtpAsync(challenge.Id, "999999", OtpPurpose.LOOKUP);

        // Assert
        verifyResult.IsSuccess.Should().BeFalse();
        verifyResult.ErrorMessage.Should().Contain("Mã xác thực OTP không chính xác");
        verifyResult.ErrorMessage.Should().Contain("Bạn còn 4 lần thử");

        var updatedChallenge = await db.OtpChallenges.FindAsync(challenge.Id);
        updatedChallenge!.IsUsed.Should().BeFalse();
        updatedChallenge.AttemptsCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyOtp_WhenExceedingMaxAttempts_LocksChallenge_PermanentlyFails()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        var challenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "123456", TimeSpan.FromMinutes(5), maxAttempts: 3);
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // Nhập sai 3 lần
        await service.VerifyOtpAsync(challenge.Id, "000001", OtpPurpose.LOOKUP);
        await service.VerifyOtpAsync(challenge.Id, "000002", OtpPurpose.LOOKUP);
        var res3 = await service.VerifyOtpAsync(challenge.Id, "000003", OtpPurpose.LOOKUP);

        res3.IsSuccess.Should().BeFalse();
        res3.ErrorMessage.Should().Contain("Bạn đã hết lượt thử");

        // Lần thứ 4 dù nhập đúng OTP "123456" vẫn bị từ chối do đã bị khóa
        var res4 = await service.VerifyOtpAsync(challenge.Id, "123456", OtpPurpose.LOOKUP);
        res4.IsSuccess.Should().BeFalse();
        res4.ErrorMessage.Should().Contain("đã vượt quá giới hạn 3 lần thử và đã bị khóa");
    }

    [Fact]
    public async Task VerifyOtp_WhenExpired_Fails()
    {
        // Arrange: Challenge đã hết hạn
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        var challenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "123456", TimeSpan.FromMinutes(-1));
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // Act
        var result = await service.VerifyOtpAsync(challenge.Id, "123456", OtpPurpose.LOOKUP);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Mã xác thực OTP đã hết hạn");
    }

    [Fact]
    public async Task VerifyOtp_WhenAlreadyUsed_Fails()
    {
        // Arrange: Challenge đã sử dụng
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        var challenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "123456", TimeSpan.FromMinutes(5));
        challenge.IsUsed = true;
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // Act
        var result = await service.VerifyOtpAsync(challenge.Id, "123456", OtpPurpose.LOOKUP);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("đã được sử dụng trước đó");
    }

    [Fact]
    public async Task VerifyOtp_WhenPurposeMismatch_Fails_PurposeBinding()
    {
        // Arrange: Challenge cấp cho mục đích LOOKUP, nhưng client cố tình verify cho RESCHEDULE
        using var db = CreateInMemoryDbContext();
        var (appointment, patient) = SeedAppointmentWithPatient(db, "0912345678");
        var (service, _, _) = CreateOtpService(db);

        var challenge = OtpChallenge.Create(
            appointment.Id, patient.Id, "0912345678", OtpPurpose.LOOKUP, "123456", TimeSpan.FromMinutes(5));
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // Act: Verify với mục đích RESCHEDULE
        var result = await service.VerifyOtpAsync(challenge.Id, "123456", OtpPurpose.RESCHEDULE);

        // Assert: Thất bại do sai purpose binding
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Mã OTP này không dành cho mục đích được yêu cầu");
    }

    [Fact]
    public void OtpValidators_ValidateCorrectly()
    {
        var reqValidator = new RequestOtpRequestValidator();
        var verifyValidator = new VerifyOtpRequestValidator();

        // Request validator
        var badPhoneReq = new RequestOtpRequest("APT-123456", "invalid-phone", OtpPurpose.LOOKUP);
        reqValidator.TestValidate(badPhoneReq).ShouldHaveValidationErrorFor(x => x.PhoneNumber);

        var emptyCodeReq = new RequestOtpRequest("", "0912345678", OtpPurpose.LOOKUP);
        reqValidator.TestValidate(emptyCodeReq).ShouldHaveValidationErrorFor(x => x.BookingCode);

        // Verify validator
        var badOtpVerify = new VerifyOtpRequest(Guid.NewGuid(), "123", OtpPurpose.LOOKUP);
        verifyValidator.TestValidate(badOtpVerify).ShouldHaveValidationErrorFor(x => x.Otp);

        var nonDigitOtp = new VerifyOtpRequest(Guid.NewGuid(), "abcdef", OtpPurpose.LOOKUP);
        verifyValidator.TestValidate(nonDigitOtp).ShouldHaveValidationErrorFor(x => x.Otp);

        var emptyGuid = new VerifyOtpRequest(Guid.Empty, "123456", OtpPurpose.LOOKUP);
        verifyValidator.TestValidate(emptyGuid).ShouldHaveValidationErrorFor(x => x.ChallengeId);
    }
}
