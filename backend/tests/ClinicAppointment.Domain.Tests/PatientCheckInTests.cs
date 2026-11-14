using System.Reflection;
using System.Text.Json;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PatientCheckInTests
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
        AppointmentStatus status = AppointmentStatus.Confirmed)
    {
        var patient = Patient.Create("Đỗ Mai Phương", "0912345678", new DateOnly(1995, 8, 20), 1995);
        db.Patients.Add(patient);

        var doctorUser = User.Create("dr.checkin@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội tổng quát", "noi-tong-quat");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-INT-01", "BS. Hoàng Minh Trí");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-NOI-01", "Khám nội tổng quát chuyên sâu", 30, 250000m);
        db.Services.Add(service);

        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            DateTime.UtcNow.AddHours(2),
            "Khám sức khỏe tổng quát");

        if (status == AppointmentStatus.Confirmed)
        {
            appointment.Confirm();
        }
        else if (status == AppointmentStatus.CheckedIn)
        {
            appointment.Confirm();
            appointment.CheckIn();
        }
        else if (status == AppointmentStatus.InProgress)
        {
            appointment.Confirm();
            appointment.CheckIn();
            appointment.Status = AppointmentStatus.InProgress;
        }
        else if (status == AppointmentStatus.Completed)
        {
            appointment.Confirm();
            appointment.CheckIn();
            appointment.Status = AppointmentStatus.Completed;
        }
        else if (status == AppointmentStatus.Cancelled)
        {
            appointment.Cancel("Người bệnh có việc bận gia đình", DateTime.UtcNow);
        }

        db.Appointments.Add(appointment);
        db.SaveChanges();

        return appointment;
    }

    [Fact]
    public async Task CheckIn_Confirmed_ByReceptionist_ShouldSucceed_AndChangeStatusToCheckedIn()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Confirmed);
        var receptionistUserId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            UserId = receptionistUserId,
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id, Note: "Bệnh nhân đã đến quầy số 2 tiếp nhận");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.AppointmentId.Should().Be(appointment.Id);
        result.Value.Status.Should().Be(AppointmentStatus.CheckedIn);
        result.Value.Note.Should().Be("Bệnh nhân đã đến quầy số 2 tiếp nhận");
        result.Value.CheckedInAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Kiểm tra trong CSDL
        var reloaded = await db.Appointments.FindAsync(appointment.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(AppointmentStatus.CheckedIn);
        reloaded.UpdatedAt.Should().NotBeNull();
        reloaded.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CheckIn_Confirmed_ByAdmin_ShouldSucceed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Confirmed);
        var adminUserId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            UserId = adminUserId,
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id, Note: "Quản trị viên hỗ trợ check-in tại quầy");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value!.Status.Should().Be(AppointmentStatus.CheckedIn);
    }

    [Fact]
    public async Task CheckIn_ShouldRecordAuditLog_WithAllRequiredFields()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Confirmed);
        var receptionistUserId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            UserId = receptionistUserId,
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id, Note: "Tiếp nhận giấy hẹn số khám");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CHECK_IN_APPOINTMENT" && a.EntityId == appointment.Id);

        auditLog.Should().NotBeNull();
        auditLog!.ActorType.Should().Be("RECEPTIONIST");
        auditLog.ActorUserId.Should().Be(receptionistUserId);
        auditLog.EntityType.Should().Be("Appointment");
        auditLog.EntityId.Should().Be(appointment.Id);
        auditLog.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Kiểm tra nội dung metadata
        var metadataJson = auditLog.MetadataRedacted;
        metadataJson.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(metadataJson!);
        var root = doc.RootElement;
        root.GetProperty("BookingCode").GetString().Should().Be(appointment.BookingCode);
        root.GetProperty("DoctorId").GetGuid().Should().Be(appointment.DoctorId);
        root.GetProperty("PatientId").GetGuid().Should().Be(appointment.PatientId);
        root.GetProperty("Note").GetString().Should().Be("Tiếp nhận giấy hẹn số khám");
        root.TryGetProperty("CheckedInAt", out _).Should().BeTrue();

        // Không chứa thông tin nhạy cảm
        metadataJson.Should().NotContain("Password");
        metadataJson.Should().NotContain("Otp");
        metadataJson.Should().NotContain("0912345678");
    }

    [Fact]
    public async Task CheckIn_ByPatientOrAnonymous_ShouldBeDenied_WithForbidden()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.Confirmed);

        // Trường hợp 1: Người dùng là Bệnh nhân
        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, patientUser);
        var command = new CheckInAppointmentCommand(appointment.Id);

        // Act 1
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert 1
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bạn không có quyền tiếp nhận/check-in");

        // Trường hợp 2: Khách ẩn danh
        var anonUser = new TestCurrentUserService
        {
            UserId = null,
            Roles = new List<string>(),
            IsAuthenticated = false
        };

        var anonHandler = new CheckInAppointmentCommandHandler(db, anonUser);

        // Act 2
        var anonResult = await anonHandler.Handle(command, CancellationToken.None);

        // Assert 2
        anonResult.IsSuccess.Should().BeFalse();
        anonResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task CheckIn_NonExistentAppointment_ShouldReturnNotFound()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error.Should().Contain("Không tìm thấy thông tin ca khám");
    }

    [Fact]
    public async Task CheckIn_PendingConfirmation_ShouldBeDenied_WithBadRequest()
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

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Chờ xác nhận (PendingConfirmation)");
        result.Error.Should().Contain("Vui lòng xác nhận lịch hẹn trước khi làm thủ tục check-in");

        // Trạng thái giữ nguyên
        var reloaded = await db.Appointments.FindAsync(appointment.Id);
        reloaded!.Status.Should().Be(AppointmentStatus.PendingConfirmation);
    }

    [Fact]
    public async Task CheckIn_AlreadyCheckedIn_ShouldBeDenied_WithBadRequest()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.CheckedIn);
        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Lịch hẹn đã được check-in tiếp nhận trước đó");
    }

    [Fact]
    public async Task CheckIn_Cancelled_ShouldBeDenied_WithBadRequest()
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

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Không thể check-in cho lịch hẹn đã bị hủy");
    }

    [Fact]
    public async Task CheckIn_Completed_ShouldBeDenied_WithBadRequest()
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

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Không thể check-in cho lịch hẹn đã hoàn thành");
    }

    [Fact]
    public async Task CheckIn_InProgress_ShouldBeDenied_WithBadRequest()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointment = SeedAppointment(db, AppointmentStatus.InProgress);
        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new CheckInAppointmentCommandHandler(db, currentUser);
        var command = new CheckInAppointmentCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Không thể check-in cho lịch hẹn đang trong ca khám");
    }

    [Fact]
    public void CheckIn_DomainEntity_CheckInMethod_ThrowsWhenNotConfirmed()
    {
        // Arrange
        var appointment = Appointment.CreateGuest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dịch vụ thử nghiệm",
            30,
            100000m,
            DateTime.UtcNow.AddDays(1),
            "Khám kiểm tra");

        // Act & Assert 1: Ở PendingConfirmation
        var act1 = () => appointment.CheckIn();
        act1.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể Check-in cho lịch hẹn ở trạng thái PendingConfirmation*");

        // Chuyển sang Confirmed rồi CheckIn thành công
        appointment.Confirm();
        appointment.CheckIn();
        appointment.Status.Should().Be(AppointmentStatus.CheckedIn);

        // Act & Assert 2: Khi đã CheckedIn gọi tiếp CheckIn phải ném ngoại lệ
        var act2 = () => appointment.CheckIn();
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể Check-in cho lịch hẹn ở trạng thái CheckedIn*");
    }

    [Fact]
    public void CheckIn_ControllerEndpoints_HaveStaffOnlyAuthorizationAttribute()
    {
        // Kiểm tra controller ReceptionAppointmentsController
        var receptionControllerType = typeof(ReceptionAppointmentsController);
        var receptionClassAuth = receptionControllerType.GetCustomAttribute<AuthorizeAttribute>();
        receptionClassAuth.Should().NotBeNull();
        receptionClassAuth!.Policy.Should().Be("StaffOnly");

        var receptionCheckInMethod = receptionControllerType.GetMethod(nameof(ReceptionAppointmentsController.CheckInAppointment));
        receptionCheckInMethod.Should().NotBeNull();

        // Kiểm tra controller AppointmentsController endpoint CheckInAppointment
        var apptControllerType = typeof(AppointmentsController);
        var apptCheckInMethod = apptControllerType.GetMethod(nameof(AppointmentsController.CheckInAppointment));
        apptCheckInMethod.Should().NotBeNull();
        var apptMethodAuth = apptCheckInMethod!.GetCustomAttribute<AuthorizeAttribute>();
        apptMethodAuth.Should().NotBeNull();
        apptMethodAuth!.Policy.Should().Be("StaffOnly");
    }
}
