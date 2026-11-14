using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class GuestBookingApiTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private BookGuestAppointmentCommandHandler CreateHandler(ApplicationDbContext db)
    {
        var duplicateService = new PatientDuplicateDetectionService();
        var abuseProtectionService = new AbuseProtectionService(new MemoryCache(new MemoryCacheOptions()));
        return new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtectionService);
    }

    private (User User, Specialty Specialty, Doctor Doctor, Service Service) SeedDoctorAndService(
        ApplicationDbContext db, 
        bool doctorActive = true, 
        bool specialtyActive = true, 
        bool serviceActive = true)
    {
        var user = User.Create("doctor@clinic.vn", "hash");
        var specialty = Specialty.Create("CK-NOI", "Khoa Nội Tổng Quát", "noi-tong-quat");
        if (!specialtyActive) specialty.SetStatus(false);

        db.Users.Add(user);
        db.Specialties.Add(specialty);

        var doctor = Doctor.Create(user.Id, specialty.Id, "DOC-001", "BS. Nguyễn Văn A");
        if (!doctorActive) doctor.SetStatus(false);

        db.Doctors.Add(doctor);

        var service = Service.Create(specialty.Id, "DV-KHAM", "Khám nội tổng quát", 30, 200000m);
        if (!serviceActive) service.SetStatus(false);

        db.Services.Add(service);

        // Lịch làm việc Thứ Hai - Thứ Sáu: 08:00 - 17:00 (giờ VN)
        for (short d = 1; d <= 5; d++)
        {
            var schedule = DoctorSchedule.Create(
                doctor.Id,
                d,
                new TimeOnly(8, 0),
                new TimeOnly(17, 0),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31));
            db.DoctorSchedules.Add(schedule);
        }

        db.SaveChanges();
        return (user, specialty, doctor, service);
    }

    [Fact]
    public async Task GuestBooking_Success_CreatesIndependentPatientWithoutUser()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        // Đặt lịch vào Thứ Hai 16/11/2026 lúc 09:00 sáng giờ VN (tức 02:00 UTC)
        var mondayDate = new DateOnly(2026, 11, 16);
        var startAtUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));

        var command = new BookGuestAppointmentCommand(
            FullName: "Nguyễn Văn Khách",
            PhoneNumber: "0912345678",
            DateOfBirth: new DateOnly(1995, 6, 20),
            BirthYear: null,
            Email: "guest@example.com",
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: startAtUtc,
            ReasonForVisit: "Đau đầu nhẹ");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.Should().NotBeNull();
        result.Value!.BookingCode.Should().StartWith("APT-");
        result.Value.PatientFullName.Should().Be("Nguyễn Văn Khách");
        result.Value.ServiceName.Should().Be(service.Name);
        result.Value.ServicePrice.Should().Be(200000m);
        result.Value.ServiceDuration.Should().Be(30);
        result.Value.Status.Should().Be(AppointmentStatus.PendingConfirmation);

        // Verify Database
        var savedPatient = await db.Patients.FirstOrDefaultAsync(p => p.FullName == "Nguyễn Văn Khách");
        savedPatient.Should().NotBeNull();
        savedPatient!.UserId.Should().BeNull("Hồ sơ bệnh nhân guest phải độc lập và UserId là null");
        savedPatient.PhoneNumber.Should().Be("0912345678");

        var savedAppointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == result.Value.AppointmentId);
        savedAppointment.Should().NotBeNull();
        savedAppointment!.IsGuest.Should().BeTrue();
        savedAppointment.PatientId.Should().Be(savedPatient.Id);
    }

    [Fact]
    public async Task GuestBooking_Generates_UniqueSecureBookingCode()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var date1 = new DateOnly(2026, 11, 16);
        var start1 = TimeZoneHelper.ToUtc(date1, new TimeOnly(9, 0));
        var start2 = TimeZoneHelper.ToUtc(date1, new TimeOnly(10, 0));

        var cmd1 = new BookGuestAppointmentCommand("Bệnh Nhân A", "0911111111", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, start1, null);
        var cmd2 = new BookGuestAppointmentCommand("Bệnh Nhân B", "0922222222", new DateOnly(1992, 2, 2), null, null, doctor.Id, service.Id, start2, null);

        // Act
        var res1 = await handler.Handle(cmd1, CancellationToken.None);
        var res2 = await handler.Handle(cmd2, CancellationToken.None);

        // Assert
        res1.IsSuccess.Should().BeTrue();
        res2.IsSuccess.Should().BeTrue();
        res1.Value!.BookingCode.Should().NotBe(res2.Value!.BookingCode);
        res1.Value.BookingCode.Should().MatchRegex(@"^APT-\d{8}-[A-Z0-9]{6}$");
    }

    [Fact]
    public async Task GuestBooking_Captures_ServicePriceAndDurationSnapshots()
    {
        // Arrange: Snapshot giữ nguyên ngay cả khi dịch vụ sau đó đổi giá/tên
        using var db = CreateInMemoryDbContext();
        var (_, specialty, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var date = new DateOnly(2026, 11, 16);
        var startAtUtc = TimeZoneHelper.ToUtc(date, new TimeOnly(9, 0));

        var cmd = new BookGuestAppointmentCommand("Trần Thị Snapshot", "0988776655", new DateOnly(1988, 8, 8), null, null, doctor.Id, service.Id, startAtUtc, null);

        // Act
        var res = await handler.Handle(cmd, CancellationToken.None);
        res.IsSuccess.Should().BeTrue();

        // Đổi giá và tên của dịch vụ
        service.Update(specialty.Id, "Khám Nội Chuyên Sâu Tăng Giá", 60, 500000m);
        await db.SaveChangesAsync();

        // Assert
        var appointmentInDb = await db.Appointments.FindAsync(res.Value!.AppointmentId);
        appointmentInDb.Should().NotBeNull();
        appointmentInDb!.ServiceNameSnapshot.Should().Be("Khám nội tổng quát");
        appointmentInDb.ServiceDurationSnapshot.Should().Be(30);
        appointmentInDb.ServicePriceSnapshot.Should().Be(200000m);
        appointmentInDb.PaidAmount.Should().Be(0.0m);
    }

    [Fact]
    public async Task GuestBooking_Rejects_WhenDoctorOrSpecialtyInactive()
    {
        // Arrange 1: Doctor inactive
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db, doctorActive: false);
        var handler = CreateHandler(db);

        var date = new DateOnly(2026, 11, 16);
        var startAtUtc = TimeZoneHelper.ToUtc(date, new TimeOnly(9, 0));

        var cmd = new BookGuestAppointmentCommand("Lê Văn C", "0912345678", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, startAtUtc, null);

        // Act 1
        var res1 = await handler.Handle(cmd, CancellationToken.None);

        // Assert 1
        res1.IsSuccess.Should().BeFalse();
        res1.StatusCode.Should().Be(404);
        res1.Error.Should().Contain("Bác sĩ không tồn tại hoặc đã ngừng nhận lịch");

        // Arrange 2: Specialty inactive
        using var db2 = CreateInMemoryDbContext();
        var (_, specialty2, doctor2, service2) = SeedDoctorAndService(db2, doctorActive: true, specialtyActive: false);
        var handler2 = CreateHandler(db2);

        // Act 2
        var res2 = await handler2.Handle(cmd with { DoctorId = doctor2.Id, ServiceId = service2.Id }, CancellationToken.None);

        // Assert 2
        res2.IsSuccess.Should().BeFalse();
        res2.StatusCode.Should().Be(400);
        res2.Error.Should().Contain("Chuyên khoa của bác sĩ hiện đang tạm ngưng hoạt động");
    }

    [Fact]
    public async Task GuestBooking_Rejects_WhenOutsideDoctorWorkingSchedule()
    {
        // Arrange: Bác sĩ làm 08:00 - 17:00 các ngày Thứ Hai - Thứ Sáu
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        // 1. Đặt vào ngày Chủ Nhật (22/11/2026) - không có ca làm việc
        var sundayDate = new DateOnly(2026, 11, 22);
        var sundayUtc = TimeZoneHelper.ToUtc(sundayDate, new TimeOnly(9, 0));
        var cmdSunday = new BookGuestAppointmentCommand("Người Khám", "0912345678", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, sundayUtc, null);

        var resSunday = await handler.Handle(cmdSunday, CancellationToken.None);
        resSunday.IsSuccess.Should().BeFalse();
        resSunday.StatusCode.Should().Be(400);
        resSunday.Error.Should().Contain("Khung giờ hẹn không nằm trong ca làm việc");

        // 2. Đặt vào Thứ Hai nhưng ngoài giờ: 07:00 sáng
        var mondayDate = new DateOnly(2026, 11, 16);
        var earlyUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(7, 30));
        var cmdEarly = cmdSunday with { StartAtUtc = earlyUtc };

        var resEarly = await handler.Handle(cmdEarly, CancellationToken.None);
        resEarly.IsSuccess.Should().BeFalse();
        resEarly.Error.Should().Contain("Khung giờ hẹn không nằm trong ca làm việc");
    }

    [Fact]
    public async Task GuestBooking_ThrowsConflict_WhenDoctorHasApprovedTimeOff()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (user, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var mondayDate = new DateOnly(2026, 11, 16);
        var timeOffStartUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));
        var timeOffEndUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(12, 0));

        var timeOff = DoctorTimeOff.Create(doctor.Id, timeOffStartUtc, timeOffEndUtc, user.Id, "Nghỉ phép cá nhân");
        db.DoctorTimeOffs.Add(timeOff);
        await db.SaveChangesAsync();

        // Khách đặt vào lúc 09:30 - 10:00 (nằm trong lịch nghỉ)
        var bookStartUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 30));
        var cmd = new BookGuestAppointmentCommand("Bệnh Nhân Nghỉ", "0912345678", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, bookStartUtc, null);

        // Act & Assert
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<AppointmentConflictException>()
            .WithMessage("*Bác sĩ có lịch nghỉ đã được duyệt*");
    }

    [Fact]
    public async Task GuestBooking_ThrowsConflict_WhenDoctorScheduleIsBlocked()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (user, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var mondayDate = new DateOnly(2026, 11, 16);
        var blockStartUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(10, 0));
        var blockEndUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(11, 0));

        var block = DoctorScheduleBlock.Create(doctor.Id, blockStartUtc, blockEndUtc, user.Id, ScheduleBlockType.Meeting, "Họp chuyên môn");
        db.DoctorScheduleBlocks.Add(block);
        await db.SaveChangesAsync();

        // Khách đặt vào lúc 10:00 (bị block)
        var cmd = new BookGuestAppointmentCommand("Bệnh Nhân Block", "0912345678", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, blockStartUtc, null);

        // Act & Assert
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<AppointmentConflictException>()
            .WithMessage("*Khung giờ này đã bị khóa hoặc bác sĩ có lịch bận*");
    }

    [Fact]
    public async Task GuestBooking_ThrowsConflict_WhenSlotAlreadyBooked()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var mondayDate = new DateOnly(2026, 11, 16);
        var startUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));

        var cmd1 = new BookGuestAppointmentCommand("Người 1", "0911111111", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, startUtc, null);
        var res1 = await handler.Handle(cmd1, CancellationToken.None);
        res1.IsSuccess.Should().BeTrue();

        // Người 2 cố gắng đặt cùng khung giờ 09:00 - 09:30
        var cmd2 = new BookGuestAppointmentCommand("Người 2", "0922222222", new DateOnly(1992, 2, 2), null, null, doctor.Id, service.Id, startUtc, null);

        // Act & Assert
        var act = async () => await handler.Handle(cmd2, CancellationToken.None);
        await act.Should().ThrowAsync<AppointmentConflictException>()
            .WithMessage("*Khung giờ đã có lịch hẹn khác*");
    }

    [Fact]
    public async Task GuestBooking_CancelledAppointment_DoesNotBlockNewBooking()
    {
        // Arrange: Cuộc hẹn trước bị hủy -> giải phóng slot cho người khác đặt
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var mondayDate = new DateOnly(2026, 11, 16);
        var startUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));

        var cmd1 = new BookGuestAppointmentCommand("Người 1 Hủy", "0911111111", new DateOnly(1990, 1, 1), null, null, doctor.Id, service.Id, startUtc, null);
        var res1 = await handler.Handle(cmd1, CancellationToken.None);
        res1.IsSuccess.Should().BeTrue();

        // Hủy cuộc hẹn 1
        var apt1 = await db.Appointments.FindAsync(res1.Value!.AppointmentId);
        apt1!.Cancel("Bận việc đột xuất", DateTime.UtcNow);
        await db.SaveChangesAsync();

        // Người 2 đặt lại đúng slot 09:00
        var cmd2 = new BookGuestAppointmentCommand("Người 2 Đặt Lại", "0922222222", new DateOnly(1992, 2, 2), null, null, doctor.Id, service.Id, startUtc, null);

        // Act
        var res2 = await handler.Handle(cmd2, CancellationToken.None);

        // Assert
        res2.IsSuccess.Should().BeTrue("Lịch đã hủy phải giải phóng slot để người khác đặt");
    }

    [Fact]
    public async Task GuestBooking_StrictlyNoAutoMerge_CreatesSeparatePatientAndOpensReviewCase_WhenDuplicateSuspected()
    {
        // Arrange: Đã có bệnh nhân cũ tên "Trần Văn An", sinh ngày 15/05/1990 trong hệ thống
        using var db = CreateInMemoryDbContext();
        var (_, _, doctor, service) = SeedDoctorAndService(db);
        var handler = CreateHandler(db);

        var existingPatient = Patient.Create("Trần Văn An", "0900000001", new DateOnly(1990, 5, 15), 1990, null, "an.old@example.com");
        db.Patients.Add(existingPatient);
        await db.SaveChangesAsync();

        var mondayDate = new DateOnly(2026, 11, 16);
        var startUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(14, 0));

        // Guest mới đặt lịch cũng tên "Trần Văn An", sinh ngày 15/05/1990, nhưng dùng số điện thoại khác
        var cmd = new BookGuestAppointmentCommand(
            FullName: "Trần Văn An",
            PhoneNumber: "0999888777",
            DateOfBirth: new DateOnly(1990, 5, 15),
            BirthYear: 1990,
            Email: "an.new@example.com",
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: startUtc,
            ReasonForVisit: "Khám lại định kỳ");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // 1. Invariant: Tuyệt đối không auto-merge! Phải tạo bệnh nhân mới riêng biệt
        var newPatient = await db.Patients.FirstOrDefaultAsync(p => p.PhoneNumber == "0999888777");
        newPatient.Should().NotBeNull();
        newPatient!.Id.Should().NotBe(existingPatient.Id, "Bệnh nhân mới không được tự động gộp vào bệnh nhân cũ");

        var appointment = await db.Appointments.FindAsync(result.Value!.AppointmentId);
        appointment!.PatientId.Should().Be(newPatient.Id);

        // 2. Invariant: Phải tạo PatientIdentityReviewCase trạng thái OPEN để lễ tân rà soát tại quầy
        var reviewCase = await db.PatientIdentityReviewCases
            .FirstOrDefaultAsync(c => c.PatientId == newPatient.Id && c.CandidatePatientId == existingPatient.Id);

        reviewCase.Should().NotBeNull("Phải tạo ca rà soát đối soát nghi vấn trùng hồ sơ");
        reviewCase!.Status.Should().Be(IdentityReviewStatus.OPEN);
        reviewCase.MatchReason.Should().Be(IdentityMatchReason.NAME_DOB);
    }

    [Fact]
    public void GuestBooking_Validation_RejectsMissingRequiredFields()
    {
        var validator = new BookGuestAppointmentCommandValidator();

        // Thiếu cả DateOfBirth và BirthYear
        var cmdMissingBirth = new BookGuestAppointmentCommand(
            "Nguyễn Văn A", "0912345678", null, null, null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(2), null);
        var resultBirth = validator.TestValidate(cmdMissingBirth);
        resultBirth.ShouldHaveValidationErrorFor(x => x);

        // Số điện thoại sai định dạng
        var cmdBadPhone = new BookGuestAppointmentCommand(
            "Nguyễn Văn A", "123456", new DateOnly(1990, 1, 1), null, null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(2), null);
        var resultPhone = validator.TestValidate(cmdBadPhone);
        resultPhone.ShouldHaveValidationErrorFor(x => x.PhoneNumber);

        // Giờ hẹn ở quá khứ
        var cmdPastTime = new BookGuestAppointmentCommand(
            "Nguyễn Văn A", "0912345678", new DateOnly(1990, 1, 1), null, null, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), null);
        var resultTime = validator.TestValidate(cmdPastTime);
        resultTime.ShouldHaveValidationErrorFor(x => x.StartAtUtc);
    }
}
