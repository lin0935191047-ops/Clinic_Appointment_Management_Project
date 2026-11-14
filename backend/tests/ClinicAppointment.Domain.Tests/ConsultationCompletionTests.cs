using System.Reflection;
using System.Text.Json;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Consultations.Commands.CompleteConsultation;
using ClinicAppointment.Application.Features.Consultations.Commands.RecordConsultation;
using ClinicAppointment.Application.Features.Consultations.Commands.StartConsultation;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class ConsultationCompletionTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (User UserDocA, Doctor DoctorA, User UserDocB, Doctor DoctorB, Appointment Appointment, Consultation Consultation) SeedData(
        ApplicationDbContext db, 
        AppointmentStatus initialStatus = AppointmentStatus.InProgress,
        bool createConsultationRecord = true)
    {
        var spec = Specialty.Create("CK-TAI", "Tai Mũi Họng", "tai-mui-hong");
        db.Specialties.Add(spec);

        var userDocA = User.Create("dr.quang@clinic.vn", "hash_pw_1");
        var userDocB = User.Create("dr.linh@clinic.vn", "hash_pw_2");
        db.Users.AddRange(userDocA, userDocB);

        var docA = Doctor.Create(userDocA.Id, spec.Id, "DOC-ENT-01", "BS. Lê Quang");
        docA.Title = "Bác sĩ Chuyên khoa";
        docA.RoomNumber = "Phòng 301";

        var docB = Doctor.Create(userDocB.Id, spec.Id, "DOC-ENT-02", "BS. Phạm Linh");
        docB.Title = "Bác sĩ";
        docB.RoomNumber = "Phòng 302";

        db.Doctors.AddRange(docA, docB);

        var svc = Service.Create(spec.Id, "DV-ENT-01", "Nội soi tai mũi họng", 30, 300000m);
        db.Services.Add(svc);

        var patient = Patient.Create("Hoàng Văn Thái", "0909888777", new DateOnly(1988, 6, 20), 1988);
        db.Patients.Add(patient);

        var apt = Appointment.CreateGuest(
            docA.Id, 
            patient.Id, 
            svc.Id, 
            svc.Name, 
            svc.DurationMinutes, 
            svc.Price, 
            DateTime.UtcNow.AddMinutes(30), 
            "Viêm họng hạt và nghẹt mũi");

        if (initialStatus == AppointmentStatus.Confirmed)
        {
            apt.Confirm();
        }
        else if (initialStatus == AppointmentStatus.CheckedIn)
        {
            apt.Confirm();
            apt.CheckIn();
        }
        else if (initialStatus == AppointmentStatus.InProgress)
        {
            apt.Confirm();
            apt.CheckIn();
            apt.StartConsultation();
        }
        else if (initialStatus == AppointmentStatus.Completed)
        {
            apt.Confirm();
            apt.CheckIn();
            apt.StartConsultation();
            apt.CompleteConsultation();
        }
        else if (initialStatus == AppointmentStatus.Cancelled)
        {
            apt.Cancel("Bệnh nhân bận việc đột xuất", DateTime.UtcNow);
        }

        db.Appointments.Add(apt);

        Consultation consultation = null!;
        if (createConsultationRecord)
        {
            consultation = Consultation.Create(
                appointmentId: apt.Id,
                doctorId: docA.Id,
                symptoms: "Bệnh nhân đau rát họng, nuốt vướng, ho húng hắng 3 ngày nay",
                diagnosis: "Viêm họng cấp thể xuất tiết",
                examinationNotes: "Niêm mạc họng sung huyết đỏ, hai amidan quá phát độ I không có giả mạc",
                conclusion: "Theo dõi viêm họng cấp",
                followUpInstructions: "Uống nhiều nước ấm, súc họng nước muối sinh lý",
                consultedAtUtc: DateTime.UtcNow.AddMinutes(-10)
            );
            db.Consultations.Add(consultation);
        }

        db.SaveChanges();
        return (userDocA, docA, userDocB, docB, apt, consultation);
    }

    [Fact]
    public async Task CompleteConsultation_WithInProgressAppointment_SucceedsAndTransitionsToCompleted()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, docA, _, _, apt, _) = SeedData(db, AppointmentStatus.InProgress);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(
            AppointmentId: apt.Id,
            Conclusion: "Viêm họng cấp đã kê đơn thuốc 5 ngày",
            FollowUpInstructions: "Tái khám sau 5 ngày nếu còn sốt hoặc đau tăng"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(AppointmentStatus.Completed);
        result.Value.Conclusion.Should().Be("Viêm họng cấp đã kê đơn thuốc 5 ngày");
        result.Value.FollowUpInstructions.Should().Be("Tái khám sau 5 ngày nếu còn sốt hoặc đau tăng");
        result.Value.DoctorName.Should().Be(docA.FullName);

        var updatedApt = await db.Appointments.FindAsync(apt.Id);
        updatedApt!.Status.Should().Be(AppointmentStatus.Completed);
        updatedApt.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var updatedConsultation = await db.Consultations.FirstOrDefaultAsync(c => c.AppointmentId == apt.Id);
        updatedConsultation!.Conclusion.Should().Be("Viêm họng cấp đã kê đơn thuốc 5 ngày");
        updatedConsultation.FollowUpInstructions.Should().Be("Tái khám sau 5 ngày nếu còn sốt hoặc đau tăng");
    }

    [Theory]
    [InlineData(AppointmentStatus.PendingConfirmation)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.CheckedIn)]
    public async Task CompleteConsultation_WhenNotInProgress_ReturnsBadRequest(AppointmentStatus nonInProgressStatus)
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, nonInProgressStatus);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("InProgress");
    }

    [Fact]
    public async Task CompleteConsultation_WhenAlreadyCompleted_ReturnsBadRequest()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, AppointmentStatus.Completed);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("hoàn tất trước đó");
    }

    [Fact]
    public async Task CompleteConsultation_WhenCancelled_ReturnsBadRequest()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, AppointmentStatus.Cancelled);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("hủy");
    }

    [Fact]
    public async Task CompleteConsultation_WhenNoConsultationRecordExists_ReturnsBadRequest()
    {
        // Arrange: Ca khám InProgress nhưng chưa có kết quả Consultation nào
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, AppointmentStatus.InProgress, createConsultationRecord: false);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("chưa được ghi chép kết quả");
    }

    [Fact]
    public async Task CompleteConsultation_ByUnassignedDoctor_ThrowsForbidden()
    {
        // Arrange: Doctor B cố gắng complete ca của Doctor A
        using var db = CreateInMemoryDbContext();
        var (_, _, userDocB, _, apt, _) = SeedData(db, AppointmentStatus.InProgress);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocB.Id,
            Email = userDocB.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("không được phân công");
    }

    [Fact]
    public async Task CompleteConsultation_ByDoctorWithoutProfile_ThrowsForbidden()
    {
        // Arrange: Tài khoản có role Doctor nhưng chưa có hồ sơ Bác sĩ trong DB
        using var db = CreateInMemoryDbContext();
        var (_, _, _, _, apt, _) = SeedData(db, AppointmentStatus.InProgress);

        var fakeDoctorUserId = Guid.NewGuid();
        var currentUserService = new TestCurrentUserService
        {
            UserId = fakeDoctorUserId,
            Email = "stranger.doctor@clinic.vn",
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("chưa được liên kết với hồ sơ Bác sĩ");
    }

    [Fact]
    public async Task CompleteConsultation_ByPatientOrAnonymous_ThrowsForbidden()
    {
        // Arrange: Bệnh nhân cố gắng hoàn tất ca khám
        using var db = CreateInMemoryDbContext();
        var (_, _, _, _, apt, _) = SeedData(db, AppointmentStatus.InProgress);

        var currentUserService = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "patient@gmail.com",
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task CompleteConsultation_ByAdmin_Succeeds()
    {
        // Arrange: Admin có quyền hoàn thành ca khám
        using var db = CreateInMemoryDbContext();
        var (_, _, _, _, apt, _) = SeedData(db, AppointmentStatus.InProgress);

        var currentUserService = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "admin@clinic.vn",
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(
            AppointmentId: apt.Id,
            Conclusion: "Admin duyệt hoàn tất ca khám theo biên bản"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public async Task CompleteConsultation_WritesAuditLog_WithoutPiiOrMedicalNotes()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, AppointmentStatus.InProgress);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new CompleteConsultationCommandHandler(db, currentUserService);
        var command = new CompleteConsultationCommand(
            AppointmentId: apt.Id,
            Conclusion: "Đã khỏi triệu chứng cấp tính",
            FollowUpInstructions: "Giữ ấm cổ họng"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLogs = await db.AuditLogs
            .Where(l => l.Action == "COMPLETE_CONSULTATION" && l.EntityId == apt.Id)
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
        var log = auditLogs.First();
        log.ActorType.Should().Be("DOCTOR");
        log.ActorUserId.Should().Be(userDocA.Id);

        // Kiểm tra bảo mật thông tin y tế: Tuyệt đối không log nội dung chẩn đoán hay triệu chứng bệnh
        log.MetadataRedacted.Should().NotBeNull();
        log.MetadataRedacted.Should().NotContain("Bệnh nhân đau rát họng");
        log.MetadataRedacted.Should().NotContain("Viêm họng cấp");
        log.MetadataRedacted.Should().NotContain("Đã khỏi triệu chứng cấp tính");

        var metaJson = JsonDocument.Parse(log.MetadataRedacted!);
        metaJson.RootElement.GetProperty("HasConclusion").GetBoolean().Should().BeTrue();
        metaJson.RootElement.GetProperty("HasFollowUpInstructions").GetBoolean().Should().BeTrue();
        metaJson.RootElement.GetProperty("AppointmentId").GetString().Should().Be(apt.Id.ToString());
    }

    [Fact]
    public async Task RecordConsultation_WhenAppointmentAlreadyCompleted_IsBlocked()
    {
        // Arrange: Ca khám đã Completed -> Bác sĩ cố sửa kết quả khám phải bị chặn
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, AppointmentStatus.Completed);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new RecordConsultationCommandHandler(db, currentUserService);
        var command = new RecordConsultationCommand(
            AppointmentId: apt.Id,
            Symptoms: "Triệu chứng mới",
            Diagnosis: "Chẩn đoán sửa đổi"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("hoàn tất");
        result.Error.Should().Contain("khóa hồ sơ");
    }

    [Fact]
    public async Task StartConsultation_WhenAppointmentAlreadyCompleted_IsBlocked()
    {
        // Arrange: Ca khám đã Completed -> Cố tình Start lại phải bị chặn
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apt, _) = SeedData(db, AppointmentStatus.Completed);

        var currentUserService = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Email = userDocA.Email,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, currentUserService);
        var command = new StartConsultationCommand(apt.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void CompleteConsultationValidator_ValidatesExcessiveLength()
    {
        // Arrange
        var validator = new CompleteConsultationCommandValidator();

        var validCommand = new CompleteConsultationCommand(
            AppointmentId: Guid.NewGuid(),
            Conclusion: "Kết luận hợp lệ",
            FollowUpInstructions: "Hướng dẫn hợp lệ"
        );

        var invalidCommand = new CompleteConsultationCommand(
            AppointmentId: Guid.Empty,
            Conclusion: new string('A', 2001),
            FollowUpInstructions: new string('B', 2001)
        );

        // Act & Assert
        validator.Validate(validCommand).IsValid.Should().BeTrue();

        var invalidResult = validator.Validate(invalidCommand);
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.Errors.Should().Contain(e => e.PropertyName == "AppointmentId");
        invalidResult.Errors.Should().Contain(e => e.PropertyName == "Conclusion");
        invalidResult.Errors.Should().Contain(e => e.PropertyName == "FollowUpInstructions");
    }

    [Fact]
    public void DoctorAppointmentsController_CompleteEndpoint_ProtectedWithStaffOrDoctorPolicy()
    {
        // Arrange & Act
        var controllerType = typeof(DoctorAppointmentsController);
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        var completeMethod = controllerType.GetMethod(nameof(DoctorAppointmentsController.CompleteConsultation));

        // Assert
        authAttr.Should().NotBeNull();
        authAttr!.Policy.Should().Be("StaffOrDoctor");
        completeMethod.Should().NotBeNull();
    }
}
