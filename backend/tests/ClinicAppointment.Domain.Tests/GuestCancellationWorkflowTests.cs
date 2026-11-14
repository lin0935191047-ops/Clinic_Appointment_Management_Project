using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class GuestCancellationWorkflowTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient, Doctor Doctor, Service Service, string CancelGrantToken) SeedAppointmentWithCancelGrant(
        ApplicationDbContext db,
        OtpPurpose grantPurpose = OtpPurpose.CANCEL,
        DateTime? appointmentStartAt = null,
        decimal paidAmount = 250_000m,
        AppointmentStatus status = AppointmentStatus.PendingConfirmation)
    {
        var patient = Patient.Create("Vũ Đình Hưng", "0977889900", new DateOnly(1991, 3, 25), 1991);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.cancelwf@clinic.vn", "hash");
        var spec = Specialty.Create("CK-UNG", "Ung Bướu", "ung-buou");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-ONCO-01", "BS. Vũ Ung Bướu");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-UNG-01", "Tầm soát & Tư vấn Ung Bướu", 30, 250_000m);
        db.Services.Add(service);

        var startAt = appointmentStartAt ?? DateTime.UtcNow.AddHours(3); // 3 giờ nữa (>= 60 phút)
        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            startAt,
            "Khám tầm soát u tuyến giáp");
        appointment.PaidAmount = paidAmount;

        if (status == AppointmentStatus.Confirmed) appointment.Confirm();
        else if (status == AppointmentStatus.Cancelled) appointment.Cancel("Đã hủy từ trước", DateTime.UtcNow);
        else if (status == AppointmentStatus.Completed) appointment.Status = AppointmentStatus.Completed;

        db.Appointments.Add(appointment);

        // Tạo OTP challenge và grant token
        var rawOtp = "998877";
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
    public async Task Cancel_WhenCancelledMoreThan60MinutesBefore_ShouldRefund100Percent_AndCancelAppointment()
    {
        // Arrange: Hủy trước giờ hẹn 3 tiếng (>= 60 phút) -> Hoàn 100%, phí 0%
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(
            db, 
            appointmentStartAt: DateTime.UtcNow.AddHours(3),
            paidAmount: 250_000m);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(
            appointment.BookingCode,
            Reason: "Có lịch công tác đột xuất",
            GrantToken: grantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(AppointmentStatus.Cancelled);
        result.Value.CancellationReason.Should().Be("Có lịch công tác đột xuất");
        result.Value.RefundRate.Should().Be(1.0m);
        result.Value.CancellationFeeRate.Should().Be(0.0m);
        result.Value.CancellationFeeAmount.Should().Be(0.0m);
        result.Value.ExpectedRefundAmount.Should().Be(250_000m);
        result.Value.RefundStatus.Should().Be(RefundStatus.Pending);

        // Acceptance Criteria 3: Appointment status Cancelled
        var cancelledAppointment = await db.Appointments.FindAsync(appointment.Id);
        cancelledAppointment!.Status.Should().Be(AppointmentStatus.Cancelled);
        cancelledAppointment.CancellationAcceptedAt.Should().NotBeNull();
        cancelledAppointment.CancellationReason.Should().Be("Có lịch công tác đột xuất");

        // Acceptance Criteria 4: Bản ghi Refund được tạo trong DB
        var refund = await db.Refunds.FirstOrDefaultAsync(r => r.AppointmentId == appointment.Id);
        refund.Should().NotBeNull();
        refund!.PaidServiceAmount.Should().Be(250_000m);
        refund.CancellationFeeRate.Should().Be(0.0m);
        refund.CancellationFeeAmount.Should().Be(0.0m);
        refund.ExpectedRefundAmount.Should().Be(250_000m);
        refund.Status.Should().Be(RefundStatus.Pending);

        // Acceptance Criteria 1: Grant token đã bị tiêu thụ (One-time use)
        var consumedGrant = await db.OtpVerificationGrants.FirstOrDefaultAsync(g => g.AppointmentId == appointment.Id);
        consumedGrant!.ConsumedAt.Should().NotBeNull();

        // Thử gọi hủy lại ca khám đã bị hủy -> Phải bị từ chối 400 Bad Request
        var secondCommand = new CancelAppointmentCommand(appointment.BookingCode, "Thử hủy lại", grantToken);
        var secondResult = await handler.Handle(secondCommand, CancellationToken.None);
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.StatusCode.Should().Be(400);
        secondResult.Error.Should().Contain("đã bị hủy");
    }

    [Fact]
    public async Task Cancel_WhenGrantTokenAlreadyConsumed_ShouldFailWith403()
    {
        // Arrange: Cấp grant token và đánh dấu đã tiêu thụ
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(db);

        // Đánh dấu grant đã tiêu thụ
        var grant = await db.OtpVerificationGrants.FirstAsync(g => g.AppointmentId == appointment.Id);
        grant.ConsumedAt = DateTime.UtcNow;
        db.SaveChanges();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(appointment.BookingCode, "Hủy lịch", grantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: One-time use: grant đã tiêu thụ phải bị từ chối 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("đã được sử dụng");
    }

    [Fact]
    public async Task Cancel_WhenCancelledUnder60MinutesBefore_ShouldRefund50Percent()
    {
        // Arrange: Hủy trước giờ hẹn 30 phút (< 60 phút) -> Hoàn 50%, phí hủy 50%
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(
            db, 
            appointmentStartAt: DateTime.UtcNow.AddMinutes(30),
            paidAmount: 250_000m);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(
            appointment.BookingCode,
            Reason: "Không kịp đến phòng khám",
            GrantToken: grantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value!.RefundRate.Should().Be(0.50m);
        result.Value.CancellationFeeRate.Should().Be(0.50m);
        result.Value.CancellationFeeAmount.Should().Be(125_000m);
        result.Value.ExpectedRefundAmount.Should().Be(125_000m);

        var refund = await db.Refunds.FirstOrDefaultAsync(r => r.AppointmentId == appointment.Id);
        refund.Should().NotBeNull();
        refund!.CancellationFeeAmount.Should().Be(125_000m);
        refund.ExpectedRefundAmount.Should().Be(125_000m);
    }

    [Fact]
    public async Task Cancel_WhenUnpaid_ShouldCancelWithoutCreatingPendingRefund()
    {
        // Arrange: Ca khám chưa thanh toán (PaidAmount = 0)
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(
            db, 
            appointmentStartAt: DateTime.UtcNow.AddHours(2),
            paidAmount: 0m);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(
            appointment.BookingCode,
            GrantToken: grantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AppointmentStatus.Cancelled);
        result.Value.PaidAmount.Should().Be(0m);
        result.Value.ExpectedRefundAmount.Should().Be(0m);
        result.Value.RefundStatus.Should().Be(RefundStatus.NotApplicable);

        // Không tạo bản ghi Refund pending
        var refund = await db.Refunds.FirstOrDefaultAsync(r => r.AppointmentId == appointment.Id);
        refund.Should().BeNull();
    }

    [Fact]
    public async Task Cancel_WithoutGrantToken_ShouldFailWith401()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _) = SeedAppointmentWithCancelGrant(db);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(appointment.BookingCode, GrantToken: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Acceptance Criteria 1: CANCEL OTP required
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Error.Should().Contain("Yêu cầu xác thực OTP");
    }

    [Fact]
    public async Task Cancel_WithWrongPurposeGrant_ShouldFailWith403()
    {
        // Arrange: Grant có purpose RESCHEDULE thay vì CANCEL
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, rescheduleGrantToken) = SeedAppointmentWithCancelGrant(
            db, 
            grantPurpose: OtpPurpose.RESCHEDULE);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(appointment.BookingCode, GrantToken: rescheduleGrantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("mục đích");
    }

    [Fact]
    public async Task Cancel_WhenAppointmentAlreadyCancelled_ShouldFailWith400()
    {
        // Arrange: Ca khám đã hủy từ trước
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(
            db, 
            status: AppointmentStatus.Cancelled);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(appointment.BookingCode, GrantToken: grantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("đã bị hủy");
    }

    [Fact]
    public async Task Cancel_WhenAppointmentInPastOrAtStart_ShouldFailWith400()
    {
        // Arrange: Ca khám đã quá giờ bắt đầu
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(
            db, 
            appointmentStartAt: DateTime.UtcNow.AddMinutes(-10));

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(appointment.BookingCode, GrantToken: grantToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Acceptance Criteria 2: Self-cancel before StartAt
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("đã bắt đầu hoặc trong quá khứ");
    }

    [Fact]
    public async Task Cancel_WithNonExistentBookingCode_ShouldFailWith404()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand("APT-INVALID-999", GrantToken: "dummy");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Cancel_ShouldRecordAuditLog_WithNoPii()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, grantToken) = SeedAppointmentWithCancelGrant(
            db, 
            appointmentStartAt: DateTime.UtcNow.AddHours(4));

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var command = new CancelAppointmentCommand(appointment.BookingCode, "Bận việc đột xuất", grantToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == appointment.Id);
        log.Should().NotBeNull();
        log!.Action.Should().Be("CANCEL_APPOINTMENT");
        log.MetadataRedacted.Should().NotBeNullOrEmpty();
        log.MetadataRedacted.Should().Contain(appointment.BookingCode);
        log.MetadataRedacted.Should().NotContain("Password");
        log.MetadataRedacted.Should().NotContain("Otp");
        log.MetadataRedacted.Should().NotContain("Secret");
    }
}
