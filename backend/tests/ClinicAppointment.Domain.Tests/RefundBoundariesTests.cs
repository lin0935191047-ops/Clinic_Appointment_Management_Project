using System;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

/// <summary>
/// FEAT-049: Kiểm thử chuyên sâu ranh giới hoàn tiền (Refund boundaries)
/// Tuân thủ nghiêm ngặt quy tắc bất biến trong docs/BUSINESS_RULES.md, docs/SPEC.md và AGENTS.md:
/// - >= 60 phút (gồm đúng 60 phút): hoàn 100%, phí hủy 0%
/// - < 60 phút nhưng trước giờ hẹn: hoàn 50%, phí hủy 50%
/// - Tại hoặc sau giờ hẹn (<= 0 phút): từ chối tự hủy, CanCancel = false
/// - Làm tròn số nguyên VND chuẩn, không làm mất đồng nào (Refund + Fee = PaidAmount)
/// </summary>
public class RefundBoundariesTests
{
    private const decimal StandardPrice = 300_000m;
    private readonly CancellationPolicyService _policyService = new();

    #region 1. Mốc Ranh Giới 60 Phút Bất Biến

    [Fact]
    public void Boundary_Exactly60Minutes_Returns100PercentRefund_And0PercentFee()
    {
        // Arrange: Đúng mốc 60 phút (60.00m) thuộc nhóm hoàn 100% (BR invariant)
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddMinutes(-60);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(60);
        result.RefundRate.Should().Be(1.0m);
        result.CancellationFeeRate.Should().Be(0.0m);
        result.ExpectedRefundAmount.Should().Be(StandardPrice);
        result.CancellationFeeAmount.Should().Be(0m);
        (result.ExpectedRefundAmount + result.CancellationFeeAmount).Should().Be(StandardPrice);
    }

    [Fact]
    public void Boundary_60MinutesAnd1Second_Returns100PercentRefund()
    {
        // Arrange: 60 phút 01 giây (trên 60 phút) -> Hoàn 100%
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddMinutes(-60).AddSeconds(-1);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);

