using System.Reflection;
using System.Text.Json;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class AppointmentConfirmationTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private Appointment SeedAppointment(
        ApplicationDbContext db, 
        AppointmentStatus status = AppointmentStatus.PendingConfirmation)
    {
        var patient = Patient.Create("Lê Thị Thảo", "0988776655", new DateOnly(1992, 4, 15), 1992);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.test@clinic.vn", "hash");
        var spec = Specialty.Create("CK-MAT", "Mắt", "chuyen-khoa-mat");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-EYE-01", "BS. Lê Nhãn Khoa");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-MAT-01", "Đo thị lực & Khám đáy mắt", 30, 200000m);
        db.Services.Add(service);

        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            DateTime.UtcNow.AddDays(1),
            "Kiểm tra cận thị");

        if (status == AppointmentStatus.Confirmed)
        {
            appointment.Confirm();
        }
        else if (status == AppointmentStatus.Cancelled)
        {
            appointment.Cancel("Người bệnh bận việc đột xuất", DateTime.UtcNow);
        }
        else if (status == AppointmentStatus.Completed)
        {
            appointment.Status = AppointmentStatus.Completed;
        }

        db.Appointments.Add(appointment);
        db.SaveChanges();

        return appointment;
    }

    [Fact]
    public async Task Confirm_PendingConfirmation_ByReceptionist_ShouldSucceed_AndRecordAuditLog()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.PendingConfirmation);
        var receptionistUserId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            UserId = receptionistUserId,
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id, Note: "Đã liên hệ bệnh nhân qua điện thoại để xác nhận thông tin");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.AppointmentId.Should().Be(appointment.Id);
        result.Value.Status.Should().Be(AppointmentStatus.Confirmed);
        result.Value.Note.Should().Be("Đã liên hệ bệnh nhân qua điện thoại để xác nhận thông tin");

        // DB state verification
        var updated = await db.Appointments.FindAsync(appointment.Id);
        updated!.Status.Should().Be(AppointmentStatus.Confirmed);
        updated.UpdatedAt.Should().NotBeNull();

        // Audit Log verification
        var auditLogs = await db.AuditLogs.Where(a => a.EntityId == appointment.Id).ToListAsync();
        auditLogs.Should().HaveCount(1);
        var log = auditLogs.First();
        log.Action.Should().Be("CONFIRM_APPOINTMENT");
        log.ActorType.Should().Be("RECEPTIONIST");
        log.ActorUserId.Should().Be(receptionistUserId);
        log.EntityType.Should().Be("Appointment");
        log.MetadataRedacted.Should().NotBeNullOrEmpty();
        log.MetadataRedacted.Should().Contain(appointment.BookingCode);

        using var jsonDoc = JsonDocument.Parse(log.MetadataRedacted!);
        jsonDoc.RootElement.GetProperty("Note").GetString().Should().Be("Đã liên hệ bệnh nhân qua điện thoại để xác nhận thông tin");
        jsonDoc.RootElement.GetProperty("BookingCode").GetString().Should().Be(appointment.BookingCode);
    }

    [Fact]
    public async Task Confirm_PendingConfirmation_ByAdmin_ShouldSucceed_AndRecordAdminAuditLog()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.PendingConfirmation);
        var adminUserId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            UserId = adminUserId,
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id, Note: "Admin xác nhận thủ công");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == appointment.Id);
        log.Should().NotBeNull();
        log!.ActorType.Should().Be("ADMIN");
        log.ActorUserId.Should().Be(adminUserId);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmed_ShouldFailWith400()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Confirmed);

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Không thể xác nhận lịch hẹn ở trạng thái Confirmed");
        result.Error.Should().Contain("PendingConfirmation");

        // Không tạo audit log mới
        var auditLogs = await db.AuditLogs.Where(a => a.EntityId == appointment.Id).ToListAsync();
        auditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task Confirm_CancelledAppointment_ShouldFailWith400()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Cancelled);

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Cancelled");
    }

    [Fact]
    public async Task Confirm_CompletedAppointment_ShouldFailWith400()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Completed);

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Completed");
    }

    [Fact]
    public async Task Confirm_NonExistentAppointment_ShouldFailWith404()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Contain("Không tìm thấy");
    }

    [Fact]
    public async Task Confirm_ByNonStaffUser_ShouldFailWith403()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.PendingConfirmation);

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode }, // Role PATIENT (không phải STAFF)
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("không có quyền");

        // Lịch khám không bị đổi trạng thái
        var currentAppointment = await db.Appointments.FindAsync(appointment.Id);
        currentAppointment!.Status.Should().Be(AppointmentStatus.PendingConfirmation);

        // Không có audit log nào được tạo
        var auditLogs = await db.AuditLogs.Where(a => a.EntityId == appointment.Id).ToListAsync();
        auditLogs.Should().BeEmpty();
    }

    [Fact]
    public void Controller_Endpoints_ShouldHave_AuthorizeStaffOnlyPolicy()
    {
        // Kiểm tra ReceptionAppointmentsController
        var receptionControllerType = typeof(ReceptionAppointmentsController);
        var receptionAuthorizeAttr = receptionControllerType.GetCustomAttribute<AuthorizeAttribute>();
        receptionAuthorizeAttr.Should().NotBeNull("ReceptionAppointmentsController phải có attribute [Authorize]");
        receptionAuthorizeAttr!.Policy.Should().Be("StaffOnly", "Policy của ReceptionAppointmentsController phải là StaffOnly");

        // Kiểm tra method ConfirmAppointment trên AppointmentsController
        var appointmentsControllerType = typeof(AppointmentsController);
        var confirmMethod = appointmentsControllerType.GetMethod(nameof(AppointmentsController.ConfirmAppointment));
        confirmMethod.Should().NotBeNull();
        var methodAuthorizeAttr = confirmMethod!.GetCustomAttribute<AuthorizeAttribute>();
        methodAuthorizeAttr.Should().NotBeNull("AppointmentsController.ConfirmAppointment phải có [Authorize]");
        methodAuthorizeAttr!.Policy.Should().Be("StaffOnly");
    }

    [Fact]
    public async Task AuditLog_ShouldNotContainSensitivePii()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.PendingConfirmation);

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ConfirmAppointmentCommandHandler(db, currentUser);
        var command = new ConfirmAppointmentCommand(appointment.Id, Note: "Bệnh nhân xác nhận đến đúng giờ");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var log = await db.AuditLogs.FirstAsync(a => a.EntityId == appointment.Id);
        var json = log.MetadataRedacted;
        json.Should().NotBeNullOrEmpty();

        // Tuyệt đối không log thông tin nhạy cảm (mật khẩu, OTP, CCCD, Token, số điện thoại thô)
        json.Should().NotContain("Password", because: "Audit log không được chứa mật khẩu");
        json.Should().NotContain("Otp", because: "Audit log không được chứa OTP");
        json.Should().NotContain("Secret", because: "Audit log không được chứa secrets");
        json.Should().NotContain("IdentityCard", because: "Audit log không được chứa CCCD");
        json.Should().NotContain("AccessToken", because: "Audit log không được chứa access token");
    }
}
