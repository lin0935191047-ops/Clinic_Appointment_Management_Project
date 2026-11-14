using System.Reflection;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Reporting.DTOs;
using ClinicAppointment.Application.Features.Reporting.Queries.GetDoctorDashboard;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class DoctorDashboardTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string? Email { get; set; } = "doctor@clinic.vn";
        public bool IsAuthenticated => UserId.HasValue;
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { Role.DoctorCode };
        public bool IsAdmin => IsInRole(Role.AdminCode);
        public bool IsStaff => IsInRole(Role.AdminCode) || IsInRole(Role.ReceptionistCode);
        public bool IsDoctor => IsInRole(Role.DoctorCode);
        public bool IsPatient => IsInRole(Role.PatientCode);
        public Guid? PatientProfileId { get; set; }
        public Guid? DoctorProfileId { get; set; }

        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private (Specialty SpecA, Doctor DocA, Doctor DocB, Patient Patient, Service ServiceA) SeedData(ApplicationDbContext db)
    {
        var specA = Specialty.Create("CK-NOI", "Nội khoa tổng quát", "noi-tong-quat");
        db.Specialties.Add(specA);

        var userA = User.Create("dr.an@clinic.vn", "hash");
        var userB = User.Create("dr.binh@clinic.vn", "hash");
        db.Users.AddRange(userA, userB);

        var docA = Doctor.Create(userA.Id, specA.Id, "DOC-01", "BS. Nguyễn Văn An");
        docA.RoomNumber = "Phòng 102";

        var docB = Doctor.Create(userB.Id, specA.Id, "DOC-02", "BS. Lê Văn Bình");
        docB.RoomNumber = "Phòng 103";

        db.Doctors.AddRange(docA, docB);

        var svcA = Service.Create(specA.Id, "DV-01", "Khám tổng quát", 30, 200000m);
        db.Services.Add(svcA);

        var patient = Patient.Create("Hoàng Minh Tuấn", "0987654321", new DateOnly(1995, 5, 10), 1995);
        db.Patients.Add(patient);

        db.SaveChanges();
        return (specA, docA, docB, patient, svcA);
    }

    [Fact]
    public async Task DoctorDashboard_TodaySchedule_ReturnsCorrectMetricsAndAppointments()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (specA, docA, docB, patient, svcA) = SeedData(db);

        var todayUtc = DateTime.UtcNow.Date.AddHours(2); // Giờ UTC tương ứng sáng hôm nay ở VN (+7 -> 9:00 AM)

        // 1. PendingConfirmation
        var aptPending = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(1), "Lý do 1");

        // 2. Confirmed
        var aptConfirmed = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(2), "Lý do 2");
        aptConfirmed.Confirm();

        // 3. CheckedIn
        var aptCheckedIn = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(3), "Lý do 3");
        aptCheckedIn.Confirm();
        aptCheckedIn.CheckIn();

        // 4. InProgress (có Consultation lâm sàng)
        var aptInProgress = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(4), "Lý do 4");
        aptInProgress.Confirm();
        aptInProgress.CheckIn();
        aptInProgress.StartConsultation();

        var consultInProgress = Consultation.Create(aptInProgress.Id, docA.Id, "Đau đầu nhẹ", "Theo dõi viêm xoang");
        db.Consultations.Add(consultInProgress);

        // 5. Completed (có Consultation)
        var aptCompleted = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(5), "Lý do 5");
        aptCompleted.Confirm();
        aptCompleted.CheckIn();
        aptCompleted.StartConsultation();
        aptCompleted.CompleteConsultation();

        var consultCompleted = Consultation.Create(
            appointmentId: aptCompleted.Id,
            doctorId: docA.Id,
            symptoms: "Sốt nhẹ",
            diagnosis: "Cảm cúm thông thường",
            examinationNotes: null,
            conclusion: "Nghỉ ngơi 3 ngày",
            followUpInstructions: "Uống nhiều nước"
        );
        db.Consultations.Add(consultCompleted);

        // 6. Cancelled
        var aptCancelled = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(6), "Lý do 6");
        aptCancelled.Confirm();
        aptCancelled.Cancel("Bận đột xuất", todayUtc.AddHours(5));

        db.Appointments.AddRange(aptPending, aptConfirmed, aptCheckedIn, aptInProgress, aptCompleted, aptCancelled);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            UserId = docA.UserId,
            Roles = new List<string> { Role.DoctorCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, currentUser, maskingService);

        var todayVn = DateOnly.FromDateTime(todayUtc.AddHours(7));
        var query = new GetDoctorDashboardQuery(Date: todayVn);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dashboard = result.Value!;

        dashboard.DoctorId.Should().Be(docA.Id);
        dashboard.DoctorName.Should().Be(docA.FullName);
        dashboard.SpecialtyName.Should().Be(specA.Name);
        dashboard.RoomNumber.Should().Be("Phòng 102");
        dashboard.Date.Should().Be(todayVn);

        // 1. Kiểm tra Metrics
        var m = dashboard.Metrics;
        m.TotalAppointments.Should().Be(6);
        m.PendingConfirmationCount.Should().Be(1);
        m.ConfirmedCount.Should().Be(1);
        m.CheckedInCount.Should().Be(1);
        m.InProgressCount.Should().Be(1);
        m.CompletedCount.Should().Be(1);
        m.CancelledCount.Should().Be(1);
        m.NoShowCount.Should().Be(0);
        m.CompletionRate.Should().Be(Math.Round(1.0 / 6 * 100, 2)); // 16.67%

        // 2. Kiểm tra WaitingQueue (Chỉ gồm các ca CheckedIn đang chờ khám)
        dashboard.WaitingQueue.Should().ContainSingle();
        dashboard.WaitingQueue[0].AppointmentId.Should().Be(aptCheckedIn.Id);
        dashboard.WaitingQueue[0].Status.Should().Be(AppointmentStatus.CheckedIn);

        // 3. Kiểm tra CurrentActiveAppointment (Ca InProgress)
        dashboard.CurrentActiveAppointment.Should().NotBeNull();
        dashboard.CurrentActiveAppointment!.AppointmentId.Should().Be(aptInProgress.Id);
        dashboard.CurrentActiveAppointment.Status.Should().Be(AppointmentStatus.InProgress);
        dashboard.CurrentActiveAppointment.HasConsultation.Should().BeTrue();

        // 4. Kiểm tra TodayAppointments và che số điện thoại (Masking)
        dashboard.TodayAppointments.Should().HaveCount(6);
        foreach (var item in dashboard.TodayAppointments)
        {
            item.MaskedPhoneNumber.Should().Be("098***4321");
            item.LocalStartTime.Should().NotBeNullOrEmpty();
            item.LocalEndTime.Should().NotBeNullOrEmpty();
            item.QueueNumber.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task DoctorDashboard_AssignedScopeEnforced_DoctorCanOnlyViewOwnAppointments()
    {
        // Arrange: Có ca khám của Bác sĩ A và Bác sĩ B trong cùng ngày
        using var db = CreateInMemoryDbContext();
        var (specA, docA, docB, patient, svcA) = SeedData(db);

        var todayUtc = DateTime.UtcNow.Date.AddHours(2);

        // Ca của Doc A
        var aptA = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(1), "Ca Doc A");
        aptA.Confirm();

        // Ca của Doc B
        var aptB = Appointment.CreateGuest(docB.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(2), "Ca Doc B");
        aptB.Confirm();

        db.Appointments.AddRange(aptA, aptB);
        await db.SaveChangesAsync();

        // Bác sĩ A đăng nhập và cố tình truyền DoctorId của Bác sĩ B
        var currentUserA = new TestCurrentUserService
        {
            UserId = docA.UserId,
            Roles = new List<string> { Role.DoctorCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, currentUserA, maskingService);

        var query = new GetDoctorDashboardQuery(DoctorId: docB.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Luôn chỉ trả về của Bác sĩ A (Chống IDOR & Assigned Scope Enforced)
        result.IsSuccess.Should().BeTrue();
        var dashboard = result.Value!;
        dashboard.DoctorId.Should().Be(docA.Id);
        dashboard.DoctorName.Should().Be(docA.FullName);
        dashboard.TodayAppointments.Should().ContainSingle(a => a.AppointmentId == aptA.Id);
        dashboard.TodayAppointments.Should().NotContain(a => a.AppointmentId == aptB.Id);
        dashboard.Metrics.TotalAppointments.Should().Be(1);
    }

    [Fact]
    public async Task DoctorDashboard_FilterByDifferentDate_ReturnsAppointmentsForThatDate()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (_, docA, _, patient, svcA) = SeedData(db);

        var todayUtc = DateTime.UtcNow.Date.AddHours(2);
        var tomorrowUtc = todayUtc.AddDays(1);

        var aptToday = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(1), "Hôm nay");
        var aptTomorrow = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, tomorrowUtc.AddHours(1), "Ngày mai");

        db.Appointments.AddRange(aptToday, aptTomorrow);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            UserId = docA.UserId,
            Roles = new List<string> { Role.DoctorCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, currentUser, maskingService);

        var tomorrowVn = DateOnly.FromDateTime(tomorrowUtc.AddHours(7));
        var query = new GetDoctorDashboardQuery(Date: tomorrowVn);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Chỉ lấy ca ngày mai
        result.IsSuccess.Should().BeTrue();
        result.Value!.TodayAppointments.Should().ContainSingle(a => a.AppointmentId == aptTomorrow.Id);
        result.Value.Metrics.TotalAppointments.Should().Be(1);
    }

    [Fact]
    public async Task DoctorDashboard_AdminCanViewSpecificDoctorDashboard()
    {
        // Arrange: Quản trị viên truy vấn dashboard của Bác sĩ B
        using var db = CreateInMemoryDbContext();
        var (_, docA, docB, patient, svcA) = SeedData(db);

        var todayUtc = DateTime.UtcNow.Date.AddHours(2);
        var aptB = Appointment.CreateGuest(docB.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, todayUtc.AddHours(2), "Ca Doc B");
        aptB.Confirm();

        db.Appointments.Add(aptB);
        await db.SaveChangesAsync();

        var adminUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.AdminCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, adminUser, maskingService);

        var query = new GetDoctorDashboardQuery(DoctorId: docB.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DoctorId.Should().Be(docB.Id);
        result.Value.DoctorName.Should().Be(docB.FullName);
        result.Value.TodayAppointments.Should().ContainSingle(a => a.AppointmentId == aptB.Id);
    }

    [Fact]
    public async Task DoctorDashboard_AdminWithoutDoctorId_ReturnsFirstDoctorDashboard()
    {
        // Arrange: Quản trị viên không truyền DoctorId, hệ thống tự động trả về bác sĩ đầu tiên
        using var db = CreateInMemoryDbContext();
        var (_, docA, _, _, _) = SeedData(db);

        var adminUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.AdminCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, adminUser, maskingService);

        var query = new GetDoctorDashboardQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DoctorId.Should().Be(docA.Id);
        result.Value.DoctorName.Should().Be(docA.FullName);
    }

    [Fact]
    public async Task DoctorDashboard_NonDoctorNonAdmin_ThrowsForbidden()
    {
        // Arrange: User có role Receptionist hoặc Patient
        using var db = CreateInMemoryDbContext();
        var receptionistUser = new TestCurrentUserService
        {
            Roles = new List<string> { Role.ReceptionistCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, receptionistUser, maskingService);

        var query = new GetDoctorDashboardQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bác sĩ hoặc Quản trị viên");
    }

    [Fact]
    public async Task DoctorDashboard_DoctorWithoutProfile_ThrowsForbidden()
    {
        // Arrange: Có role Doctor nhưng chưa tạo hồ sơ Doctor trong DB
        using var db = CreateInMemoryDbContext();
        var orphanDoctorUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.DoctorCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, orphanDoctorUser, maskingService);

        var query = new GetDoctorDashboardQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Không tìm thấy hồ sơ Bác sĩ");
    }

    [Fact]
    public async Task DoctorDashboard_UnauthenticatedUser_ThrowsUnauthorized()
    {
        // Arrange: Chưa đăng nhập (UserId = null)
        using var db = CreateInMemoryDbContext();
        var unauthenticatedUser = new TestCurrentUserService
        {
            UserId = null,
            Roles = new List<string> { Role.DoctorCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, unauthenticatedUser, maskingService);

        var query = new GetDoctorDashboardQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task DoctorDashboard_EmptyDay_HandlesZeroSafely()
    {
        // Arrange: Bác sĩ không có lịch hẹn nào hôm nay
        using var db = CreateInMemoryDbContext();
        var (_, docA, _, _, _) = SeedData(db);

        var currentUser = new TestCurrentUserService
        {
            UserId = docA.UserId,
            Roles = new List<string> { Role.DoctorCode }
        };
        var maskingService = new IdentifierMaskingService();
        var handler = new GetDoctorDashboardQueryHandler(db, currentUser, maskingService);

        var query = new GetDoctorDashboardQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        d.Metrics.TotalAppointments.Should().Be(0);
        d.Metrics.CompletionRate.Should().Be(0.0);
        d.TodayAppointments.Should().BeEmpty();
        d.WaitingQueue.Should().BeEmpty();
        d.CurrentActiveAppointment.Should().BeNull();
    }

    [Fact]
    public void DoctorDashboardController_ProtectedWithStaffOrDoctorPolicy()
    {
        // Arrange & Act
        var controllerType = typeof(DoctorDashboardController);
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authAttr.Should().NotBeNull();
        authAttr!.Policy.Should().Be("StaffOrDoctor");
    }
}