        // Assert
        result.MinutesBeforeAppointment.Should().BeGreaterThan(60);
        result.RefundRate.Should().Be(1.0m);
        result.CancellationFeeRate.Should().Be(0.0m);
        result.ExpectedRefundAmount.Should().Be(StandardPrice);
        result.CancellationFeeAmount.Should().Be(0m);
    }

    [Fact]
    public void Boundary_59MinutesAnd59Seconds_Returns50PercentRefund_And50PercentFee()
    {
        // Arrange: 59 phút 59 giây (dưới 60 phút) -> Hoàn 50%, phí hủy 50%
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddMinutes(-59).AddSeconds(-59);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);

        // Assert
        result.MinutesBeforeAppointment.Should().BeLessThan(60);
        result.RefundRate.Should().Be(0.50m);
        result.CancellationFeeRate.Should().Be(0.50m);
        result.ExpectedRefundAmount.Should().Be(150_000m);
        result.CancellationFeeAmount.Should().Be(150_000m);
        (result.ExpectedRefundAmount + result.CancellationFeeAmount).Should().Be(StandardPrice);
    }

    [Fact]
    public void Boundary_1MinuteBeforeAppointment_Returns50PercentRefund()
    {
        // Arrange: 1 phút trước giờ hẹn (vẫn trước giờ hẹn) -> Hoàn 50%
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddMinutes(-1);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(1);
        result.RefundRate.Should().Be(0.50m);
        result.CancellationFeeRate.Should().Be(0.50m);
        result.ExpectedRefundAmount.Should().Be(150_000m);
        result.CancellationFeeAmount.Should().Be(150_000m);
    }

    [Fact]
    public void Boundary_10SecondsBeforeAppointment_Returns50PercentRefund()
    {
        // Arrange: 10 giây trước giờ hẹn (vẫn hợp lệ trước StartAt)
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddSeconds(-10);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);

        // Assert
        result.RefundRate.Should().Be(0.50m);
        result.CancellationFeeRate.Should().Be(0.50m);
        result.ExpectedRefundAmount.Should().Be(150_000m);
    }

    #endregion

    #region 2. Ranh Giới Tại Và Sau Giờ Hẹn (<= 0 phút)

    [Fact]
    public void Boundary_ExactlyAtAppointmentStart_ThrowsInvalidOperationException()
    {
        // Arrange: Đúng thời điểm giờ hẹn (0 giây) -> Không được tự hủy
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc;

        // Act & Assert
        var act = () => RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ*");
    }

    [Fact]
    public void Boundary_1SecondAfterAppointmentStart_ThrowsInvalidOperationException()
    {
        // Arrange: 1 giây sau giờ hẹn -> Từ chối
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddSeconds(1);

        // Act & Assert
        var act = () => RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, StandardPrice);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ*");
    }

    [Fact]
    public void Boundary_PastAppointment_PolicyServiceReturnsCanCancelFalse()
    {
        // Arrange: Kiểm tra qua CancellationPolicyService (tầng Application)
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var serverClockUtc = startAtUtc.AddMinutes(15); // 15 phút sau giờ hẹn

        // Act
        var evaluation = _policyService.Evaluate(startAtUtc, StandardPrice, serverClockUtc);

        // Assert
        evaluation.CanCancel.Should().BeFalse();
        evaluation.DenialReason.Should().NotBeNullOrEmpty();
        evaluation.DenialReason.Should().Contain("quá khứ");
        evaluation.ExpectedRefundAmount.Should().Be(0m);
        evaluation.CancellationFeeAmount.Should().Be(StandardPrice);
        evaluation.CancellationFeeRate.Should().Be(1.0m);
    }

    #endregion

    #region 3. Bảo Toàn Số Tiền & Làm Tròn Số Nguyên VND Chuẩn

    [Theory]
    [InlineData(155_555)]  // 155_555 * 0.5 = 77_777.5 -> Fee: 77_778, Refund: 77_777
    [InlineData(333_333)]  // 333_333 * 0.5 = 166_666.5 -> Fee: 166_667, Refund: 166_666
    [InlineData(999_999)]  // 999_999 * 0.5 = 499_999.5 -> Fee: 500_000, Refund: 499_999
    [InlineData(1)]        // 1 * 0.5 = 0.5 -> Fee: 1, Refund: 0
    public void OddVndAmounts_PreserveTotalSumExactly_WithoutLosingFraction(decimal oddAmount)
    {
        // Arrange: Hủy dưới 60 phút (50% refund, 50% fee)
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddMinutes(-30);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, oddAmount);

        // Assert
        result.CancellationFeeAmount.Should().Be(Math.Round(oddAmount * 0.50m, 0, MidpointRounding.AwayFromZero));
        result.ExpectedRefundAmount.Should().Be(oddAmount - result.CancellationFeeAmount);
        (result.CancellationFeeAmount + result.ExpectedRefundAmount).Should().Be(oddAmount, "Tổng tiền hoàn và phí hủy phải bảo toàn 100% không mất một đồng");
    }

    [Fact]
    public void ZeroPaidAmount_ReturnsZeroRefundAndZeroFee()
    {
        // Arrange: Ca khám miễn phí hoặc chưa thanh toán
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);
        var cancellationTimeUtc = startAtUtc.AddMinutes(-40);

        // Act
        var result = RefundCalculator.Calculate(startAtUtc, cancellationTimeUtc, paidServiceAmount: 0m);

        // Assert
        result.ExpectedRefundAmount.Should().Be(0m);
        result.CancellationFeeAmount.Should().Be(0m);
    }

    [Fact]
    public void HighValueService_CalculatesAccurately()
    {
        // Arrange: Dịch vụ phẫu thuật/kỹ thuật cao giá 15.000.000 VND
        decimal highPrice = 15_000_000m;
        var startAtUtc = new DateTime(2026, 12, 1, 14, 0, 0, DateTimeKind.Utc);

        // Act (hủy trước 60 phút)
        var result100 = RefundCalculator.Calculate(startAtUtc, startAtUtc.AddMinutes(-120), highPrice);
        // Act (hủy dưới 60 phút)
        var result50 = RefundCalculator.Calculate(startAtUtc, startAtUtc.AddMinutes(-30), highPrice);

        // Assert
        result100.ExpectedRefundAmount.Should().Be(15_000_000m);
        result100.CancellationFeeAmount.Should().Be(0m);

        result50.ExpectedRefundAmount.Should().Be(7_500_000m);
        result50.CancellationFeeAmount.Should().Be(7_500_000m);
    }

    #endregion
}
