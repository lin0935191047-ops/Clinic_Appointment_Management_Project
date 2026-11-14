using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class AppointmentLookupTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient, Doctor Doctor, Service Service, string LookupGrantToken) SeedAppointmentWithGrant(
        ApplicationDbContext db,
        OtpPurpose grantPurpose = OtpPurpose.LOOKUP,
        DateTime? appointmentStartAt = null,
        AppointmentStatus status = AppointmentStatus.PendingConfirmation)
    {
        var patient = Patient.Create("Đỗ Văn Tra Cứu", "0933333333", new DateOnly(1985, 5, 20), 1985, null, "docuu@example.com");
        db.Patients.Add(patient);

        var user = User.Create("doctor.lookup@clinic.vn", "hash");
        var spec = Specialty.Create("CK-TIM", "Tim Mạch", "tim-mach");
        db.Users.Add(user);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-LOOKUP", "BS. Nguyễn Tim Mạch", title: "ThS.BS", roomNumber: "P.302");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-TIM", "Khám Tim Chuyên Sâu", 45, 350000m);
        db.Services.Add(service);

        var startAt = appointmentStartAt ?? DateTime.UtcNow.AddDays(2);
        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            startAt,
            "Đau ngực trái khi gắng sức");

        if (status == AppointmentStatus.Confirmed) appointment.Confirm();
        else if (status == AppointmentStatus.Cancelled) appointment.Cancel("Bận việc", DateTime.UtcNow);

        db.Appointments.Add(appointment);

        // Tạo OtpChallenge và OtpVerificationGrant
        var challenge = OtpChallenge.Create(appointment.Id, patient.Id, "0933333333", grantPurpose, "123456");
        challenge.IsUsed = true;
        db.OtpChallenges.Add(challenge);

        var (grant, rawToken) = OtpVerificationGrant.Issue(challenge.Id, appointment.Id, grantPurpose, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(grant);

        db.SaveChanges();

        return (appointment, patient, doctor, service, rawToken);
    }

    [Fact]
    public async Task Lookup_WithValidBookingCodeAndValidLookupGrant_ReturnsMinimalSummary()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, service, grantToken) = SeedAppointmentWithGrant(db, OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var query = new LookupAppointmentQuery(appointment.BookingCode, grantToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();

        var summary = result.Value!;
        summary.BookingCode.Should().Be(appointment.BookingCode);
        summary.Status.Should().Be(AppointmentStatus.PendingConfirmation);
        summary.PatientFullName.Should().Be("Đỗ Văn Tra Cứu");
        summary.DoctorFullName.Should().Be("BS. Nguyễn Tim Mạch");
        summary.DoctorTitle.Should().Be("ThS.BS");
        summary.DoctorRoomNumber.Should().Be("P.302");
        summary.SpecialtyName.Should().Be("Tim Mạch");
        summary.ServiceName.Should().Be("Khám Tim Chuyên Sâu");
        summary.ServicePrice.Should().Be(350000m);
        summary.ServiceDurationMinutes.Should().Be(45);
        summary.PaidAmount.Should().Be(0.0m);
        summary.ReasonForVisit.Should().Be("Đau ngực trái khi gắng sức");
        summary.CanCancel.Should().BeTrue();
        summary.CanReschedule.Should().BeTrue();
    }

    [Fact]
    public async Task Lookup_WithoutGrantToken_FailsWith401()
    {
        // Arrange: Acceptance Criteria 1 — Không cho phép lookup chỉ bằng BookingCode
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _) = SeedAppointmentWithGrant(db);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var query = new LookupAppointmentQuery(appointment.BookingCode, GrantToken: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Error.Should().Contain("Yêu cầu xác thực OTP để tra cứu thông tin ca khám");
    }

    [Fact]
    public async Task Lookup_WithInvalidOrExpiredGrantToken_FailsWith403()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _) = SeedAppointmentWithGrant(db);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var invalidGrantToken = "grnt_invalidtoken1234567890abcdef";
        var query = new LookupAppointmentQuery(appointment.BookingCode, invalidGrantToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Quyền truy cập (Grant token) không tồn tại hoặc không hợp lệ");
    }

    [Fact]
    public async Task Lookup_WithGrantForWrongPurpose_FailsWith403()
    {
        // Arrange: Grant được cấp cho CANCEL hoặc RESCHEDULE, mang đi LOOKUP
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, cancelGrantToken) = SeedAppointmentWithGrant(db, grantPurpose: OtpPurpose.CANCEL);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var query = new LookupAppointmentQuery(appointment.BookingCode, cancelGrantToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("không thể dùng cho mục đích LOOKUP");
    }

    [Fact]
    public async Task Lookup_WithGrantForDifferentAppointment_FailsWith403()
    {
        // Arrange: Grant của Appointment 1 mang đi tra cứu Appointment 2
        using var db = CreateInMemoryDbContext();
        var (apt1, _, _, _, grantTokenApt1) = SeedAppointmentWithGrant(db);
        var (apt2, _, _, _, _) = SeedAppointmentWithGrant(db);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var query = new LookupAppointmentQuery(apt2.BookingCode, grantTokenApt1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("không thuộc về cuộc hẹn này");
    }

    [Fact]
    public async Task Lookup_WithNonExistentBookingCode_Returns404NotFound()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var query = new LookupAppointmentQuery("NON-EXISTENT-CODE", "grnt_somevalidtoken");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Contain("Không tìm thấy thông tin ca khám");
    }

    [Fact]
    public void Lookup_EnsuresNoConsultationOrRawPiiInResponse()
    {
        // Arrange: Acceptance Criteria 3 — Phân tích kiểu dữ liệu của PublicAppointmentSummaryDto
        var properties = typeof(PublicAppointmentSummaryDto).GetProperties();
        var propNames = properties.Select(p => p.Name).ToList();

        // Assert: Tuyệt đối không chứa PatientId, không chứa Consultation/Bệnh án, không chứa CCCD/Passport
        propNames.Should().NotContain("PatientId");
        propNames.Should().NotContain("DoctorId");
        propNames.Should().NotContain("UserId");
        propNames.Should().NotContain("Consultation");
        propNames.Should().NotContain("Symptoms");
        propNames.Should().NotContain("Diagnosis");
        propNames.Should().NotContain("ExaminationNotes");
        propNames.Should().NotContain("NationalId");
        propNames.Should().NotContain("Passport");
        propNames.Should().NotContain("HealthInsurance");
    }

    [Fact]
    public async Task Lookup_CanCancelAndCanReschedule_AreComputedAccurately()
    {
        // Arrange: Ca khám đã bị hủy trong quá khứ -> CanCancel = false, CanReschedule = false
        using var db = CreateInMemoryDbContext();
        var pastTime = DateTime.UtcNow.AddDays(-1);
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithGrant(
            db, OtpPurpose.LOOKUP, appointmentStartAt: pastTime, status: AppointmentStatus.Cancelled);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new LookupAppointmentQueryHandler(db, grantService);

        var query = new LookupAppointmentQuery(appointment.BookingCode, grantToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CanCancel.Should().BeFalse("Lịch hẹn ở quá khứ hoặc đã hủy không thể hủy tiếp");
        result.Value.CanReschedule.Should().BeFalse("Lịch hẹn ở quá khứ hoặc đã hủy không thể đổi lịch tiếp");
    }

    [Fact]
    public void LookupAppointmentQueryValidator_RejectsMissingCodeOrGrant()
    {
        var validator = new LookupAppointmentQueryValidator();

        var missingCode = new LookupAppointmentQuery("", "grnt_token");
        var result1 = validator.TestValidate(missingCode);
        result1.ShouldHaveValidationErrorFor(x => x.BookingCode);

        var missingGrant = new LookupAppointmentQuery("APT-123", null);
        var result2 = validator.TestValidate(missingGrant);
        result2.ShouldHaveValidationErrorFor(x => x.GrantToken);
    }
}
