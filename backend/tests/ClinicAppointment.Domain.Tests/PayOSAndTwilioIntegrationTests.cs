using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PayOSAndTwilioIntegrationTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SpeedSmsSender_WithUnconfiguredCredentials_ShouldFallbackGracefullyAndReturnTrue()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["SpeedSMS:AccessToken"] = "",
            ["SpeedSMS:SmsType"] = "2"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var logger = NullLogger<SpeedSmsSender>.Instance;
        var maskingService = new IdentifierMaskingService();

        var httpClient = new HttpClient();
        var speedSmsSender = new SpeedSmsSender(config, logger, maskingService, httpClient);

        // Act
        var result = await speedSmsSender.SendOtpAsync("0935191047", "123456", OtpPurpose.LOOKUP);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TwilioSmsSender_WithUnconfiguredCredentials_ShouldFallbackGracefullyAndReturnTrue()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Twilio:AccountSid"] = "",
            ["Twilio:AuthToken"] = "",
            ["Twilio:FromPhoneNumber"] = ""
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var logger = NullLogger<TwilioSmsSender>.Instance;
        var maskingService = new IdentifierMaskingService();

        var httpClient = new HttpClient();
        var twilioSender = new TwilioSmsSender(config, logger, maskingService, httpClient);

        // Act
        var result = await twilioSender.SendOtpAsync("0935191047", "123456", OtpPurpose.LOOKUP);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PayOSPaymentService_CreatePaymentLink_WhenUnconfigured_ShouldReturnMockVietQr()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = Appointment.CreateGuest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Khám Tổng Quát",
            30,
            250000m,
            DateTime.UtcNow.AddDays(1),
            "Khám bệnh");
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["PayOS:ClientId"] = "",
            ["PayOS:ApiKey"] = "",
            ["PayOS:ChecksumKey"] = ""
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var logger = NullLogger<PayOSPaymentService>.Instance;
        var httpClient = new HttpClient();
        var payOsService = new PayOSPaymentService(db, config, logger, httpClient);

        // Act
        var result = await payOsService.CreatePaymentLinkAsync(appointment.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);
        result.QrCode.Should().Contain("vietqr.io");
        result.OrderCode.Should().NotBeNull();
    }

    [Fact]
    public async Task PayOSPaymentService_ProcessWebhook_ShouldUpdatePaidAmountAndConfirmAppointment()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = Appointment.CreateGuest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Khám Nhi Khoa",
            30,
            200000m,
            DateTime.UtcNow.AddDays(1),
            "Khám bệnh");
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var logger = NullLogger<PayOSPaymentService>.Instance;
        var httpClient = new HttpClient();
        var payOsService = new PayOSPaymentService(db, config, logger, httpClient);

        var webhookPayload = JsonSerializer.Serialize(new
        {
            code = "00",
            data = new
            {
                orderCode = 987654321,
                amount = 200000m,
                description = $"TT {appointment.BookingCode}"
            }
        });

        // Act
        var processed = await payOsService.ProcessWebhookAsync(webhookPayload, null);

        // Assert
        processed.Should().BeTrue();

        var updated = await db.Appointments.FindAsync(appointment.Id);
        updated.Should().NotBeNull();
        updated!.PaidAmount.Should().Be(200000m);
        updated.Status.Should().Be(AppointmentStatus.Confirmed);

        var auditLog = await db.AuditLogs.FirstOrDefaultAsync(l => l.EntityId == appointment.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("PAYOS_WEBHOOK_PAID");
    }

    [Fact]
    public async Task PayOSPaymentService_GetPaymentStatus_ShouldAccuratelyReflectPaidState()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = Appointment.CreateGuest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Khám Mắt",
            30,
            180000m,
            DateTime.UtcNow.AddDays(1),
            "Khám mắt");
        appointment.PaidAmount = 180000m;
        appointment.Status = AppointmentStatus.Confirmed;
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var logger = NullLogger<PayOSPaymentService>.Instance;
        var httpClient = new HttpClient();
        var payOsService = new PayOSPaymentService(db, config, logger, httpClient);

        // Act
        var status = await payOsService.GetPaymentStatusAsync(appointment.Id);

        // Assert
        status.Should().NotBeNull();
        status!.IsPaid.Should().BeTrue();
        status.PaidAmount.Should().Be(180000m);
        status.ServicePrice.Should().Be(180000m);
    }
}
