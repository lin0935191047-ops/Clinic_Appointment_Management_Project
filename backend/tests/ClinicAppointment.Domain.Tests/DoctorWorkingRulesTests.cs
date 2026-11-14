using ClinicAppointment.Application.Features.Schedules.Commands;
using ClinicAppointment.Application.Features.Schedules.DTOs;
using ClinicAppointment.Application.Features.Schedules.Queries;
using ClinicAppointment.Application.Features.Schedules.Validators;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class DoctorWorkingRulesTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateSchedule_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-01", "BS. Nguyễn Văn A", isActive: true);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);
        var request = new CreateDoctorScheduleRequest(
            DoctorId: doctor.Id,
            DayOfWeek: 1, // Thứ Hai
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            EffectiveFrom: new DateOnly(2026, 1, 1),
            EffectiveTo: new DateOnly(2026, 12, 31),
            SlotDurationMinutes: 30,
            IsActive: true
        );

        // Act
        var result = await handler.Handle(new CreateDoctorScheduleCommand(request), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DoctorId.Should().Be(doctor.Id);
        result.DoctorName.Should().Be("BS. Nguyễn Văn A");
        result.DoctorCode.Should().Be("DOC-01");
        result.DayOfWeek.Should().Be(1);
        result.DayOfWeekName.Should().Be("Thứ Hai");
        result.StartTime.Should().Be(new TimeOnly(8, 0));
        result.EndTime.Should().Be(new TimeOnly(12, 0));
        result.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
        result.EffectiveTo.Should().Be(new DateOnly(2026, 12, 31));
        result.SlotDurationMinutes.Should().Be(30);
        result.IsActive.Should().BeTrue();

        var inDb = await db.DoctorSchedules.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public void Validator_ShouldRejectStartTimeGreaterThanOrEqualToEndTime()
    {
        var validator = new CreateDoctorScheduleRequestValidator();

        // Start == End
        var reqEqual = new CreateDoctorScheduleRequest(
            DoctorId: Guid.NewGuid(),
            DayOfWeek: 2,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(10, 0),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );
        validator.TestValidate(reqEqual)
            .ShouldHaveValidationErrorFor(x => x.StartTime);

        // Start > End
        var reqGreater = new CreateDoctorScheduleRequest(
            DoctorId: Guid.NewGuid(),
            DayOfWeek: 2,
            StartTime: new TimeOnly(11, 0),
            EndTime: new TimeOnly(9, 0),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );
        validator.TestValidate(reqGreater)
            .ShouldHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void Validator_ShouldRejectInvalidDayOfWeekAndEffectiveDates()
    {
        var validator = new CreateDoctorScheduleRequestValidator();

        // DayOfWeek < 1 hoặc > 7
        var reqInvalidDay = new CreateDoctorScheduleRequest(
            DoctorId: Guid.NewGuid(),
            DayOfWeek: 0,
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );
        validator.TestValidate(reqInvalidDay)
            .ShouldHaveValidationErrorFor(x => x.DayOfWeek);

        // EffectiveTo < EffectiveFrom
        var reqInvalidDates = new CreateDoctorScheduleRequest(
            DoctorId: Guid.NewGuid(),
            DayOfWeek: 3,
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            EffectiveFrom: new DateOnly(2026, 12, 31),
            EffectiveTo: new DateOnly(2026, 1, 1)
        );
        validator.TestValidate(reqInvalidDates)
            .ShouldHaveValidationErrorFor(x => x.EffectiveFrom);
    }

    [Fact]
    public void DoctorScheduleEntity_ShouldEnforceInvariants()
    {
        var doctorId = Guid.NewGuid();

        // StartTime >= EndTime
        var actTime = () => DoctorSchedule.Create(
            doctorId, 1, new TimeOnly(14, 0), new TimeOnly(13, 0), new DateOnly(2026, 1, 1));
        actTime.Should().Throw<ArgumentException>()
            .WithMessage("*trước giờ kết thúc*");

        // DayOfWeek out of range 1..7
        var actDay = () => DoctorSchedule.Create(
            doctorId, 8, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        actDay.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Thứ trong tuần phải từ 1*");

        // EffectiveTo < EffectiveFrom
        var actDates = () => DoctorSchedule.Create(
            doctorId, 1, new TimeOnly(8, 0), new TimeOnly(12, 0),
            new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 1));
        actDates.Should().Throw<ArgumentException>()
            .WithMessage("*không được lớn hơn ngày kết thúc*");
    }

    [Fact]
    public async Task CreateSchedule_WhenDoctorInactiveOrNotFound_ShouldThrowInvalidOperationException()
    {
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội", "noi", isActive: true);
        db.Specialties.Add(specialty);
        var inactiveDoctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-INACTIVE", "BS. Nghỉ việc", isActive: false);
        db.Doctors.Add(inactiveDoctor);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);

        // Bác sĩ Inactive
        var reqInactive = new CreateDoctorScheduleRequest(
            DoctorId: inactiveDoctor.Id,
            DayOfWeek: 1,
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );
        var actInactive = async () => await handler.Handle(new CreateDoctorScheduleCommand(reqInactive), CancellationToken.None);
        await actInactive.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tạm ngừng hoạt động*");

        // Bác sĩ Không tồn tại
        var reqNotFound = new CreateDoctorScheduleRequest(
            DoctorId: Guid.NewGuid(),
            DayOfWeek: 1,
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );
        var actNotFound = async () => await handler.Handle(new CreateDoctorScheduleCommand(reqNotFound), CancellationToken.None);
        await actNotFound.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không tồn tại*");
    }

    [Fact]
    public async Task CreateSchedule_WhenOverlappingSameDoctorSameDayAndActiveDates_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-TIM", "Tim Mạch", "tim-mach", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-TIM-01", "BS. Trần Văn B", isActive: true);
        db.Doctors.Add(doctor);

        // Ca 1: Thứ Hai, 08:00 - 12:00, hiệu lực cả năm 2026
        var schedule1 = DoctorSchedule.Create(
            doctor.Id, 1, new TimeOnly(8, 0), new TimeOnly(12, 0),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        db.DoctorSchedules.Add(schedule1);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);

        // Ca 2: Thứ Hai, 10:00 - 14:00 (chồng lấn từ 10:00 đến 12:00) -> PHẢI BỊ TỪ CHỐI
        var overlapReq = new CreateDoctorScheduleRequest(
            DoctorId: doctor.Id,
            DayOfWeek: 1,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(14, 0),
            EffectiveFrom: new DateOnly(2026, 6, 1),
            EffectiveTo: new DateOnly(2026, 8, 31)
        );

        // Act
        var act = async () => await handler.Handle(new CreateDoctorScheduleCommand(overlapReq), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chồng lấn với ca làm việc hiện có (08:00 - 12:00)*");
    }

    [Fact]
    public async Task CreateSchedule_MorningAndAfternoonShifts_SameDay_ShouldSucceed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-02", "BS. Lê Văn C", isActive: true);
        db.Doctors.Add(doctor);

        // Ca sáng: 08:00 - 12:00
        var morning = DoctorSchedule.Create(
            doctor.Id, 2, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.Add(morning);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);

        // Ca chiều: 13:30 - 17:30 (cùng Thứ Ba, không giao nhau với ca sáng) -> HỢP LỆ
        var afternoonReq = new CreateDoctorScheduleRequest(
            DoctorId: doctor.Id,
            DayOfWeek: 2,
            StartTime: new TimeOnly(13, 30),
            EndTime: new TimeOnly(17, 30),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );

        // Act
        var result = await handler.Handle(new CreateDoctorScheduleCommand(afternoonReq), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().Be(new TimeOnly(13, 30));
        result.EndTime.Should().Be(new TimeOnly(17, 30));

        var allSchedules = await db.DoctorSchedules.Where(s => s.DoctorId == doctor.Id).ToListAsync();
        allSchedules.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSchedule_SameTime_DifferentNonOverlappingEffectiveDates_ShouldSucceed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-03", "BS. Hoàng Văn D", isActive: true);
        db.Doctors.Add(doctor);

        // Ca 1: Thứ Sáu 08:00 - 12:00, hiệu lực 6 tháng đầu năm (01/01/2026 - 30/06/2026)
        var term1 = DoctorSchedule.Create(
            doctor.Id, 5, new TimeOnly(8, 0), new TimeOnly(12, 0),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        db.DoctorSchedules.Add(term1);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);

        // Ca 2: Cùng Thứ Sáu 08:00 - 12:00, nhưng hiệu lực 6 tháng cuối năm (01/07/2026 - 31/12/2026) -> HỢP LỆ
        var term2Req = new CreateDoctorScheduleRequest(
            DoctorId: doctor.Id,
            DayOfWeek: 5,
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            EffectiveFrom: new DateOnly(2026, 7, 1),
            EffectiveTo: new DateOnly(2026, 12, 31)
        );

        // Act
        var result = await handler.Handle(new CreateDoctorScheduleCommand(term2Req), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EffectiveFrom.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task UpdateSchedule_WhenIntroducingOverlap_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-04", "BS. Vũ Thị E", isActive: true);
        db.Doctors.Add(doctor);

        var morning = DoctorSchedule.Create(doctor.Id, 3, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        var afternoon = DoctorSchedule.Create(doctor.Id, 3, new TimeOnly(13, 0), new TimeOnly(17, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.AddRange(morning, afternoon);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);

        // Cập nhật ca chiều thành 11:30 - 15:30 -> Chồng lấn với ca sáng (8:00 - 12:00)
        var updateReq = new UpdateDoctorScheduleRequest(
            DayOfWeek: 3,
            StartTime: new TimeOnly(11, 30),
            EndTime: new TimeOnly(15, 30),
            EffectiveFrom: new DateOnly(2026, 1, 1)
        );

        // Act
        var act = async () => await handler.Handle(new UpdateDoctorScheduleCommand(afternoon.Id, updateReq), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chồng lấn với ca làm việc hiện có*");
    }

    [Fact]
    public async Task SetScheduleStatus_WhenReactivatingOverlappingSchedule_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-05", "BS. Ngô Văn F", isActive: true);
        db.Doctors.Add(doctor);

        var activeShift = DoctorSchedule.Create(doctor.Id, 4, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1), isActive: true);
        var inactiveShift = DoctorSchedule.Create(doctor.Id, 4, new TimeOnly(9, 0), new TimeOnly(13, 0), new DateOnly(2026, 1, 1), isActive: false);
        db.DoctorSchedules.AddRange(activeShift, inactiveShift);
        await db.SaveChangesAsync();

        var handler = new DoctorScheduleCommandHandlers(db);

        // Act: Kích hoạt lại ca inactive đang bị trùng giờ với activeShift
        var act = async () => await handler.Handle(new SetDoctorScheduleStatusCommand(inactiveShift.Id, true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không thể kích hoạt lại ca làm việc do bị chồng lấn*");
    }

    [Fact]
    public async Task QuerySchedules_WithEffectiveDateFilter_ShouldReturnOnlyEffectiveSchedules()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        var doctor = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-06", "BS. Đặng Văn G", isActive: true);
        db.Doctors.Add(doctor);

        // Ca 1: Hết hạn ngày 31/03/2026
        var past = DoctorSchedule.Create(doctor.Id, 1, new TimeOnly(8, 0), new TimeOnly(12, 0),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
        // Ca 2: Bắt đầu từ 01/04/2026 đến hết năm
        var future = DoctorSchedule.Create(doctor.Id, 1, new TimeOnly(8, 30), new TimeOnly(11, 30),
            new DateOnly(2026, 4, 1), new DateOnly(2026, 12, 31));

        db.DoctorSchedules.AddRange(past, future);
        await db.SaveChangesAsync();

        var queryHandler = new DoctorScheduleQueryHandlers(db);

        // Act: Tra cứu lịch hiệu lực vào ngày 15/05/2026
        var result = await queryHandler.Handle(new GetDoctorSchedulesQuery(doctor.Id, EffectiveDate: new DateOnly(2026, 5, 15)), CancellationToken.None);

        // Assert: Chỉ có ca future (bắt đầu từ 01/04/2026) được trả về
        result.Should().HaveCount(1);
        result.First().StartTime.Should().Be(new TimeOnly(8, 30));
        result.First().EndTime.Should().Be(new TimeOnly(11, 30));
    }

    [Fact]
    public async Task PublicDoctorSchedules_ShouldOnlyReturnActiveDoctorAndSpecialtySchedules()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var activeSpecialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        var inactiveSpecialty = Specialty.Create("CK-CU", "Chuyên khoa cũ", "ck-cu", isActive: false);
        db.Specialties.AddRange(activeSpecialty, inactiveSpecialty);

        var activeDoc = Doctor.Create(Guid.NewGuid(), activeSpecialty.Id, "DOC-ACT", "BS. Hoạt Động", isActive: true);
        var inactiveDoc = Doctor.Create(Guid.NewGuid(), activeSpecialty.Id, "DOC-INACT", "BS. Nghỉ Dài Hạn", isActive: false);
        var docInInactiveSpec = Doctor.Create(Guid.NewGuid(), inactiveSpecialty.Id, "DOC-SPEC-INACT", "BS. Khoa Cũ", isActive: true);
        db.Doctors.AddRange(activeDoc, inactiveDoc, docInInactiveSpec);

        var sched1 = DoctorSchedule.Create(activeDoc.Id, 1, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        var sched2 = DoctorSchedule.Create(inactiveDoc.Id, 1, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        var sched3 = DoctorSchedule.Create(docInInactiveSpec.Id, 1, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.AddRange(sched1, sched2, sched3);
        await db.SaveChangesAsync();

        var queryHandler = new DoctorScheduleQueryHandlers(db);

        // Act 1: Bác sĩ Active trong Chuyên khoa Active -> Trả về lịch
        var resActive = await queryHandler.Handle(new GetPublicDoctorSchedulesQuery(activeDoc.Id), CancellationToken.None);
        resActive.Should().HaveCount(1);

        // Act 2: Bác sĩ Inactive -> Không trả về
        var resInactiveDoc = await queryHandler.Handle(new GetPublicDoctorSchedulesQuery(inactiveDoc.Id), CancellationToken.None);
        resInactiveDoc.Should().BeEmpty();

        // Act 3: Bác sĩ trong Chuyên khoa Inactive -> Không trả về
        var resInactiveSpec = await queryHandler.Handle(new GetPublicDoctorSchedulesQuery(docInInactiveSpec.Id), CancellationToken.None);
        resInactiveSpec.Should().BeEmpty();
    }
}
