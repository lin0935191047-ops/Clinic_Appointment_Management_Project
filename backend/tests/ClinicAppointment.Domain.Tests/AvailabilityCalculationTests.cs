using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Features.Scheduling.Queries;
using ClinicAppointment.Application.Features.Scheduling.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class AvailabilityCalculationTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CalculateAvailability_WhenDoctorHasNoScheduleOnDate_ShouldReturnEmptySlots()
    {
        // Arrange: Bác sĩ chỉ làm việc vào Thứ Hai, kiểm tra tra cứu vào Thứ Ba
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-01", "BS. Nguyễn Văn A");
        db.Doctors.Add(doctor);

        // Thứ Hai (DayOfWeek = 1)
        var mondaySchedule = DoctorSchedule.Create(
            doctor.Id, 1, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.Add(mondaySchedule);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Ngày 17/11/2026 là Thứ Ba (DayOfWeek = 2)
        var tuesday = new DateOnly(2026, 11, 17);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, tuesday);

        // Assert
        result.Should().NotBeNull();
        result.DoctorId.Should().Be(doctor.Id);
        result.Date.Should().Be(tuesday);
        result.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAvailability_WithDefaultDuration_ShouldGenerateCorrectTimeSlots()
    {
        // Arrange: Ca làm việc Thứ Hai 08:00 - 10:00 (120 phút), mặc định 30 phút -> 4 slots
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-02", "BS. Trần Văn B");
        db.Doctors.Add(doctor);

        // Thứ Hai 08:00 - 10:00
        var schedule = DoctorSchedule.Create(
            doctor.Id, 1, new TimeOnly(8, 0), new TimeOnly(10, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);
        // Ngày Thứ Hai trong tương lai
        var monday = new DateOnly(2026, 11, 23);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, monday);

        // Assert
        result.Should().NotBeNull();
        result.DurationMinutes.Should().Be(30);
        result.Slots.Should().HaveCount(4);

        result.Slots[0].StartTime.Should().Be(new TimeOnly(8, 0));
        result.Slots[0].EndTime.Should().Be(new TimeOnly(8, 30));
        result.Slots[0].IsAvailable.Should().BeTrue();

        result.Slots[1].StartTime.Should().Be(new TimeOnly(8, 30));
        result.Slots[1].EndTime.Should().Be(new TimeOnly(9, 0));

        result.Slots[2].StartTime.Should().Be(new TimeOnly(9, 0));
        result.Slots[2].EndTime.Should().Be(new TimeOnly(9, 30));

        result.Slots[3].StartTime.Should().Be(new TimeOnly(9, 30));
        result.Slots[3].EndTime.Should().Be(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task CalculateAvailability_WithCustomServiceDuration_ShouldSliceCorrectly()
    {
        // Arrange: Dịch vụ thời lượng 45 phút, ca làm việc 08:00 - 10:00 (120 phút)
        // 08:00-08:45 (slot 1), 08:45-09:30 (slot 2). Khoảng 09:30-10:00 còn 30p (<45p) nên không sinh slot thứ 3
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-MAT", "Mắt", "mat");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-03", "BS. Lê Văn C");
        db.Doctors.Add(doctor);

        var specialtyService = Service.Create(spec.Id, "SRV-DO-KHUC-XA", "Đo khúc xạ nâng cao", durationMinutes: 45, price: 300000m);
        db.Services.Add(specialtyService);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 3, new TimeOnly(8, 0), new TimeOnly(10, 0), new DateOnly(2026, 1, 1)); // Thứ Tư
        db.DoctorSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);
        // Ngày 18/11/2026 là Thứ Tư
        var wednesday = new DateOnly(2026, 11, 18);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, wednesday, specialtyService.Id);

        // Assert
        result.Should().NotBeNull();
        result.DurationMinutes.Should().Be(45);
        result.ServiceName.Should().Be("Đo khúc xạ nâng cao");
        result.Slots.Should().HaveCount(2);

        result.Slots[0].StartTime.Should().Be(new TimeOnly(8, 0));
        result.Slots[0].EndTime.Should().Be(new TimeOnly(8, 45));

        result.Slots[1].StartTime.Should().Be(new TimeOnly(8, 45));
        result.Slots[1].EndTime.Should().Be(new TimeOnly(9, 30));
    }

    [Fact]
    public async Task CalculateAvailability_WhenSlotOverlapsDoctorTimeOff_ShouldBeMarkedUnavailable()
    {
        // Arrange: Ca 08:00 - 11:00 (6 slots 30p). Bác sĩ xin nghỉ từ 09:00 - 10:30
        // -> Slots 08:00-08:30 & 08:30-09:00 & 10:30-11:00 khả dụng.
        // -> Các slots trong khoảng 09:00 - 10:30 (09:00-09:30, 09:30-10:00, 10:00-10:30) KHÔNG khả dụng!
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-04", "BS. Hoàng Văn D");
        db.Doctors.Add(doctor);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 4, new TimeOnly(8, 0), new TimeOnly(11, 0), new DateOnly(2026, 1, 1)); // Thứ Năm
        db.DoctorSchedules.Add(schedule);

        // Ngày 19/11/2026 là Thứ Năm
        var thursday = new DateOnly(2026, 11, 19);

        // Bác sĩ xin nghỉ từ 09:00 đến 10:30 (giờ VN -> chuyển UTC)
        var timeOffStartUtc = TimeZoneHelper.ToUtc(thursday, new TimeOnly(9, 0));
        var timeOffEndUtc = TimeZoneHelper.ToUtc(thursday, new TimeOnly(10, 30));
        var timeOff = DoctorTimeOff.Create(doctor.Id, timeOffStartUtc, timeOffEndUtc, user.Id, "Đi họp bộ y tế");
        db.DoctorTimeOffs.Add(timeOff);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, thursday);

        // Assert
        result.Slots.Should().HaveCount(6);

        // Slot 1: 08:00 - 08:30 -> Khả dụng
        result.Slots[0].IsAvailable.Should().BeTrue();
        // Slot 2: 08:30 - 09:00 -> Khả dụng
        result.Slots[1].IsAvailable.Should().BeTrue();
        // Slot 3: 09:00 - 09:30 -> KHÔNG khả dụng do trùng TimeOff
        result.Slots[2].IsAvailable.Should().BeFalse();
        result.Slots[2].ReasonUnavailable.Should().Be("Bác sĩ có lịch nghỉ");
        // Slot 4: 09:30 - 10:00 -> KHÔNG khả dụng
        result.Slots[3].IsAvailable.Should().BeFalse();
        // Slot 5: 10:00 - 10:30 -> KHÔNG khả dụng
        result.Slots[4].IsAvailable.Should().BeFalse();
        // Slot 6: 10:30 - 11:00 -> Khả dụng
        result.Slots[5].IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateAvailability_WhenSlotOverlapsScheduleBlock_ShouldBeMarkedUnavailable()
    {
        // Arrange: Ca Thứ Sáu 08:00 - 12:00. Bác sĩ bận họp nội bộ từ 10:00 - 11:00
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-05", "BS. Ngô Văn E");
        db.Doctors.Add(doctor);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 5, new TimeOnly(8, 0), new TimeOnly(12, 0), new DateOnly(2026, 1, 1)); // Thứ Sáu
        db.DoctorSchedules.Add(schedule);

        var friday = new DateOnly(2026, 11, 20); // Thứ Sáu
        var blockStartUtc = TimeZoneHelper.ToUtc(friday, new TimeOnly(10, 0));
        var blockEndUtc = TimeZoneHelper.ToUtc(friday, new TimeOnly(11, 0));

        var block = DoctorScheduleBlock.Create(
            doctor.Id, blockStartUtc, blockEndUtc, user.Id, ScheduleBlockType.Meeting, "Họp chuyên môn");
        db.DoctorScheduleBlocks.Add(block);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, friday);

        // Assert
        var slot10_1030 = result.Slots.First(s => s.StartTime == new TimeOnly(10, 0));
        slot10_1030.IsAvailable.Should().BeFalse();
        slot10_1030.ReasonUnavailable.Should().Be("Khung giờ bận");

        var slot1030_1100 = result.Slots.First(s => s.StartTime == new TimeOnly(10, 30));
        slot1030_1100.IsAvailable.Should().BeFalse();
        slot1030_1100.ReasonUnavailable.Should().Be("Khung giờ bận");

        var slot1100_1130 = result.Slots.First(s => s.StartTime == new TimeOnly(11, 0));
        slot1100_1130.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateAvailability_WhenSlotOverlapsActiveAppointment_ShouldBeMarkedUnavailable()
    {
        // Arrange: Ca Thứ Bảy 08:00 - 10:00. Đã có ca khám Confirmed lúc 08:30 - 09:00
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-06", "BS. Vũ Văn F");
        db.Doctors.Add(doctor);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 6, new TimeOnly(8, 0), new TimeOnly(10, 0), new DateOnly(2026, 1, 1)); // Thứ Bảy
        db.DoctorSchedules.Add(schedule);

        var saturday = new DateOnly(2026, 11, 21); // Thứ Bảy
        var aptStartUtc = TimeZoneHelper.ToUtc(saturday, new TimeOnly(8, 30));

        var appointment = Appointment.CreateGuest(
            doctorId: doctor.Id,
            patientId: Guid.NewGuid(),
            serviceId: Guid.NewGuid(),
            serviceName: "Khám nội tổng quát",
            serviceDuration: 30,
            servicePrice: 150000m,
            startAtUtc: aptStartUtc,
            reasonForVisit: "Đau đầu"
        );
        appointment.Confirm();
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, saturday);

        // Assert
        var slot0800 = result.Slots.First(s => s.StartTime == new TimeOnly(8, 0));
        slot0800.IsAvailable.Should().BeTrue();

        var slot0830 = result.Slots.First(s => s.StartTime == new TimeOnly(8, 30));
        slot0830.IsAvailable.Should().BeFalse();
        slot0830.ReasonUnavailable.Should().Be("Đã có ca khám");

        var slot0900 = result.Slots.First(s => s.StartTime == new TimeOnly(9, 0));
        slot0900.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateAvailability_WhenAppointmentIsCancelled_SlotShouldBeAvailable()
    {
        // Arrange: Ca khám lúc 08:30 - 09:00 nhưng đã bị hủy (Status = Cancelled)
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-07", "BS. Đỗ Văn G");
        db.Doctors.Add(doctor);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 7, new TimeOnly(8, 0), new TimeOnly(10, 0), new DateOnly(2026, 1, 1)); // Chủ Nhật
        db.DoctorSchedules.Add(schedule);

        var sunday = new DateOnly(2026, 11, 22); // Chủ Nhật
        var aptStartUtc = TimeZoneHelper.ToUtc(sunday, new TimeOnly(8, 30));

        var appointment = Appointment.CreateGuest(
            doctorId: doctor.Id,
            patientId: Guid.NewGuid(),
            serviceId: Guid.NewGuid(),
            serviceName: "Khám nội",
            serviceDuration: 30,
            servicePrice: 150000m,
            startAtUtc: aptStartUtc,
            reasonForVisit: "Khám ho"
        );
        appointment.Status = AppointmentStatus.Cancelled; // Đã bị hủy
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, sunday);

        // Assert: Cuộc hẹn đã bị hủy sẽ giải phóng slot, slot 08:30 - 09:00 phải khả dụng!
        var slot0830 = result.Slots.First(s => s.StartTime == new TimeOnly(8, 30));
        slot0830.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateAvailability_WhenOnlyAvailableTrue_ShouldFilterOutUnavailableSlots()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-08", "BS. Bùi Văn H");
        db.Doctors.Add(doctor);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 1, new TimeOnly(8, 0), new TimeOnly(10, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.Add(schedule);

        var monday = new DateOnly(2026, 11, 23);
        // Khóa slot 09:00 - 09:30 bằng 1 cuộc hẹn
        var apt = Appointment.CreateGuest(
            doctor.Id, Guid.NewGuid(), Guid.NewGuid(), "Khám", 30, 100000m,
            TimeZoneHelper.ToUtc(monday, new TimeOnly(9, 0)), null);
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Act: Chỉ lấy các slot còn trống (onlyAvailable = true)
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, monday, onlyAvailable: true);

        // Assert: Tổng 4 slots, 1 slot bị chiếm -> Chỉ trả về 3 slots
        result.Slots.Should().HaveCount(3);
        result.Slots.Should().OnlyContain(s => s.IsAvailable);
        result.Slots.Should().NotContain(s => s.StartTime == new TimeOnly(9, 0));
    }

    [Fact]
    public async Task CalculateAvailability_MultipleShiftsSameDay_ShouldGenerateSlotsForBothShifts()
    {
        // Arrange: Bác sĩ có ca sáng 08:00 - 10:00 (4 slots) và ca chiều 14:00 - 16:00 (4 slots)
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-09", "BS. Phạm Văn I");
        db.Doctors.Add(doctor);

        var morning = DoctorSchedule.Create(
            doctor.Id, 2, new TimeOnly(8, 0), new TimeOnly(10, 0), new DateOnly(2026, 1, 1));
        var afternoon = DoctorSchedule.Create(
            doctor.Id, 2, new TimeOnly(14, 0), new TimeOnly(16, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.AddRange(morning, afternoon);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);
        var tuesday = new DateOnly(2026, 11, 24); // Thứ Ba

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, tuesday);

        // Assert: 4 slots sáng + 4 slots chiều = 8 slots
        result.Slots.Should().HaveCount(8);
        result.Slots.First().StartTime.Should().Be(new TimeOnly(8, 0));
        result.Slots.Last().EndTime.Should().Be(new TimeOnly(16, 0));
    }

    [Fact]
    public async Task CalculateAvailability_PrivacyVerification_ShouldNotLeakAnyPatientData()
    {
        // Acceptance criteria: Không lộ patient info (FR-030)
        using var db = CreateInMemoryDbContext();
        var user = User.Create("dr@clinic.vn", "hash");
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa");
        db.Users.Add(user);
        db.Specialties.Add(spec);
        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-10", "BS. Dương Văn K");
        db.Doctors.Add(doctor);

        var schedule = DoctorSchedule.Create(
            doctor.Id, 3, new TimeOnly(8, 0), new TimeOnly(9, 0), new DateOnly(2026, 1, 1));
        db.DoctorSchedules.Add(schedule);

        var wednesday = new DateOnly(2026, 11, 25);
        var patientSecretName = "Bệnh nhân có thông tin nhạy cảm";
        var appointment = Appointment.CreateGuest(
            doctor.Id, Guid.NewGuid(), Guid.NewGuid(), "Khám mật", 30, 500000m,
            TimeZoneHelper.ToUtc(wednesday, new TimeOnly(8, 0)), null);
        appointment.ServiceNameSnapshot = patientSecretName;
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var service = new AvailabilityService(db);

        // Act
        var result = await service.GetDoctorAvailabilityAsync(doctor.Id, wednesday);

        // Assert: DTO công khai không có bất kỳ trường nào chứa tên hay mã booking của bệnh nhân
        result.Slots.Should().HaveCount(2);
        var occupiedSlot = result.Slots[0];
        occupiedSlot.IsAvailable.Should().BeFalse();
        occupiedSlot.ReasonUnavailable.Should().Be("Đã có ca khám");
        // Kiểm tra kiểu TimeSlotDto hoàn toàn không có thuộc tính liên quan đến bệnh nhân
        typeof(ClinicAppointment.Application.Features.Scheduling.DTOs.TimeSlotDto).GetProperty("PatientName").Should().BeNull();
        typeof(ClinicAppointment.Application.Features.Scheduling.DTOs.TimeSlotDto).GetProperty("BookingCode").Should().BeNull();
    }
}
