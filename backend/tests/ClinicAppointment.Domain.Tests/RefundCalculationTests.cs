using ClinicAppointment.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class RefundCalculationTests
{
    private const decimal StandardFee = 200_000m;

    [Fact]
    public void Calculate_WhenCancelled61MinutesBefore_ShouldRefund100Percent()
    {
        // Arrange
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-61);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(61);
        result.CancellationFeeRate.Should().Be(0.0m);
        result.CancellationFeeAmount.Should().Be(0.0m);
        result.ExpectedRefundAmount.Should().Be(StandardFee);
    }

    [Fact]
    public void Calculate_WhenCancelledExactly60MinutesBefore_ShouldRefund100Percent()
    {
        // Arrange: Đúng mốc 60 phút thuộc nhóm hoàn 100% theo SRS 1.2
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-60);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(60);
        result.CancellationFeeRate.Should().Be(0.0m);
        result.CancellationFeeAmount.Should().Be(0.0m);
        result.ExpectedRefundAmount.Should().Be(StandardFee);
    }

    [Fact]
    public void Calculate_WhenCancelled59MinutesBefore_ShouldRefund50Percent()
    {
        // Arrange: Dưới 60 phút nhưng trước giờ hẹn hoàn 50%, phí hủy 50%
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-59);

        // Act
        var result = RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);

        // Assert
        result.MinutesBeforeAppointment.Should().Be(59);
        result.CancellationFeeRate.Should().Be(0.50m);
        result.CancellationFeeAmount.Should().Be(100_000m);
        result.ExpectedRefundAmount.Should().Be(100_000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(-60)]
    public void Calculate_WhenCancelledAtOrAfterStart_ShouldThrowException(int minutesOffset)
    {
        // Arrange
        var appointmentStart = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var cancellationTime = appointmentStart.AddMinutes(-minutesOffset);

        // Act & Assert
        var act = () => RefundCalculator.Calculate(appointmentStart, cancellationTime, StandardFee);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ*");
    }
}

