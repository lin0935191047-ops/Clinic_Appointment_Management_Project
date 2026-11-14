using System.Reflection;
using System.Text.Json;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Consultations.Commands.RecordConsultation;
using ClinicAppointment.Application.Features.Consultations.Commands.StartConsultation;
using ClinicAppointment.Application.Features.Consultations.Queries.GetConsultationByAppointmentId;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class ConsultationStartAndRecordTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (User UserDocA, Doctor DoctorA, User UserDocB, Doctor DoctorB, Appointment Appointment) SeedData(
        ApplicationDbContext db, 
        AppointmentStatus initialStatus = AppointmentStatus.CheckedIn)
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
            apt.Cancel("Bệnh nhân hủy lịch", DateTime.UtcNow);
        }

        db.Appointments.Add(apt);
        db.SaveChanges();

        return (userDocA, docA, userDocB, docB, apt);
    }

    [Fact]
    public async Task StartConsultation_CheckedIn_ByAssignedDoctor_ShouldSucceed_AndChangeStatusToInProgress()
    {
        // Arrange: Bác sĩ A (được phân công) bắt đầu ca khám đang ở trạng thái CheckedIn
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.CheckedIn);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, doctorAUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(AppointmentStatus.InProgress);
        result.Value.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Kiểm tra trong CSDL
        var reloaded = await db.Appointments.FindAsync(appointment.Id);
        reloaded!.Status.Should().Be(AppointmentStatus.InProgress);
        reloaded.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartConsultation_ByUnassignedDoctor_ShouldBeDenied_WithForbidden()
    {
        // Arrange: Bác sĩ B cố tình bắt đầu ca khám của Bác sĩ A
        using var db = CreateInMemoryDbContext();
        var (_, _, userDocB, _, appointment) = SeedData(db, AppointmentStatus.CheckedIn);

        var doctorBUser = new TestCurrentUserService
        {
            UserId = userDocB.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, doctorBUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bạn không được phân công phụ trách ca khám này");

        // Trạng thái giữ nguyên CheckedIn
        var reloaded = await db.Appointments.FindAsync(appointment.Id);
        reloaded!.Status.Should().Be(AppointmentStatus.CheckedIn);
    }

    [Fact]
    public async Task StartConsultation_ByPatientOrAnonymous_ShouldBeDenied_WithForbidden()
    {
        // Arrange: Bệnh nhân gọi lệnh bắt đầu khám
        using var db = CreateInMemoryDbContext();
        var (_, _, _, _, appointment) = SeedData(db, AppointmentStatus.CheckedIn);

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, patientUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task StartConsultation_InvalidState_Confirmed_NotCheckedIn_ShouldBeDenied_WithBadRequest()
    {
        // Arrange: Ca khám mới chỉ Confirmed, bệnh nhân chưa check-in tại quầy
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.Confirmed);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, doctorAUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Bị từ chối HTTP 400 Bad Request
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Bệnh nhân chưa làm thủ tục tiếp nhận tại quầy lễ tân (CheckedIn)");
    }

    [Fact]
    public async Task StartConsultation_InvalidState_AlreadyInProgress_ShouldBeDenied_WithBadRequest()
    {
        // Arrange: Ca khám đã ở trạng thái InProgress
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.InProgress);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, doctorAUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Ca khám đã được bắt đầu trước đó");
    }

    [Fact]
    public async Task StartConsultation_InvalidState_CancelledOrCompleted_ShouldBeDenied_WithBadRequest()
    {
        // Arrange: Ca khám đã bị hủy
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.Cancelled);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, doctorAUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Không thể bắt đầu ca khám đã bị hủy");
    }

    [Fact]
    public async Task StartConsultation_ShouldRecordAuditLog_WithoutClinicalData()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.CheckedIn);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new StartConsultationCommandHandler(db, doctorAUser);
        var command = new StartConsultationCommand(appointment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "START_CONSULTATION" && a.EntityId == appointment.Id);
        audit.Should().NotBeNull();
        audit!.ActorType.Should().Be("DOCTOR");
        audit.ActorUserId.Should().Be(userDocA.Id);
        audit.MetadataRedacted.Should().NotBeNullOrWhiteSpace();
        audit.MetadataRedacted.Should().Contain(appointment.BookingCode);
    }

    [Fact]
    public async Task RecordConsultation_InProgress_ByAssignedDoctor_ShouldSucceed_AndSaveConsultation()
    {
        // Arrange: Ca khám đang InProgress, Bác sĩ A ghi nhận kết quả khám
        using var db = CreateInMemoryDbContext();
        var (userDocA, docA, _, _, appointment) = SeedData(db, AppointmentStatus.InProgress);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new RecordConsultationCommandHandler(db, doctorAUser);
        var command = new RecordConsultationCommand(
            AppointmentId: appointment.Id,
            Symptoms: "Đau rát họng 3 ngày nay, nuốt đau, sốt nhẹ 37.8 độ C",
            Diagnosis: "Viêm họng cấp tính do thời tiết (J02.9)",
            ExaminationNotes: "Họng đỏ xung huyết, amidan 2 bên sưng nhe, không giả mạc",
            Conclusion: "Điều trị ngoại trú, theo dõi 3 ngày",
            FollowUpInstructions: "Súc họng nước muối ấm, uống nhiều nước ấm, tái khám nếu sốt cao liên tục"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.AppointmentId.Should().Be(appointment.Id);
        result.Value.DoctorId.Should().Be(docA.Id);
        result.Value.Symptoms.Should().Be("Đau rát họng 3 ngày nay, nuốt đau, sốt nhẹ 37.8 độ C");
        result.Value.Diagnosis.Should().Be("Viêm họng cấp tính do thời tiết (J02.9)");
        result.Value.Conclusion.Should().Be("Điều trị ngoại trú, theo dõi 3 ngày");
        result.Value.ConsultedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Kiểm tra trong CSDL
        var savedConsultation = await db.Consultations.FirstOrDefaultAsync(c => c.AppointmentId == appointment.Id);
        savedConsultation.Should().NotBeNull();
        savedConsultation!.Symptoms.Should().Be("Đau rát họng 3 ngày nay, nuốt đau, sốt nhẹ 37.8 độ C");
        savedConsultation.Diagnosis.Should().Be("Viêm họng cấp tính do thời tiết (J02.9)");
    }

    [Fact]
    public async Task RecordConsultation_ByUnassignedDoctor_ShouldBeDenied_WithForbidden()
    {
        // Arrange: Bác sĩ B cố tình ghi chép vào ca khám của Bác sĩ A
        using var db = CreateInMemoryDbContext();
        var (_, _, userDocB, _, appointment) = SeedData(db, AppointmentStatus.InProgress);

        var doctorBUser = new TestCurrentUserService
        {
            UserId = userDocB.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new RecordConsultationCommandHandler(db, doctorBUser);
        var command = new RecordConsultationCommand(
            AppointmentId: appointment.Id,
            Symptoms: "Triệu chứng thử nghiệm",
            Diagnosis: "Chẩn đoán thử nghiệm"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bạn không được phân công phụ trách ca khám này");

        // Không có bản ghi Consultation nào được tạo
        var count = await db.Consultations.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task RecordConsultation_WhenNotStarted_ShouldBeDenied_WithBadRequest()
    {
        // Arrange: Ca khám mới chỉ CheckedIn (chưa InProgress)
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.CheckedIn);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new RecordConsultationCommandHandler(db, doctorAUser);
        var command = new RecordConsultationCommand(
            AppointmentId: appointment.Id,
            Symptoms: "Triệu chứng",
            Diagnosis: "Chẩn đoán"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Bị từ chối HTTP 400 Bad Request
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Ca khám chưa được bắt đầu (InProgress)");
    }

    [Fact]
    public void RecordConsultation_RequiredFieldValidation_MissingSymptomsOrDiagnosis_FailsValidation()
    {
        // Arrange
        var validator = new RecordConsultationCommandValidator();

        var invalidCommand1 = new RecordConsultationCommand(
            AppointmentId: Guid.NewGuid(),
            Symptoms: "", // Rỗng
            Diagnosis: "Chẩn đoán"
        );

        var invalidCommand2 = new RecordConsultationCommand(
            AppointmentId: Guid.NewGuid(),
            Symptoms: "Triệu chứng",
            Diagnosis: "   " // Khoảng trắng
        );

        // Act & Assert
        var result1 = validator.Validate(invalidCommand1);
        result1.IsValid.Should().BeFalse();
        result1.Errors.Should().Contain(e => e.PropertyName == "Symptoms");

        var result2 = validator.Validate(invalidCommand2);
        result2.IsValid.Should().BeFalse();
        result2.Errors.Should().Contain(e => e.PropertyName == "Diagnosis");
    }

    [Fact]
    public async Task RecordConsultation_UpdateExistingConsultation_UpdatesSuccessfully()
    {
        // Arrange: Đã có consultation ban đầu, bác sĩ cập nhật thêm kết luận
        using var db = CreateInMemoryDbContext();
        var (userDocA, docA, _, _, appointment) = SeedData(db, AppointmentStatus.InProgress);

        var initialConsultation = Consultation.Create(
            appointmentId: appointment.Id,
            doctorId: docA.Id,
            symptoms: "Sốt",
            diagnosis: "Nghi sốt siêu vi"
        );
        db.Consultations.Add(initialConsultation);
        await db.SaveChangesAsync();

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new RecordConsultationCommandHandler(db, doctorAUser);
        var updateCommand = new RecordConsultationCommand(
            AppointmentId: appointment.Id,
            Symptoms: "Sốt giảm còn 37.2 độ C",
            Diagnosis: "Cảm cúm thông thường",
            Conclusion: "Hồi phục tốt",
            FollowUpInstructions: "Nghỉ ngơi thêm 1 ngày"
        );

        // Act
        var result = await handler.Handle(updateCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Symptoms.Should().Be("Sốt giảm còn 37.2 độ C");
        result.Value.Diagnosis.Should().Be("Cảm cúm thông thường");
        result.Value.Conclusion.Should().Be("Hồi phục tốt");

        // Số lượng consultation vẫn là 1 (cập nhật đè)
        var count = await db.Consultations.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task RecordConsultation_ShouldRecordAuditLog_StrictlyNoDiagnosisOrClinicalNotes()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, appointment) = SeedData(db, AppointmentStatus.InProgress);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new RecordConsultationCommandHandler(db, doctorAUser);
        var command = new RecordConsultationCommand(
            AppointmentId: appointment.Id,
            Symptoms: "Triệu chứng bí mật y tế bệnh nhân",
            Diagnosis: "Chẩn đoán y khoa bí mật bệnh nhân"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "RECORD_CONSULTATION");
        auditLog.Should().NotBeNull();
        auditLog!.ActorType.Should().Be("DOCTOR");
        auditLog.ActorUserId.Should().Be(userDocA.Id);

        // BẢO VỆ BỆNH ÁN TUYỆT ĐỐI: Metadata không chứa nội dung chẩn đoán hay triệu chứng chi tiết
        var metadata = auditLog.MetadataRedacted;
        metadata.Should().NotContain("Triệu chứng bí mật");
        metadata.Should().NotContain("Chẩn đoán y khoa bí mật");
        metadata.Should().Contain("HasSymptoms");
        metadata.Should().Contain("HasDiagnosis");
    }

    [Fact]
    public async Task GetConsultation_ByAssignedDoctor_ShouldSucceed_UnassignedDoctorDenied()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, docA, userDocB, _, appointment) = SeedData(db, AppointmentStatus.InProgress);

        var consultation = Consultation.Create(
            appointmentId: appointment.Id,
            doctorId: docA.Id,
            symptoms: "Đau tai trái",
            diagnosis: "Viêm tai giữa cấp tính"
        );
        db.Consultations.Add(consultation);
        await db.SaveChangesAsync();

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var doctorBUser = new TestCurrentUserService
        {
            UserId = userDocB.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handlerA = new GetConsultationByAppointmentIdQueryHandler(db, doctorAUser);
        var handlerB = new GetConsultationByAppointmentIdQueryHandler(db, doctorBUser);

        // Act 1: Bác sĩ A (được phân công) xem -> Thành công
        var resultA = await handlerA.Handle(new GetConsultationByAppointmentIdQuery(appointment.Id), CancellationToken.None);

        // Act 2: Bác sĩ B (không được phân công) xem -> Bị từ chối HTTP 403
        var resultB = await handlerB.Handle(new GetConsultationByAppointmentIdQuery(appointment.Id), CancellationToken.None);

        // Assert
        resultA.IsSuccess.Should().BeTrue();
        resultA.Value!.Symptoms.Should().Be("Đau tai trái");

        resultB.IsSuccess.Should().BeFalse();
        resultB.StatusCode.Should().Be(403);
        resultB.Error.Should().Contain("Bạn không được phân công phụ trách ca khám này");
    }

    [Fact]
    public void DoctorEndpoints_ProtectedWithAuthorizationAttribute()
    {
        // Kiểm tra controller DoctorAppointmentsController
        var apptController = typeof(DoctorAppointmentsController);
        var apptAuth = apptController.GetCustomAttribute<AuthorizeAttribute>();
        apptAuth.Should().NotBeNull();
        apptAuth!.Policy.Should().Be("StaffOrDoctor");

        // Kiểm tra controller DoctorConsultationsController
        var consultController = typeof(DoctorConsultationsController);
        var consultAuth = consultController.GetCustomAttribute<AuthorizeAttribute>();
        consultAuth.Should().NotBeNull();
        consultAuth!.Policy.Should().Be("StaffOrDoctor");
    }
}
