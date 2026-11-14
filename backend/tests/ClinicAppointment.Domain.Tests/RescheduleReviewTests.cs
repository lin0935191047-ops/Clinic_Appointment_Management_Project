using System.Reflection;
using System.Text.Json;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class RescheduleReviewTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, RescheduleRequest Request, Doctor Doctor, Patient Patient) SeedPendingRescheduleRequest(
        ApplicationDbContext db,
        DateTime? oldStartAt = null,
        DateTime? proposedStartAt = null,
        AppointmentStatus appointmentStatus = AppointmentStatus.PendingConfirmation)
    {
        var patient = Patient.Create("Trần Quốc Bảo", "0909112233", new DateOnly(1988, 11, 20), 1988);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.review@clinic.vn", "hash");
        var spec = Specialty.Create("CK-RHM", "Răng Hàm Mặt", "rang-ham-mat");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-DENT-01", "BS. Trần Nha Khoa");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-RHM-01", "Cạo vôi răng & Đánh bóng", 30, 200000m);
        db.Services.Add(service);

        var originalStart = oldStartAt ?? DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            originalStart,
            "Khám định kỳ 6 tháng");

        if (appointmentStatus == AppointmentStatus.Confirmed) appointment.Confirm();
        else if (appointmentStatus == AppointmentStatus.Cancelled) appointment.Cancel("Bận việc", DateTime.UtcNow);

        db.Appointments.Add(appointment);

        var proposedStart = proposedStartAt ?? DateTime.UtcNow.AddDays(4).Date.AddHours(14);
        var rescheduleRequest = RescheduleRequest.Create(
            appointment.Id,
            doctor.Id,
            proposedStart,
            proposedStart.AddMinutes(appointment.ServiceDurationSnapshot),
            "Xin đổi sang ca chiều vì bận ca sáng");

        db.RescheduleRequests.Add(rescheduleRequest);
        db.SaveChanges();

        return (appointment, rescheduleRequest, doctor, patient);
    }

    [Fact]
    public async Task Approve_WhenSlotAvailable_ShouldUpdateAppointmentAndApproveRequest()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, request, doctor, _) = SeedPendingRescheduleRequest(db);
        var receptionistId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            UserId = receptionistId,
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ApproveRescheduleRequestCommandHandler(db, currentUser);
        var command = new ApproveRescheduleRequestCommand(request.Id, ReviewNotes: "Đã kiểm tra lịch bác sĩ và gọi xác nhận với bệnh nhân");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(RescheduleStatus.Approved);
        result.Value.CurrentStartAtUtc.Should().Be(request.ProposedStartAt);

        // Acceptance Criteria 3: Appointment cập nhật sang slot mới
        var updatedAppointment = await db.Appointments.FindAsync(appointment.Id);
        updatedAppointment!.StartAt.Should().Be(request.ProposedStartAt);
        updatedAppointment.EndAt.Should().Be(request.ProposedEndAt);
        updatedAppointment.DoctorId.Should().Be(request.RequestedDoctorId);
        updatedAppointment.UpdatedAt.Should().NotBeNull();

        // RescheduleRequest chuyển Approved
        var updatedRequest = await db.RescheduleRequests.FindAsync(request.Id);
        updatedRequest!.Status.Should().Be(RescheduleStatus.Approved);
        updatedRequest.ReviewedByUserId.Should().Be(receptionistId);
        updatedRequest.ReviewedAt.Should().NotBeNull();
        updatedRequest.ReviewNotes.Should().Be("Đã kiểm tra lịch bác sĩ và gọi xác nhận với bệnh nhân");

        // Audit Log ghi nhận APPROVE_RESCHEDULE_REQUEST
        var auditLogs = await db.AuditLogs.Where(a => a.EntityId == appointment.Id).ToListAsync();
        auditLogs.Should().ContainSingle(a => a.Action == "APPROVE_RESCHEDULE_REQUEST");
    }

    [Fact]
    public async Task Approve_WhenSlotConflictsWithAnotherAppointment_ShouldLeaveOldScheduleUnchanged_AndReturn409()
    {
        // Arrange: Có ca khám khác trùng slot đề xuất
        using var db = CreateInMemoryDbContext();
        var (appointment, request, doctor, patient) = SeedPendingRescheduleRequest(db);
        var receptionistId = Guid.NewGuid();

        var originalStart = appointment.StartAt;
        var originalEnd = appointment.EndAt;

        var conflictingAppointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            appointment.ServiceId,
            appointment.ServiceNameSnapshot,
            appointment.ServiceDurationSnapshot,
            appointment.ServicePriceSnapshot,
            request.ProposedStartAt,
            "Ca khám khác đã chiếm slot");
        db.Appointments.Add(conflictingAppointment);
        db.SaveChanges();

        var currentUser = new TestCurrentUserService
        {
            UserId = receptionistId,
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ApproveRescheduleRequestCommandHandler(db, currentUser);
        var command = new ApproveRescheduleRequestCommand(request.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Acceptance Criteria 4: Conflict leaves old schedule unchanged
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Contain("Lịch hẹn cũ của bệnh nhân được giữ nguyên");

        // Cuộc hẹn ban đầu giữ nguyên 100%
        var unchangeAppointment = await db.Appointments.FindAsync(appointment.Id);
        unchangeAppointment!.StartAt.Should().Be(originalStart);
        unchangeAppointment.EndAt.Should().Be(originalEnd);
    }

    [Fact]
    public async Task Approve_WhenSlotConflictsWithDoctorTimeOff_ShouldLeaveOldScheduleUnchanged_AndReturn409()
    {
        // Arrange: Bác sĩ có lịch nghỉ trong khung giờ đề xuất
        using var db = CreateInMemoryDbContext();
        var (appointment, request, doctor, _) = SeedPendingRescheduleRequest(db);
        var originalStart = appointment.StartAt;

        var timeOff = DoctorTimeOff.Create(
            doctor.Id,
            request.ProposedStartAt.AddHours(-1),
            request.ProposedEndAt.AddHours(1),
            doctor.UserId,
            "Nghỉ phép cá nhân");
        db.DoctorTimeOffs.Add(timeOff);
        db.SaveChanges();

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ApproveRescheduleRequestCommandHandler(db, currentUser);
        var command = new ApproveRescheduleRequestCommand(request.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Acceptance Criteria 4: Old schedule unchanged
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error.Should().Contain("lịch nghỉ");

        var unchangeAppointment = await db.Appointments.FindAsync(appointment.Id);
        unchangeAppointment!.StartAt.Should().Be(originalStart);
    }

    [Fact]
    public async Task Reject_WithValidReason_ShouldKeepOldAppointment_AndSetRequestToRejected()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, request, _, _) = SeedPendingRescheduleRequest(db);
        var receptionistId = Guid.NewGuid();
        var originalStart = appointment.StartAt;
        var originalEnd = appointment.EndAt;
        var originalDoctorId = appointment.DoctorId;

        var currentUser = new TestCurrentUserService
        {
            UserId = receptionistId,
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new RejectRescheduleRequestCommandHandler(db, currentUser);
        var command = new RejectRescheduleRequestCommand(request.Id, ReviewNotes: "Bác sĩ kín lịch phẫu thuật ngày hôm đó, mong quý khách thông cảm");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: Acceptance Criteria 2: Reject leaves old appointment
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value!.Status.Should().Be(RescheduleStatus.Rejected);
        result.Value.ReviewNotes.Should().Be("Bác sĩ kín lịch phẫu thuật ngày hôm đó, mong quý khách thông cảm");

        // Ca khám ban đầu hoàn toàn giữ nguyên
        var keptAppointment = await db.Appointments.FindAsync(appointment.Id);
        keptAppointment!.StartAt.Should().Be(originalStart);
        keptAppointment.EndAt.Should().Be(originalEnd);
        keptAppointment.DoctorId.Should().Be(originalDoctorId);

        // RescheduleRequest chuyển Rejected
        var rejectedRequest = await db.RescheduleRequests.FindAsync(request.Id);
        rejectedRequest!.Status.Should().Be(RescheduleStatus.Rejected);
        rejectedRequest.ReviewedByUserId.Should().Be(receptionistId);
        rejectedRequest.ReviewedAt.Should().NotBeNull();

        // Audit Log ghi nhận REJECT_RESCHEDULE_REQUEST
        var auditLogs = await db.AuditLogs.Where(a => a.EntityId == appointment.Id).ToListAsync();
        auditLogs.Should().ContainSingle(a => a.Action == "REJECT_RESCHEDULE_REQUEST");
    }

    [Fact]
    public async Task Reject_WithoutReason_ShouldFailWith400()
    {
        // Arrange: Từ chối nhưng không nhập lý do
        using var db = CreateInMemoryDbContext();
        var (_, request, _, _) = SeedPendingRescheduleRequest(db);

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new RejectRescheduleRequestCommandHandler(db, currentUser);
        var command = new RejectRescheduleRequestCommand(request.Id, ReviewNotes: "   ");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Lý do từ chối");
    }

    [Fact]
    public async Task Review_ByNonStaffUser_ShouldFailWith403()
    {
        // Arrange: Bệnh nhân (PATIENT) hoặc người ngoài cố tình duyệt/từ chối
        using var db = CreateInMemoryDbContext();
        var (_, request, _, _) = SeedPendingRescheduleRequest(db);

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode }, // Role PATIENT (không phải STAFF)
            IsAuthenticated = true
        };

        var approveHandler = new ApproveRescheduleRequestCommandHandler(db, patientUser);
        var rejectHandler = new RejectRescheduleRequestCommandHandler(db, patientUser);

        // Act
        var approveResult = await approveHandler.Handle(new ApproveRescheduleRequestCommand(request.Id), CancellationToken.None);
        var rejectResult = await rejectHandler.Handle(new RejectRescheduleRequestCommand(request.Id, "Lý do"), CancellationToken.None);

        // Assert: Acceptance Criteria 1: Only Receptionist can review
        approveResult.IsSuccess.Should().BeFalse();
        approveResult.StatusCode.Should().Be(403);
        approveResult.Error.Should().Contain("không có quyền");

        rejectResult.IsSuccess.Should().BeFalse();
        rejectResult.StatusCode.Should().Be(403);
        rejectResult.Error.Should().Contain("không có quyền");
    }

    [Fact]
    public async Task Review_WhenRequestAlreadyReviewed_ShouldFailWith400()
    {
        // Arrange: Yêu cầu đã được duyệt hoặc từ chối trước đó
        using var db = CreateInMemoryDbContext();
        var (_, request, _, _) = SeedPendingRescheduleRequest(db);
        request.Approve(Guid.NewGuid());
        db.SaveChanges();

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var approveHandler = new ApproveRescheduleRequestCommandHandler(db, currentUser);
        var rejectHandler = new RejectRescheduleRequestCommandHandler(db, currentUser);

        // Act
        var approveResult = await approveHandler.Handle(new ApproveRescheduleRequestCommand(request.Id), CancellationToken.None);
        var rejectResult = await rejectHandler.Handle(new RejectRescheduleRequestCommand(request.Id, "Lý do"), CancellationToken.None);

        // Assert
        approveResult.IsSuccess.Should().BeFalse();
        approveResult.StatusCode.Should().Be(400);
        approveResult.Error.Should().Contain("Approved");

        rejectResult.IsSuccess.Should().BeFalse();
        rejectResult.StatusCode.Should().Be(400);
        rejectResult.Error.Should().Contain("Approved");
    }

    [Fact]
    public async Task GetPendingRescheduleRequests_ShouldReturnOnlyPendingReviewRequests()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (app1, req1, _, _) = SeedPendingRescheduleRequest(db);
        var (app2, req2, _, _) = SeedPendingRescheduleRequest(db);

        // Cho req2 thành Approved
        req2.Approve(Guid.NewGuid());
        db.SaveChanges();

        var currentUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetPendingRescheduleRequestsQueryHandler(db, currentUser);

        // Act
        var result = await handler.Handle(new GetPendingRescheduleRequestsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().ContainSingle(r => r.RequestId == req1.Id);
        result.Value.Should().NotContain(r => r.RequestId == req2.Id);
    }

    [Fact]
    public void ReceptionRescheduleRequestsController_ShouldHave_AuthorizeStaffOnlyAttribute()
    {
        var controllerType = typeof(ReceptionRescheduleRequestsController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttr.Should().NotBeNull("ReceptionRescheduleRequestsController phải có attribute [Authorize]");
        authorizeAttr!.Policy.Should().Be("StaffOnly", "Policy của ReceptionRescheduleRequestsController phải là StaffOnly");
    }
}
