using ClinicAppointment.Application.Features.Appointments.Queries.PreviewCancellationPolicy;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class RefundCalculationTests
{
    private const decimal StandardFee = 200_000m;

    [Fact]
    public void Calculate_WhenCancelled61MinutesBefore_ShouldRefund100Percent()
    {
        // Arrange: 61 phút trước giờ hẹn -> Hoàn 100%, phí hủy 0%
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-61);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(61);
        result.CancellationFeeRate.Should().Be(0.0m);
        result.RefundRate.Should().Be(1.0m);
        result.CancellationFeeAmount.Should().Be(0.0m);
        result.ExpectedRefundAmount.Should().Be(StandardFee);
    }

    [Fact]
    public void Calculate_WhenCancelledExactly60MinutesBefore_ShouldRefund100Percent()
    {
        // Arrange: Đúng mốc 60 phút thuộc nhóm hoàn 100% theo SRS 1.2 & BR-010
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-60);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(60);
        result.CancellationFeeRate.Should().Be(0.0m);
        result.RefundRate.Should().Be(1.0m);
        result.CancellationFeeAmount.Should().Be(0.0m);
        result.ExpectedRefundAmount.Should().Be(StandardFee);
    }

    [Fact]
    public void Calculate_WhenCancelled59MinutesBefore_ShouldRefund50Percent()
    {
        // Arrange: Dưới 60 phút nhưng trước giờ hẹn -> Hoàn 50%, phí hủy 50%
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-59);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(59);
        result.CancellationFeeRate.Should().Be(0.50m);
        result.RefundRate.Should().Be(0.50m);
        result.CancellationFeeAmount.Should().Be(100_000m);
        result.ExpectedRefundAmount.Should().Be(100_000m);
    }

    [Theory]
    [InlineData(45, 100_000)]
    [InlineData(30, 100_000)]
    [InlineData(15, 100_000)]
    [InlineData(1, 100_000)]
    public void Calculate_VariousMinutesUnder60_ShouldRefund50Percent(int minutesBefore, decimal expectedRefund)
    {
        // Arrange
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-minutesBefore);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.CancellationFeeRate.Should().Be(0.50m);
        result.RefundRate.Should().Be(0.50m);
        result.ExpectedRefundAmount.Should().Be(expectedRefund);
        result.CancellationFeeAmount.Should().Be(StandardFee - expectedRefund);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(-60)]
    public void Calculate_WhenCancelledAtOrAfterStart_ShouldThrowException(int minutesOffset)
    {
        // Arrange: Tại hoặc sau giờ hẹn bệnh nhân không được phép tự hủy
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-minutesOffset);

        // Act & Assert
        var act = () => RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ*");
    }

    [Fact]
    public void Calculate_WhenUnpaid_ShouldRefundZeroAndFeeZero()
    {
        // Arrange: Chưa thanh toán (paidAmount = 0) -> Không tự thu phí hủy chưa có policy (TBD-04)
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-30);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, paidServiceAmount: 0m);

        // Assert
        result.CancellationFeeRate.Should().Be(0.50m);
        result.CancellationFeeAmount.Should().Be(0m);
        result.ExpectedRefundAmount.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WhenOddAmount_ShouldRoundVndToInteger()
    {
        // Arrange: Giá dịch vụ lẻ (355.555 đ) -> Làm tròn số nguyên VND chuẩn
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-30);
        decimal oddAmount = 355_555m;

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, oddAmount);

        // Assert
        // 355_555 * 0.5 = 177_777.5 -> làm tròn thành 177_778 đ
        result.CancellationFeeAmount.Should().Be(177_778m);
        result.ExpectedRefundAmount.Should().Be(177_777m);
        (result.CancellationFeeAmount + result.ExpectedRefundAmount).Should().Be(oddAmount);
    }

    [Fact]
    public void CancellationPolicyService_Evaluate_WhenPast_ShouldReturnCanCancelFalse_AndDenialReason()
    {
        // Arrange
        var service = new CancellationPolicyService();
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var serverClock = appointmentStart.AddMinutes(5); // 5 phút sau giờ hẹn

        // Act
        var evaluation = service.Evaluate(appointmentStart, StandardFee, serverClock);

        // Assert
        evaluation.CanCancel.Should().BeFalse();
        evaluation.DenialReason.Should().NotBeNullOrEmpty();
        evaluation.DenialReason.Should().Contain("quá khứ");
        evaluation.ExpectedRefundAmount.Should().Be(0m);
    }

    [Fact]
    public void CancellationPolicyService_Evaluate_When60MinutesBefore_ShouldReturn100Percent()
    {
        // Arrange
        var service = new CancellationPolicyService();
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var serverClock = appointmentStart.AddMinutes(-60);

        // Act
        var evaluation = service.Evaluate(appointmentStart, StandardFee, serverClock);

        // Assert
        evaluation.CanCancel.Should().BeTrue();
        evaluation.DenialReason.Should().BeNull();
        evaluation.RefundRate.Should().Be(1.0m);
        evaluation.CancellationFeeRate.Should().Be(0.0m);
        evaluation.ExpectedRefundAmount.Should().Be(StandardFee);
        evaluation.CancellationFeeAmount.Should().Be(0m);
    }

    [Fact]
    public async Task PreviewCancellationPolicy_WithValidGrantToken_ShouldReturnAccuratePreview()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new ApplicationDbContext(options);

        var patient = Patient.Create("Đặng Quốc Hưng", "0933445566", new DateOnly(1989, 7, 10), 1989);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.cancel@clinic.vn", "hash");
        var spec = Specialty.Create("CK-TAI", "Tai Mũi Họng", "tai-mui-hong");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-ENT-01", "BS. Đặng Tai Mũi Họng");
        db.Doctors.Add(doctor);

        var clinicService = Service.Create(spec.Id, "DV-TAI-01", "Nội soi Tai Mũi Họng", 30, 300_000m);
        db.Services.Add(clinicService);

        var startAt = DateTime.UtcNow.AddMinutes(90); // 90 phút trước giờ hẹn (>= 60 phút)
        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            clinicService.Id,
            clinicService.Name,
            clinicService.DurationMinutes,
            clinicService.Price,
            startAt,
            "Viêm họng hạt");
        appointment.PaidAmount = 300_000m;
        db.Appointments.Add(appointment);

        // Cấp grant token CANCEL
        var challenge = OtpChallenge.Create(appointment.Id, patient.Id, patient.PhoneNumber, OtpPurpose.CANCEL, "654321");
        challenge.Verify("654321", DateTime.UtcNow, out _);
        db.OtpChallenges.Add(challenge);

        var (grant, rawToken) = OtpVerificationGrant.Issue(challenge.Id, appointment.Id, OtpPurpose.CANCEL, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(grant);
        db.SaveChanges();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new PreviewCancellationPolicyQueryHandler(db, grantService, policyService);

        var query = new PreviewCancellationPolicyQuery(appointment.BookingCode, rawToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CanCancel.Should().BeTrue();
        result.Value.RefundRate.Should().Be(1.0m);
        result.Value.ExpectedRefundAmount.Should().Be(300_000m);
        result.Value.PolicyExplanation.Should().Contain(">= 60 phút");

        // Grant token không bị tiêu thụ khi chỉ xem preview
        var keptGrant = await db.OtpVerificationGrants.FindAsync(grant.Id);
        keptGrant!.ConsumedAt.Should().BeNull("Xem preview chính sách hủy không được tiêu thụ grant token");
    }
}
