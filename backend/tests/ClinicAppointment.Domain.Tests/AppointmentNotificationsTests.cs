using System.Reflection;
using System.Text;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment;
using ClinicAppointment.Application.Features.Notifications.Queries;
using ClinicAppointment.Application.Features.Notifications.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services.Notifications;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class AppointmentNotificationsTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Doctor Doctor, Patient Patient, Service Service) SeedAppointment(
        ApplicationDbContext db, 
        AppointmentStatus initialStatus = AppointmentStatus.PendingConfirmation)
    {
        var spec = Specialty.Create("CK-MAT", "Mắt", "chuyen-khoa-mat");
        db.Specialties.Add(spec);

        var docUser = User.Create("dr.minh@clinic.vn", "hash_pw");
        db.Users.Add(docUser);

        var doctor = Doctor.Create(docUser.Id, spec.Id, "DOC-EYE-01", "BS. Vũ Bình Minh");
        doctor.RoomNumber = "Phòng 204";
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-EYE-01", "Khám thị lực toàn diện", 30, 250000m);
        db.Services.Add(service);

        var patient = Patient.Create("Trần Khánh Linh", "0905123456", new DateOnly(1995, 4, 15), 1995);
        db.Patients.Add(patient);

        var apt = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            DateTime.UtcNow.AddDays(2),
            "Khám mắt định kỳ"
        );

        if (initialStatus == AppointmentStatus.Confirmed)
        {
            apt.Confirm();
        }
        else if (initialStatus == AppointmentStatus.Cancelled)
        {
            apt.Cancel("Bệnh nhân bận việc", DateTime.UtcNow);
        }

        db.Appointments.Add(apt);
        db.SaveChanges();

        return (apt, doctor, patient, service);
    }

    private (AppointmentNotificationService ApptNotificationService, DevStubSmsChannel SmsChannel, NotificationService CoreNotificationService) CreateServices(ApplicationDbContext db)
    {
        var maskingService = new IdentifierMaskingService();
        var smsChannel = new DevStubSmsChannel(NullLogger<DevStubSmsChannel>.Instance, maskingService);
        var emailChannel = new DevStubEmailChannel(NullLogger<DevStubEmailChannel>.Instance);
        var inAppChannel = new DevStubInAppChannel(NullLogger<DevStubInAppChannel>.Instance);
        var safetyGuard = new NotificationSafetyGuard();

        var dummyKey = "12345678901234567890123456789012";
        var encryptionService = new PiiEncryptionService(Encoding.UTF8.GetBytes(dummyKey));

        var coreNotificationService = new NotificationService(
            db,
            new INotificationChannel[] { smsChannel, emailChannel, inAppChannel },
            safetyGuard,
            maskingService,
            NullLogger<NotificationService>.Instance,
            encryptionService
        );

        var apptNotificationService = new AppointmentNotificationService(
            db,
            coreNotificationService,
            NullLogger<AppointmentNotificationService>.Instance
        );

        return (apptNotificationService, smsChannel, coreNotificationService);
    }

    [Fact]
    public async Task NotifyBookingCreated_EnqueuesAndDispatchesSmsNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, doc, patient, _) = SeedAppointment(db, AppointmentStatus.PendingConfirmation);
        var (service, smsChannel, _) = CreateServices(db);

        // Act
        var result = await service.NotifyBookingCreatedAsync(apt.Id, dispatchImmediately: true);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateCode.Should().Be("BOOKING_CREATED");
        result.Channel.Should().Be(NotificationChannel.Sms);
        result.Status.Should().Be(NotificationStatus.Sent);
        result.RecipientMasked.Should().Be("090***3456");
        result.ProviderMessageId.Should().StartWith("SMS_DEV_");

        smsChannel.SentMessages.Should().ContainSingle();
        var sentMsg = smsChannel.SentMessages[0];
        sentMsg.Content.Should().Contain(apt.BookingCode);
        sentMsg.Content.Should().Contain(patient.FullName);
        sentMsg.Content.Should().Contain(doc.FullName);
    }

    [Fact]
    public async Task NotifyBookingCreated_NoDuplicateEventNotification()
    {
        // Arrange: Gọi notify lần 1
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.PendingConfirmation);
        var (service, smsChannel, _) = CreateServices(db);

        var firstResult = await service.NotifyBookingCreatedAsync(apt.Id, dispatchImmediately: true);
        firstResult.Should().NotBeNull();
        smsChannel.SentMessages.Should().HaveCount(1);

        // Act: Gọi notify lần 2 cho cùng sự kiện
        var secondResult = await service.NotifyBookingCreatedAsync(apt.Id, dispatchImmediately: true);

        // Assert: Chống gửi trùng, trả về null và không phát sinh thêm tin nhắn
        secondResult.Should().BeNull();
        smsChannel.SentMessages.Should().HaveCount(1);

        var countInDb = await db.Notifications.CountAsync(n => n.AppointmentId == apt.Id && n.TemplateCode == "BOOKING_CREATED");
        countInDb.Should().Be(1);
    }

    [Fact]
    public async Task NotifyAppointmentConfirmed_EnqueuesAndDispatchesNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, doc, patient, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, smsChannel, _) = CreateServices(db);

        // Act
        var result = await service.NotifyAppointmentConfirmedAsync(apt.Id, dispatchImmediately: true);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateCode.Should().Be("APPOINTMENT_CONFIRMED");
        result.Status.Should().Be(NotificationStatus.Sent);

        smsChannel.SentMessages.Should().ContainSingle();
        var sent = smsChannel.SentMessages[0];
        sent.Content.Should().Contain("XÁC NHẬN");
        sent.Content.Should().Contain(apt.BookingCode);
        sent.Content.Should().Contain(doc.RoomNumber);
    }

    [Fact]
    public async Task NotifyAppointmentConfirmed_NoDuplicateEventNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, smsChannel, _) = CreateServices(db);

        await service.NotifyAppointmentConfirmedAsync(apt.Id, dispatchImmediately: true);

        // Act: Gọi lại lần 2
        var duplicateResult = await service.NotifyAppointmentConfirmedAsync(apt.Id, dispatchImmediately: true);

        // Assert: Không gửi trùng
        duplicateResult.Should().BeNull();
        smsChannel.SentMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task NotifyRescheduleApproved_EnqueuesAndDispatchesNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, doc, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, smsChannel, _) = CreateServices(db);

        // Act
        var result = await service.NotifyRescheduleApprovedAsync(apt.Id, dispatchImmediately: true);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateCode.Should().Be("RESCHEDULE_APPROVED");
        result.Status.Should().Be(NotificationStatus.Sent);

        smsChannel.SentMessages.Should().ContainSingle();
        smsChannel.SentMessages[0].Content.Should().Contain("DUYỆT");
        smsChannel.SentMessages[0].Content.Should().Contain(apt.BookingCode);
    }

    [Fact]
    public async Task NotifyRescheduleApproved_NoDuplicateEventNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, smsChannel, _) = CreateServices(db);

        await service.NotifyRescheduleApprovedAsync(apt.Id, dispatchImmediately: true);

        // Act
        var secondResult = await service.NotifyRescheduleApprovedAsync(apt.Id, dispatchImmediately: true);

        // Assert
        secondResult.Should().BeNull();
        smsChannel.SentMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task NotifyAppointmentCancelled_EnqueuesAndDispatchesNotification_WithRefundInfo()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Cancelled);
        var (service, smsChannel, _) = CreateServices(db);

        decimal refundAmount = 250000m;

        // Act
        var result = await service.NotifyAppointmentCancelledAsync(apt.Id, refundAmount, dispatchImmediately: true);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateCode.Should().Be("APPOINTMENT_CANCELLED");
        result.Status.Should().Be(NotificationStatus.Sent);

        smsChannel.SentMessages.Should().ContainSingle();
        smsChannel.SentMessages[0].Content.Should().Contain("HỦY");
        smsChannel.SentMessages[0].Content.Should().Contain("VND");
        smsChannel.SentMessages[0].Content.Should().MatchRegex(@"250[.,]000\s*VND");
    }

    [Fact]
    public async Task NotifyAppointmentCancelled_NoDuplicateEventNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Cancelled);
        var (service, smsChannel, _) = CreateServices(db);

        await service.NotifyAppointmentCancelledAsync(apt.Id, 100000m, dispatchImmediately: true);

        // Act
        var secondResult = await service.NotifyAppointmentCancelledAsync(apt.Id, 100000m, dispatchImmediately: true);

        // Assert
        secondResult.Should().BeNull();
        smsChannel.SentMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task NotifyAppointmentReminder_EnqueuesAndDispatchesNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, doc, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, smsChannel, _) = CreateServices(db);

        // Act
        var result = await service.NotifyAppointmentReminderAsync(apt.Id, dispatchImmediately: true);

        // Assert
        result.Should().NotBeNull();
        result!.TemplateCode.Should().Be("APPOINTMENT_REMINDER");
        result.Status.Should().Be(NotificationStatus.Sent);

        smsChannel.SentMessages.Should().ContainSingle();
        smsChannel.SentMessages[0].Content.Should().Contain("Nhắc hẹn");
        smsChannel.SentMessages[0].Content.Should().Contain("15 phút");
    }

    [Fact]
    public async Task MinimalContent_AllTemplates_DoNotContainDiagnosisOrSensitivePii()
    {
        // Arrange: Tạo tất cả 5 loại thông báo
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, _, _) = CreateServices(db);

        var guard = new NotificationSafetyGuard();

        // Act: Kích hoạt cả 5 sự kiện
        await service.NotifyBookingCreatedAsync(apt.Id, dispatchImmediately: false);
        
        var apt2 = Appointment.CreateGuest(apt.DoctorId, apt.PatientId, apt.ServiceId, apt.ServiceNameSnapshot, 30, 200000m, DateTime.UtcNow.AddDays(3), "Khám");
        db.Appointments.Add(apt2);
        await db.SaveChangesAsync();

        await service.NotifyAppointmentConfirmedAsync(apt2.Id, dispatchImmediately: false);
        await service.NotifyRescheduleApprovedAsync(apt2.Id, dispatchImmediately: false);
        await service.NotifyAppointmentCancelledAsync(apt2.Id, 150000m, dispatchImmediately: false);
        await service.NotifyAppointmentReminderAsync(apt2.Id, dispatchImmediately: false);

        var allNotifications = await db.Notifications.ToListAsync();
        allNotifications.Should().HaveCount(5);

        // Assert: Quét toàn bộ thông điệp qua NotificationSafetyGuard
        foreach (var notif in allNotifications)
        {
            notif.RecipientMasked.Should().NotBeNull();
            notif.RecipientMasked.Should().NotContain("0905123456"); // Không lộ sđt thô

            // Đảm bảo không chứa từ khóa y tế nhạy cảm
            var msg = new NotificationMessage(
                Recipient: "0905123456",
                Channel: notif.Channel,
                TemplateCode: notif.TemplateCode,
                Content: notif.PayloadJson ?? string.Empty
            );

            Action act = () => guard.ValidateMessageSafety(msg);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public async Task DeliveryStatus_GetNotificationsByAppointmentId_ReturnsAllEventStatuses()
    {
        // Arrange: Tạo 2 thông báo cho cùng một ca khám (1 Sent, 1 Failed do giả lập lỗi mạng)
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, smsChannel, _) = CreateServices(db);

        // Thông báo 1: Gửi thành công
        await service.NotifyBookingCreatedAsync(apt.Id, dispatchImmediately: true);

        // Thông báo 2: Giả lập lỗi nhà mạng khi gửi xác nhận
        smsChannel.SimulateFailure = true;
        smsChannel.SimulatedErrorMessage = "Cổng SMS nhà mạng phản hồi 503 Service Unavailable (Simulated).";

        await service.NotifyAppointmentConfirmedAsync(apt.Id, dispatchImmediately: true);

        // Act: Lấy danh sách delivery status
        var deliveryStatuses = await service.GetNotificationsByAppointmentIdAsync(apt.Id);

        // Assert
        deliveryStatuses.Should().HaveCount(2);

        var sentNotification = deliveryStatuses.First(n => n.TemplateCode == "BOOKING_CREATED");
        sentNotification.Status.Should().Be(NotificationStatus.Sent);
        sentNotification.SentAt.Should().NotBeNull();
        sentNotification.FailureReason.Should().BeNull();

        var failedNotification = deliveryStatuses.First(n => n.TemplateCode == "APPOINTMENT_CONFIRMED");
        failedNotification.Status.Should().Be(NotificationStatus.Failed);
        failedNotification.FailureReason.Should().Contain("503 Service Unavailable");
        failedNotification.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAppointmentNotificationsQuery_ByStaff_ReturnsDeliveryStatusList()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, _, _) = CreateServices(db);

        await service.NotifyBookingCreatedAsync(apt.Id, dispatchImmediately: true);

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "receptionist@clinic.vn",
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetAppointmentNotificationsQueryHandler(db, service, staffUser);
        var query = new GetAppointmentNotificationsQuery(apt.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().ContainSingle();
        result.Value![0].TemplateCode.Should().Be("BOOKING_CREATED");
        result.Value[0].Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task GetAppointmentNotificationsQuery_ByUnassignedDoctor_ThrowsForbidden()
    {
        // Arrange: Bác sĩ khác cố ý xem delivery status của ca khám không được phân công
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, _, _) = CreateServices(db);

        var otherDoctorUser = User.Create("other.doctor@clinic.vn", "pw");
        db.Users.Add(otherDoctorUser);
        var otherDoctor = Doctor.Create(otherDoctorUser.Id, apt.Doctor!.SpecialtyId, "DOC-OTHER", "BS. Khác");
        db.Doctors.Add(otherDoctor);
        await db.SaveChangesAsync();

        var unassignedDoctorUser = new TestCurrentUserService
        {
            UserId = otherDoctorUser.Id,
            Email = otherDoctorUser.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetAppointmentNotificationsQueryHandler(db, service, unassignedDoctorUser);
        var query = new GetAppointmentNotificationsQuery(apt.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAppointmentNotificationsQuery_ByAnonymous_ThrowsUnauthorized()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.Confirmed);
        var (service, _, _) = CreateServices(db);

        var anonymousUser = new TestCurrentUserService
        {
            UserId = null,
            IsAuthenticated = false,
            Roles = new List<string>()
        };

        var handler = new GetAppointmentNotificationsQueryHandler(db, service, anonymousUser);
        var query = new GetAppointmentNotificationsQuery(apt.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task ConfirmAppointmentCommandHandler_AutomaticallyTriggersNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (apt, _, _, _) = SeedAppointment(db, AppointmentStatus.PendingConfirmation);
        var (service, smsChannel, _) = CreateServices(db);

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "reception@clinic.vn",
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, staffUser, service);
        var command = new ConfirmAppointmentCommand(apt.Id, "Đã gọi xác nhận");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Kiểm tra thông báo APPOINTMENT_CONFIRMED tự động được tạo và gửi
        smsChannel.SentMessages.Should().ContainSingle();
        smsChannel.SentMessages[0].TemplateCode.Should().Be("APPOINTMENT_CONFIRMED");

        var notifInDb = await db.Notifications.FirstOrDefaultAsync(n => n.AppointmentId == apt.Id && n.TemplateCode == "APPOINTMENT_CONFIRMED");
        notifInDb.Should().NotBeNull();
        notifInDb!.Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public void AppointmentsController_NotificationsEndpoint_ProtectedWithStaffOrDoctorPolicy()
    {
        // Arrange & Act
        var controllerType = typeof(AppointmentsController);
        var method = controllerType.GetMethod(nameof(AppointmentsController.GetAppointmentNotifications));

        // Assert
        method.Should().NotBeNull();
        var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
        authAttr.Should().NotBeNull();
        authAttr!.Policy.Should().Be("StaffOrDoctor");
    }
}
