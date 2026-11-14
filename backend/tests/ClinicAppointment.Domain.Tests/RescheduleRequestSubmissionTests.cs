using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class RescheduleRequestSubmissionTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient, Doctor Doctor, Service Service, string RescheduleGrantToken) SeedAppointmentWithRescheduleGrant(
        ApplicationDbContext db,
        OtpPurpose grantPurpose = OtpPurpose.RESCHEDULE,
        DateTime? appointmentStartAt = null,
        AppointmentStatus status = AppointmentStatus.PendingConfirmation)
    {
        var patient = Patient.Create("Hoàng Minh Tuấn", "0911223344", new DateOnly(1990, 8, 12), 1990);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.reschedule@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NHI", "Nhi Khoa", "nhi-khoa");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-RESCHEDULE-01", "BS. Hoàng Nhi Khoa");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-NHI-01", "Khám Tổng Quát Trẻ Em", 30, 250000m);
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
            "Trẻ sốt nhẹ và ho");

        if (status == AppointmentStatus.Confirmed) appointment.Confirm();
        else if (status == AppointmentStatus.Cancelled) appointment.Cancel("Người nhà hủy lịch", DateTime.UtcNow);
        else if (status == AppointmentStatus.Completed) appointment.Status = AppointmentStatus.Completed;

        db.Appointments.Add(appointment);

        // Tạo OTP challenge và grant token
        var rawOtp = "123456";
        var challenge = OtpChallenge.Create(
            appointment.Id,
            patient.Id,
            patient.PhoneNumber,
            grantPurpose,
            rawOtp);
        challenge.Verify(rawOtp, DateTime.UtcNow, out _);
        db.OtpChallenges.Add(challenge);

        var (grant, rawToken) = OtpVerificationGrant.Issue(challenge.Id, appointment.Id, grantPurpose, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(grant);

        db.SaveChanges();

        return (appointment, patient, doctor, service, rawToken);
    }

    [Fact]
    public async Task SubmitReschedule_WithValidGrant_ShouldCreatePendingReviewRequest_AndConsumeGrant()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, grantToken) = SeedAppointmentWithRescheduleGrant(db, OtpPurpose.RESCHEDULE);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var proposedStartAt = DateTime.UtcNow.AddDays(4).Date.AddHours(9); // 09:00 UTC 4 ngày sau
        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            proposedStartAt,
            grantToken,
            doctor.Id,
            "Bận việc gia đình đột xuất");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.Should().NotBeNull();
        result.Value!.BookingCode.Should().Be(appointment.BookingCode);
        result.Value.Status.Should().Be(RescheduleStatus.PendingReview);
        result.Value.ProposedStartAtUtc.Should().Be(proposedStartAt);
        result.Value.ProposedEndAtUtc.Should().Be(proposedStartAt.AddMinutes(appointment.ServiceDurationSnapshot));

        // Acceptance Criteria 2: Bản ghi RescheduleRequest được tạo ở trạng thái PendingReview
        var req = await db.RescheduleRequests.FirstOrDefaultAsync(r => r.AppointmentId == appointment.Id);
        req.Should().NotBeNull();
        req!.Status.Should().Be(RescheduleStatus.PendingReview);
        req.RequestedDoctorId.Should().Be(doctor.Id);
        req.ProposedStartAt.Should().Be(proposedStartAt);
        req.Reason.Should().Be("Bận việc gia đình đột xuất");

        // Acceptance Criteria 3: Old slot held (Lịch cũ giữ nguyên hoàn toàn)
        var currentAppointment = await db.Appointments.FindAsync(appointment.Id);
        currentAppointment!.StartAt.Should().Be(appointment.StartAt, "Khung giờ bắt đầu cũ phải được giữ nguyên");
        currentAppointment.EndAt.Should().Be(appointment.EndAt, "Khung giờ kết thúc cũ phải được giữ nguyên");
        currentAppointment.DoctorId.Should().Be(appointment.DoctorId, "Bác sĩ của ca khám cũ phải được giữ nguyên");
        currentAppointment.Status.Should().Be(AppointmentStatus.PendingConfirmation, "Trạng thái ca khám cũ phải được giữ nguyên");

        // Acceptance Criteria 1: Grant token đã bị tiêu thụ (One-time use)
        var consumedGrant = await db.OtpVerificationGrants.FirstOrDefaultAsync(g => g.AppointmentId == appointment.Id);
        consumedGrant!.ConsumedAt.Should().NotBeNull("Grant token phải được đánh dấu đã tiêu thụ sau khi tạo yêu cầu đổi lịch");

        // Thử dùng lại grant token vừa tiêu thụ -> Phải bị từ chối 403 Forbidden
        var secondCommand = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            DateTime.UtcNow.AddDays(5),
            grantToken,
            doctor.Id,
            "Thử đổi lần nữa");
        var secondResult = await handler.Handle(secondCommand, CancellationToken.None);
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task SubmitReschedule_WithoutGrantToken_ShouldFailWith401()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, _) = SeedAppointmentWithRescheduleGrant(db);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            DateTime.UtcNow.AddDays(3),
            GrantToken: null,
            doctor.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Error.Should().Contain("Yêu cầu xác thực OTP");
    }

    [Fact]
    public async Task SubmitReschedule_WithWrongPurposeGrant_ShouldFailWith403()
    {
        // Arrange: Cấp grant mục đích LOOKUP thay vì RESCHEDULE
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, lookupGrantToken) = SeedAppointmentWithRescheduleGrant(db, grantPurpose: OtpPurpose.LOOKUP);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            DateTime.UtcNow.AddDays(3),
            lookupGrantToken,
            doctor.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("mục đích");
    }

    [Fact]
    public async Task SubmitReschedule_WhenAlreadyHasPendingReviewRequest_ShouldFailWith409()
    {
        // Arrange: Đã có sẵn 1 yêu cầu đổi lịch PendingReview cho cuộc hẹn
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, grantToken) = SeedAppointmentWithRescheduleGrant(db, OtpPurpose.RESCHEDULE);
        
        var existingRequest = RescheduleRequest.Create(
            appointment.Id,
            doctor.Id,
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(3).AddMinutes(30),
            "Yêu cầu đổi lần 1");
        db.RescheduleRequests.Add(existingRequest);
        db.SaveChanges();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            DateTime.UtcNow.AddDays(4),
            grantToken,
            doctor.Id,
            "Yêu cầu đổi lần 2");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Acceptance Criteria 4: One pending request
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Contain("PendingReview");
        result.Error.Should().Contain("tối đa 1 yêu cầu");
    }

    [Fact]
    public async Task SubmitReschedule_WhenAppointmentCancelled_ShouldFailWith400()
    {
        // Arrange: Cuộc hẹn đã bị hủy
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, grantToken) = SeedAppointmentWithRescheduleGrant(
            db, 
            OtpPurpose.RESCHEDULE, 
            status: AppointmentStatus.Cancelled);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            DateTime.UtcNow.AddDays(3),
            grantToken,
            doctor.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Cancelled");
    }

    [Fact]
    public async Task SubmitReschedule_WhenAppointmentInPast_ShouldFailWith400()
    {
        // Arrange: Cuộc hẹn đã ở quá khứ
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, grantToken) = SeedAppointmentWithRescheduleGrant(
            db, 
            OtpPurpose.RESCHEDULE, 
            appointmentStartAt: DateTime.UtcNow.AddMinutes(-30));

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            DateTime.UtcNow.AddDays(3),
            grantToken,
            doctor.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("đã hoặc đang diễn ra");
    }

    [Fact]
    public async Task SubmitReschedule_WithNonExistentBookingCode_ShouldFailWith404()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            "APT-NOT-FOUND-999",
            DateTime.UtcNow.AddDays(3),
            "dummy_grant_token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SubmitReschedule_WhenSlotConflictsWithDoctorTimeOff_ShouldFailWith409()
    {
        // Arrange: Bác sĩ có lịch nghỉ trong khung giờ đề xuất
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, grantToken) = SeedAppointmentWithRescheduleGrant(db, OtpPurpose.RESCHEDULE);

        var proposedStartAt = DateTime.UtcNow.AddDays(3).Date.AddHours(14);
        var proposedEndAt = proposedStartAt.AddMinutes(30);

        var timeOff = DoctorTimeOff.Create(
            doctor.Id,
            proposedStartAt.AddHours(-1),
            proposedEndAt.AddHours(1),
            doctor.UserId,
            "Nghỉ phép cá nhân");
        db.DoctorTimeOffs.Add(timeOff);
        db.SaveChanges();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            proposedStartAt,
            grantToken,
            doctor.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Contain("lịch nghỉ");
    }

    [Fact]
    public async Task SubmitReschedule_WhenSlotConflictsWithAnotherActiveAppointment_ShouldFailWith409()
    {
        // Arrange: Có một ca khám khác của bác sĩ trong khung giờ đề xuất
        using var db = CreateInMemoryDbContext();
        var (appointment, patient, doctor, service, grantToken) = SeedAppointmentWithRescheduleGrant(db, OtpPurpose.RESCHEDULE);

        var proposedStartAt = DateTime.UtcNow.AddDays(3).Date.AddHours(10);

        var anotherAppointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            proposedStartAt,
            "Ca khám khác của bệnh nhân khác");
        db.Appointments.Add(anotherAppointment);
        db.SaveChanges();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            proposedStartAt,
            grantToken,
            doctor.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Contain("lịch hẹn khác");
    }

    [Fact]
    public async Task SubmitReschedule_ShouldRecordAuditLog_WithNoPii()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, doctor, _, grantToken) = SeedAppointmentWithRescheduleGrant(db, OtpPurpose.RESCHEDULE);
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var handler = new SubmitRescheduleRequestCommandHandler(db, grantService);

        var proposedStartAt = DateTime.UtcNow.AddDays(3).Date.AddHours(8);
        var command = new SubmitRescheduleRequestCommand(
            appointment.BookingCode,
            proposedStartAt,
            grantToken,
            doctor.Id,
            "Đổi sang ca sáng");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: Audit log được ghi nhận
        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == appointment.Id);
        log.Should().NotBeNull();
        log!.Action.Should().Be("SUBMIT_RESCHEDULE_REQUEST");
        log.MetadataRedacted.Should().NotBeNullOrEmpty();
        log.MetadataRedacted.Should().Contain(appointment.BookingCode);

        // Không chứa thông tin nhạy cảm
        log.MetadataRedacted.Should().NotContain("Password");
        log.MetadataRedacted.Should().NotContain("Otp");
        log.MetadataRedacted.Should().NotContain("Secret");
        log.MetadataRedacted.Should().NotContain("IdentityCard");
    }
}
