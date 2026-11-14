using ClinicAppointment.Application.Features.TimeOffs.Commands;
using ClinicAppointment.Application.Features.TimeOffs.DTOs;
using ClinicAppointment.Application.Features.TimeOffs.Queries;
using ClinicAppointment.Application.Features.TimeOffs.Services;
using ClinicAppointment.Application.Features.TimeOffs.Validators;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class DoctorTimeOffTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateTimeOff_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctorUser = User.Create("dr.a@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-01", "BS. Nguyễn Văn A");
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);
        var handler = new TimeOffCommandHandlers(db, timeOffService);

        var startAt = DateTime.UtcNow.Date.AddDays(2).AddHours(8);
        var endAt = startAt.AddHours(4); // 08:00 - 12:00
        var request = new CreateDoctorTimeOffRequest(
            DoctorId: doctor.Id,
            StartAt: startAt,
            EndAt: endAt,
            Reason: "Nghỉ phép cá nhân"
        );

        // Act: Bác sĩ tự đăng ký cho chính mình
        var result = await handler.Handle(new CreateDoctorTimeOffCommand(request, doctorUser.Id, IsStaff: false), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DoctorId.Should().Be(doctor.Id);
        result.DoctorName.Should().Be("BS. Nguyễn Văn A");
        result.Status.Should().Be(TimeOffStatus.Active);
        result.Reason.Should().Be("Nghỉ phép cá nhân");

        var inDb = await db.DoctorTimeOffs.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be(TimeOffStatus.Active);
    }

    [Fact]
    public void Validator_ShouldRejectStartTimeGreaterThanOrEqualToEndTime()
    {
        var validator = new CreateDoctorTimeOffRequestValidator();

        var now = DateTime.UtcNow;
        var reqEqual = new CreateDoctorTimeOffRequest(Guid.NewGuid(), now, now);
        validator.TestValidate(reqEqual).ShouldHaveValidationErrorFor(x => x.StartAt);

        var reqGreater = new CreateDoctorTimeOffRequest(Guid.NewGuid(), now.AddHours(2), now);
        validator.TestValidate(reqGreater).ShouldHaveValidationErrorFor(x => x.StartAt);
    }

    [Fact]
    public void DoctorTimeOffEntity_ShouldEnforceInvariants()
    {
        var doctorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // startAt >= endAt
        var actInvalidTime = () => DoctorTimeOff.Create(doctorId, now.AddHours(2), now, userId);
        actInvalidTime.Should().Throw<ArgumentException>()
            .WithMessage("*bắt đầu nghỉ phải trước thời điểm kết thúc*");

        // DoctorId empty
        var actInvalidDoc = () => DoctorTimeOff.Create(Guid.Empty, now, now.AddHours(1), userId);
        actInvalidDoc.Should().Throw<ArgumentException>()
            .WithMessage("*Bác sĩ không hợp lệ*");
    }

    [Fact]
    public async Task CreateTimeOff_WhenUnauthorizedDoctor_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange: Bác sĩ 1 cố tình xin nghỉ cho Bác sĩ 2
        using var db = CreateInMemoryDbContext();
        var doc1User = User.Create("dr1@clinic.vn", "hash");
        var doc2User = User.Create("dr2@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.AddRange(doc1User, doc2User);
        db.Specialties.Add(spec);

        var doctor2 = Doctor.Create(doc2User.Id, spec.Id, "DOC-02", "BS. B");
        db.Doctors.Add(doctor2);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);
        var handler = new TimeOffCommandHandlers(db, timeOffService);

        var request = new CreateDoctorTimeOffRequest(
            DoctorId: doctor2.Id,
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(1).AddHours(4)
        );

        // Act: doc1User không phải Staff và không phải chủ tài khoản doctor2
        var act = async () => await handler.Handle(
            new CreateDoctorTimeOffCommand(request, doc1User.Id, IsStaff: false), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*chỉ có quyền đăng ký ngày nghỉ cho chính mình*");
    }

    [Fact]
    public async Task CreateTimeOff_WhenOverlappingExistingActiveTimeOff_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-03", "BS. C");
        db.Doctors.Add(doctor);

        var baseTime = DateTime.UtcNow.Date.AddDays(3);
        // Đã có lịch nghỉ: 08:00 - 12:00
        var existing = DoctorTimeOff.Create(doctor.Id, baseTime.AddHours(8), baseTime.AddHours(12), user.Id);
        db.DoctorTimeOffs.Add(existing);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);
        var handler = new TimeOffCommandHandlers(db, timeOffService);

        // Thử xin nghỉ chồng lấn: 10:00 - 15:00
        var overlapReq = new CreateDoctorTimeOffRequest(
            DoctorId: doctor.Id,
            StartAt: baseTime.AddHours(10),
            EndAt: baseTime.AddHours(15)
        );

        // Act
        var act = async () => await handler.Handle(
            new CreateDoctorTimeOffCommand(overlapReq, user.Id, IsStaff: false), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã có lịch nghỉ trùng hoặc giao nhau*");
    }

    [Fact]
    public async Task Invariant_DoNotBookOverlapTimeOff_ServiceShouldDetectConflict()
    {
        // Acceptance criteria: Không book overlap time off (FR-035)
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-04", "BS. D");
        db.Doctors.Add(doctor);

        var day = DateTime.UtcNow.Date.AddDays(5);
        // Ngày nghỉ: 08:00 - 12:00
        var timeOff = DoctorTimeOff.Create(doctor.Id, day.AddHours(8), day.AddHours(12), user.Id);
        db.DoctorTimeOffs.Add(timeOff);

        // Khung giờ họp: 14:00 - 16:00
        var block = DoctorScheduleBlock.Create(doctor.Id, day.AddHours(14), day.AddHours(16), user.Id, ScheduleBlockType.Meeting, "Giao ban khoa");
        db.DoctorScheduleBlocks.Add(block);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);

        // 1. Khung giờ 09:00 - 09:30 (rơi vào thời gian nghỉ) -> PHẢI BÁO XUNG ĐỘT (Conflict = true)
        var conflict1 = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(9), day.AddHours(9).AddMinutes(30));
        conflict1.Should().BeTrue();

        // 2. Khung giờ 14:30 - 15:00 (rơi vào giờ họp) -> PHẢI BÁO XUNG ĐỘT (Conflict = true)
        var conflict2 = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(14).AddMinutes(30), day.AddHours(15));
        conflict2.Should().BeTrue();

        // 3. Khung giờ 12:30 - 13:30 (ngoài giờ nghỉ và ngoài giờ họp) -> KHÔNG XUNG ĐỘT (Conflict = false)
        var noConflict = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(12).AddMinutes(30), day.AddHours(13).AddMinutes(30));
        noConflict.Should().BeFalse();
    }

    [Fact]
    public async Task Invariant_ScheduleChangeDoesNotSilentlyDeleteAppointments()
    {
        // Acceptance criteria: Schedule change không âm thầm xóa appointments (FR-055)
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-05", "BS. E");
        db.Doctors.Add(doctor);

        var targetDay = DateTime.UtcNow.Date.AddDays(4);
        var srv = Service.Create(spec.Id, "SRV-01", "Khám nội", 30, 100000m);
        db.Services.Add(srv);

        // Đã có 2 cuộc hẹn của bệnh nhân trong khoảng 08:00 - 12:00
        var apt1 = Appointment.CreateGuest(
            doctorId: doctor.Id,
            patientId: Guid.NewGuid(),
            serviceId: srv.Id,
            serviceName: srv.Name,
            serviceDuration: 30,
            servicePrice: 100000m,
            startAtUtc: targetDay.AddHours(9),
            reasonForVisit: "Tái khám huyết áp"
        );
        var apt2 = Appointment.CreateGuest(
            doctorId: doctor.Id,
            patientId: Guid.NewGuid(),
            serviceId: srv.Id,
            serviceName: srv.Name,
            serviceDuration: 30,
            servicePrice: 100000m,
            startAtUtc: targetDay.AddHours(10),
            reasonForVisit: "Khám định kỳ"
        );
        db.Appointments.AddRange(apt1, apt2);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);
        var handler = new TimeOffCommandHandlers(db, timeOffService);

        var timeOffReq = new CreateDoctorTimeOffRequest(
            DoctorId: doctor.Id,
            StartAt: targetDay.AddHours(8),
            EndAt: targetDay.AddHours(12),
            Reason: "Đi công tác khẩn cấp",
            AllowWithExistingAppointments: false
        );

        // 1. Khi AllowWithExistingAppointments = false -> Hệ thống từ chối và cảnh báo rõ ràng
        var actRejected = async () => await handler.Handle(
            new CreateDoctorTimeOffCommand(timeOffReq, user.Id, IsStaff: false), CancellationToken.None);
        await actRejected.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*có 2 cuộc hẹn đã được đặt trước*Hệ thống bảo toàn cuộc hẹn và không âm thầm xóa lịch*");

        // Kiểm tra database: cả 2 cuộc hẹn vẫn tồn tại 100%
        var existingApts = await db.Appointments.Where(a => a.DoctorId == doctor.Id).ToListAsync();
        existingApts.Should().HaveCount(2);

        // 2. Khi nhân viên/bác sĩ xác nhận AllowWithExistingAppointments = true
        var confirmedReq = timeOffReq with { AllowWithExistingAppointments = true };
        var result = await handler.Handle(
            new CreateDoctorTimeOffCommand(confirmedReq, user.Id, IsStaff: false), CancellationToken.None);

        result.Should().NotBeNull();
        result.AffectedAppointments.Should().NotBeNull();
        result.AffectedAppointments.Should().HaveCount(2);

        // ĐẢM BẢO BẤT BIẾN: Cả 2 cuộc hẹn vẫn nguyên vẹn trong DB, KHÔNG HỀ BỊ XÓA!
        var stillInDb = await db.Appointments.Where(a => a.DoctorId == doctor.Id).ToListAsync();
        stillInDb.Should().HaveCount(2);
        stillInDb.Should().Contain(a => a.Id == apt1.Id);
        stillInDb.Should().Contain(a => a.Id == apt2.Id);
    }

    [Fact]
    public async Task CancelTimeOff_ShouldFreeUpSchedule()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-06", "BS. F");
        db.Doctors.Add(doctor);

        var day = DateTime.UtcNow.Date.AddDays(7);
        var timeOff = DoctorTimeOff.Create(doctor.Id, day.AddHours(8), day.AddHours(12), user.Id);
        db.DoctorTimeOffs.Add(timeOff);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);
        var handler = new TimeOffCommandHandlers(db, timeOffService);

        // Trước khi hủy: Có xung đột
        var beforeCancel = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(9), day.AddHours(10));
        beforeCancel.Should().BeTrue();

        // Act: Hủy ngày nghỉ
        var cancelResult = await handler.Handle(new CancelDoctorTimeOffCommand(timeOff.Id, user.Id, IsStaff: false), CancellationToken.None);
        cancelResult.Should().BeTrue();

        // Assert: Trạng thái đổi thành Cancelled và không còn xung đột
        var inDb = await db.DoctorTimeOffs.FindAsync(timeOff.Id);
        inDb!.Status.Should().Be(TimeOffStatus.Cancelled);

        var afterCancel = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(9), day.AddHours(10));
        afterCancel.Should().BeFalse();
    }

    [Fact]
    public async Task ScheduleBlock_CreateAndDeactivate_ShouldManageAvailability()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội", "noi");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-07", "BS. G");
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var timeOffService = new DoctorTimeOffService(db);
        var handler = new TimeOffCommandHandlers(db, timeOffService);

        var day = DateTime.UtcNow.Date.AddDays(8);
        var blockReq = new CreateDoctorScheduleBlockRequest(
            DoctorId: doctor.Id,
            StartAt: day.AddHours(13),
            EndAt: day.AddHours(14),
            BlockType: ScheduleBlockType.Break,
            Reason: "Nghỉ trưa/Ăn trưa"
        );

        // Act 1: Tạo Block
        var blockDto = await handler.Handle(new CreateDoctorScheduleBlockCommand(blockReq, user.Id, IsStaff: false), CancellationToken.None);
        blockDto.Should().NotBeNull();
        blockDto.IsActive.Should().BeTrue();

        // Khung giờ 13:00 - 14:00 bị khóa
        var isBlocked = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(13).AddMinutes(15), day.AddHours(13).AddMinutes(45));
        isBlocked.Should().BeTrue();

        // Act 2: Vô hiệu hóa Block
        var deactResult = await handler.Handle(new DeactivateDoctorScheduleBlockCommand(blockDto.Id, user.Id, IsStaff: false), CancellationToken.None);
        deactResult.Should().BeTrue();

        // Khung giờ được mở lại
        var isUnblocked = await timeOffService.HasTimeOffOrBlockConflictAsync(doctor.Id, day.AddHours(13).AddMinutes(15), day.AddHours(13).AddMinutes(45));
        isUnblocked.Should().BeFalse();
    }
}
