using System.Text;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class NotificationAbstractionTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (DevStubSmsChannel SmsChannel, DevStubEmailChannel EmailChannel, DevStubInAppChannel InAppChannel, NotificationSafetyGuard SafetyGuard, PiiEncryptionService EncryptionService, IdentifierMaskingService MaskingService) CreateTestDependencies()
    {
        var maskingService = new IdentifierMaskingService();
        var smsChannel = new DevStubSmsChannel(NullLogger<DevStubSmsChannel>.Instance, maskingService);
        var emailChannel = new DevStubEmailChannel(NullLogger<DevStubEmailChannel>.Instance);
        var inAppChannel = new DevStubInAppChannel(NullLogger<DevStubInAppChannel>.Instance);
        var safetyGuard = new NotificationSafetyGuard();

        // 32-byte key AES-256
        var dummyKey = "12345678901234567890123456789012";
        var encryptionService = new PiiEncryptionService(Encoding.UTF8.GetBytes(dummyKey));

        return (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService);
    }

    [Fact]
    public async Task DevStubSmsChannel_SendsSuccessfully_WithoutRealProvider()
    {
        // Arrange
        var (smsChannel, _, _, _, _, _) = CreateTestDependencies();
        var message = new NotificationMessage(
            Recipient: "0912345678",
            Channel: NotificationChannel.Sms,
            TemplateCode: "APPOINTMENT_CONFIRMED",
            Content: "Lịch hẹn APT-20261114-001 của bạn đã được xác nhận thành công."
        );

        // Act
        var result = await smsChannel.SendAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProviderMessageId.Should().StartWith("SMS_DEV_");
        result.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        smsChannel.SentMessages.Should().ContainSingle();
        smsChannel.SentMessages[0].Recipient.Should().Be("0912345678");
    }

    [Fact]
    public async Task DevStubEmailChannel_SendsSuccessfully_WithoutSmtp()
    {
        // Arrange
        var (_, emailChannel, _, _, _, _) = CreateTestDependencies();
        var message = new NotificationMessage(
            Recipient: "patient@example.com",
            Channel: NotificationChannel.Email,
            TemplateCode: "BOOKING_CONFIRMED",
            Subject: "Xác nhận đặt lịch khám",
            Content: "Quý khách đã đặt lịch khám thành công tại Clinic."
        );

        // Act
        var result = await emailChannel.SendAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProviderMessageId.Should().StartWith("EMAIL_DEV_");
        emailChannel.SentMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task DevStubInAppChannel_SendsSuccessfully()
    {
        // Arrange
        var (_, _, inAppChannel, _, _, _) = CreateTestDependencies();
        var message = new NotificationMessage(
            Recipient: "user-guid-1234",
            Channel: NotificationChannel.InApp,
            TemplateCode: "REMINDER_30M",
            Content: "Còn 30 phút nữa tới ca khám của bạn."
        );

        // Act
        var result = await inAppChannel.SendAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProviderMessageId.Should().StartWith("INAPP_DEV_");
        inAppChannel.SentMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task NotificationService_EnqueueNotification_CreatesPendingRecordWithMaskedRecipient()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var command = new EnqueueNotificationCommand(
            Recipient: "0908123456",
            Channel: NotificationChannel.Sms,
            TemplateCode: "BOOKING_CREATED",
            Content: "Lịch hẹn APT-20261114-111 đã được tiếp nhận.",
            MaxRetries: 3
        );

        // Act
        var result = await service.EnqueueNotificationAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(NotificationStatus.Pending);
        result.Value.RecipientMasked.Should().Be("090***3456");
        result.Value.RetryCount.Should().Be(0);

        var savedInDb = await db.Notifications.FindAsync(result.Value.Id);
        savedInDb.Should().NotBeNull();
        savedInDb!.Status.Should().Be(NotificationStatus.Pending);
        savedInDb.RecipientCiphertext.Should().NotBeNull();
        savedInDb.RecipientMasked.Should().Be("090***3456");
    }

    [Fact]
    public async Task DispatchNotification_WhenChannelSucceeds_UpdatesStatusToSent()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var enqueueResult = await service.EnqueueNotificationAsync(new EnqueueNotificationCommand(
            Recipient: "0987654321",
            Channel: NotificationChannel.Sms,
            TemplateCode: "TEST_SUCCESS",
            Content: "Nội dung an toàn không chứa PII hoặc chẩn đoán."
        ));

        var notificationId = enqueueResult.Value!.Id;

        // Act
        var sendResult = await service.DispatchNotificationAsync(notificationId);

        // Assert
        sendResult.IsSuccess.Should().BeTrue();
        sendResult.ProviderMessageId.Should().StartWith("SMS_DEV_");

        var updated = await db.Notifications.FindAsync(notificationId);
        updated!.Status.Should().Be(NotificationStatus.Sent);
        updated.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.FailureReason.Should().BeNull();
        updated.ProviderMessageId.Should().Be(sendResult.ProviderMessageId);
    }

    [Fact]
    public async Task DispatchNotification_WhenChannelFails_RecordsDeliveryFailure_AndIncrementsRetryCount()
    {
        // Arrange: Cấu hình SMS channel giả lập lỗi (Simulate failure)
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        smsChannel.SimulateFailure = true;
        smsChannel.SimulatedErrorMessage = "Cổng kết nối SMS nhà mạng bị gián đoạn (Simulated error).";

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var enqueueResult = await service.EnqueueNotificationAsync(new EnqueueNotificationCommand(
            Recipient: "0901234567",
            Channel: NotificationChannel.Sms,
            TemplateCode: "TEST_FAIL",
            Content: "Tin nhắn test lỗi gửi."
        ));

        var notificationId = enqueueResult.Value!.Id;

        // Act
        var sendResult = await service.DispatchNotificationAsync(notificationId);

        // Assert
        sendResult.IsSuccess.Should().BeFalse();
        sendResult.ErrorMessage.Should().Contain("Cổng kết nối SMS nhà mạng bị gián đoạn");

        var updated = await db.Notifications.FindAsync(notificationId);
        updated!.Status.Should().Be(NotificationStatus.Failed);
        updated.RetryCount.Should().Be(1);
        updated.FailureReason.Should().Contain("Cổng kết nối SMS");
        updated.SentAt.Should().BeNull();
    }

    [Fact]
    public async Task RetryNotification_WhenFailedAndWithinLimit_ResetsAndRetriesSuccessfully()
    {
        // Arrange: Tin nhắn bị thất bại trước đó
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        smsChannel.SimulateFailure = true;

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var enqueueResult = await service.EnqueueNotificationAsync(new EnqueueNotificationCommand(
            Recipient: "0901234567",
            Channel: NotificationChannel.Sms,
            TemplateCode: "TEST_RETRY",
            Content: "Tin nhắn test thử lại.",
            MaxRetries: 3
        ));

        var notificationId = enqueueResult.Value!.Id;
        await service.DispatchNotificationAsync(notificationId);

        var failedNotification = await db.Notifications.FindAsync(notificationId);
        failedNotification!.Status.Should().Be(NotificationStatus.Failed);
        failedNotification.RetryCount.Should().Be(1);

        // Khắc phục sự cố: Nhà mạng hoạt động bình thường trở lại
        smsChannel.SimulateFailure = false;

        // Act: Thử lại
        var retryResult = await service.RetryNotificationAsync(notificationId);

        // Assert
        retryResult.IsSuccess.Should().BeTrue();
        retryResult.Value!.Status.Should().Be(NotificationStatus.Sent);
        retryResult.Value.SentAt.Should().NotBeNull();

        var reloaded = await db.Notifications.FindAsync(notificationId);
        reloaded!.Status.Should().Be(NotificationStatus.Sent);
        reloaded.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task RetryNotification_WhenExceedsMaxRetries_IsBlocked()
    {
        // Arrange: Tin nhắn đã chạm ngưỡng MaxRetries (ví dụ 3/3)
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var notification = Notification.Create(
            channel: NotificationChannel.Sms,
            templateCode: "MAX_RETRY_EXCEEDED",
            recipientMasked: "090***4567",
            maxRetries: 2
        );
        notification.MarkFailed("Lỗi lần 1");
        notification.MarkFailed("Lỗi lần 2"); // RetryCount = 2, MaxRetries = 2

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Act
        var retryResult = await service.RetryNotificationAsync(notification.Id);

        // Assert
        retryResult.IsSuccess.Should().BeFalse();
        retryResult.StatusCode.Should().Be(400);
        retryResult.Error.Should().Contain("không thể thử lại");
    }

    [Theory]
    [InlineData("Kết quả chẩn đoán: Viêm loét dạ dày tá tràng nặng.")]
    [InlineData("Bác sĩ ghi nhận triệu chứng: Sốt cao, co giật, khó thở kéo dài.")]
    [InlineData("Bệnh nhân có kết luận diagnosis: Ung thư biểu mô tuyến.")]
    [InlineData("Bệnh án: Bệnh nhân dương tính với viêm gan b.")]
    public async Task NotificationSafetyGuard_RejectsMessageWithMedicalDiagnosisOrSymptoms(string sensitiveContent)
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var command = new EnqueueNotificationCommand(
            Recipient: "0911222333",
            Channel: NotificationChannel.Sms,
            TemplateCode: "MEDICAL_ALERT",
            Content: sensitiveContent
        );

        // Act
        var result = await service.EnqueueNotificationAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("vi phạm chính sách bảo mật y tế");

        var count = await db.Notifications.CountAsync();
        count.Should().Be(0); // Tuyệt đối không lưu vào CSDL
    }

    [Fact]
    public async Task NotificationSafetyGuard_RejectsMessageWithRawNationalId()
    {
        // Arrange: Cố tình đưa số CCCD 12 số thô vào tin nhắn
        var (_, _, _, safetyGuard, _, _) = CreateTestDependencies();

        var message = new NotificationMessage(
            Recipient: "0911222333",
            Channel: NotificationChannel.Sms,
            TemplateCode: "ID_LEAK",
            Content: "Quý khách có CCCD: 079198001234 vui lòng mang theo khi đến khám."
        );

        // Act & Assert
        Action act = () => safetyGuard.ValidateMessageSafety(message);
        act.Should().Throw<NotificationSafetyViolationException>()
            .WithMessage("*định danh cá nhân/CCCD*");
    }

    [Fact]
    public async Task NotificationSafetyGuard_RejectsMessageWithRawPassword()
    {
        // Arrange: Cố tình đưa mật khẩu thô vào tin nhắn
        var (_, _, _, safetyGuard, _, _) = CreateTestDependencies();

        var message = new NotificationMessage(
            Recipient: "patient@example.com",
            Channel: NotificationChannel.Email,
            TemplateCode: "PW_LEAK",
            Content: "Tài khoản của bạn đã được tạo, mật khẩu: Password123!"
        );

        // Act & Assert
        Action act = () => safetyGuard.ValidateMessageSafety(message);
        act.Should().Throw<NotificationSafetyViolationException>()
            .WithMessage("*mật khẩu*");
    }

    [Fact]
    public async Task NotificationSafetyGuard_RejectsPayloadWithSensitiveMedicalFields()
    {
        // Arrange: Cố tình gắn Diagnosis hoặc Symptoms vào dictionary Payload
        var (_, _, _, safetyGuard, _, _) = CreateTestDependencies();

        var payload = new Dictionary<string, object>
        {
            ["Diagnosis"] = "Viêm đường hô hấp trên",
            ["BookingCode"] = "APT-001"
        };

        var message = new NotificationMessage(
            Recipient: "0911222333",
            Channel: NotificationChannel.Sms,
            TemplateCode: "TEST_PAYLOAD",
            Content: "Nội dung thông báo thông thường",
            Payload: payload
        );

        // Act & Assert
        Action act = () => safetyGuard.ValidateMessageSafety(message);
        act.Should().Throw<NotificationSafetyViolationException>()
            .WithMessage("*trường y tế nhạy cảm*Diagnosis*");
    }

    [Fact]
    public void NotificationSafetyGuard_AllowsSafeAppointmentMessage()
    {
        // Arrange: Tin nhắn lịch hẹn an toàn (chỉ chứa mã lịch hẹn, bác sĩ, ngày giờ, địa chỉ phòng khám)
        var (_, _, _, safetyGuard, _, _) = CreateTestDependencies();

        var message = new NotificationMessage(
            Recipient: "0911222333",
            Channel: NotificationChannel.Sms,
            TemplateCode: "APPOINTMENT_REMINDER",
            Subject: "Nhắc lịch khám bệnh",
            Content: "Nhắc hẹn: Quý khách có lịch khám tại Phòng khám đa khoa lúc 09:00 ngày 20/11/2026. Mã đặt hẹn: APT-20261120-123456. BS. Lê Quang phụ trách."
        );

        // Act & Assert
        Action act = () => safetyGuard.ValidateMessageSafety(message);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DispatchPendingNotifications_ProcessesBatchSuccessfully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        await service.EnqueueNotificationAsync(new EnqueueNotificationCommand("0901111111", NotificationChannel.Sms, "T1", "Tin 1"));
        await service.EnqueueNotificationAsync(new EnqueueNotificationCommand("patient2@clinic.vn", NotificationChannel.Email, "T2", "Tin 2"));
        await service.EnqueueNotificationAsync(new EnqueueNotificationCommand("user3", NotificationChannel.InApp, "T3", "Tin 3"));

        var pendingCountBefore = await db.Notifications.CountAsync(n => n.Status == NotificationStatus.Pending);
        pendingCountBefore.Should().Be(3);

        // Act: Xử lý hàng đợi
        var processed = await service.DispatchPendingNotificationsAsync(batchSize: 10);

        // Assert
        processed.Should().Be(3);
        var pendingCountAfter = await db.Notifications.CountAsync(n => n.Status == NotificationStatus.Pending);
        pendingCountAfter.Should().Be(0);

        var sentCount = await db.Notifications.CountAsync(n => n.Status == NotificationStatus.Sent);
        sentCount.Should().Be(3);
    }

    [Fact]
    public async Task ChannelDecoupled_NotificationServiceDispatchesBasedOnChannel()
    {
        // Arrange: Đảm bảo tính decoupled, SMS chỉ đi qua SmsChannel, Email qua EmailChannel
        using var db = CreateInMemoryDbContext();
        var (smsChannel, emailChannel, inAppChannel, safetyGuard, encryptionService, maskingService) = CreateTestDependencies();

        var service = new NotificationService(
            db, 
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var smsRes = await service.EnqueueNotificationAsync(new EnqueueNotificationCommand("0901111111", NotificationChannel.Sms, "SMS_TEMP", "Tin SMS"));
        var emailRes = await service.EnqueueNotificationAsync(new EnqueueNotificationCommand("mail@clinic.vn", NotificationChannel.Email, "EMAIL_TEMP", "Tin Email"));

        await service.DispatchNotificationAsync(smsRes.Value!.Id);
        await service.DispatchNotificationAsync(emailRes.Value!.Id);

        // Assert
        smsChannel.SentMessages.Should().ContainSingle();
        smsChannel.SentMessages[0].Channel.Should().Be(NotificationChannel.Sms);

        emailChannel.SentMessages.Should().ContainSingle();
        emailChannel.SentMessages[0].Channel.Should().Be(NotificationChannel.Email);
    }
}
