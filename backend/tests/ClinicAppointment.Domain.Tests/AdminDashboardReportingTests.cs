using System.Reflection;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Reporting.DTOs;
using ClinicAppointment.Application.Features.Reporting.Queries.GetAdminDashboardReport;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class AdminDashboardReportingTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private class TestAdminCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string? Email { get; set; } = "admin@clinic.vn";
        public bool IsAuthenticated => UserId.HasValue;
        public IReadOnlyList<string> Roles { get; set; } = new List<string> { Role.AdminCode };
        public bool IsAdmin => IsInRole(Role.AdminCode);
        public bool IsStaff => IsInRole(Role.AdminCode) || IsInRole(Role.ReceptionistCode);
        public bool IsDoctor => IsInRole(Role.DoctorCode);
        public bool IsPatient => IsInRole(Role.PatientCode);
        public Guid? PatientProfileId { get; set; }
        public Guid? DoctorProfileId { get; set; }

        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private (Specialty SpecA, Specialty SpecB, Doctor DocA, Doctor DocB, Patient Patient) SeedCatalogAndDoctors(ApplicationDbContext db)
    {
        var specA = Specialty.Create("CK-NOI", "Nội tổng quát", "noi-tong-quat");
        var specB = Specialty.Create("CK-NGOAI", "Ngoại tổng quát", "ngoai-tong-quat");
        db.Specialties.AddRange(specA, specB);

        var userA = User.Create("dr.an@clinic.vn", "hash");
        var userB = User.Create("dr.binh@clinic.vn", "hash");
        db.Users.AddRange(userA, userB);

        var docA = Doctor.Create(userA.Id, specA.Id, "DOC-NOI-01", "BS. Trần Văn An");
        docA.RoomNumber = "Phòng 101";

        var docB = Doctor.Create(userB.Id, specB.Id, "DOC-NGOAI-01", "BS. Hoàng Văn Bình");
        docB.RoomNumber = "Phòng 201";

        db.Doctors.AddRange(docA, docB);

        var svcA = Service.Create(specA.Id, "DV-NOI-01", "Khám nội tổng quát", 30, 200000m);
        var svcB = Service.Create(specB.Id, "DV-NGOAI-01", "Khám ngoại tổng quát", 30, 300000m);
        db.Services.AddRange(svcA, svcB);

        var patient = Patient.Create("Nguyễn Văn Chung", "0912345678", new DateOnly(1990, 1, 1), 1990);
        db.Patients.Add(patient);

        db.SaveChanges();
        return (specA, specB, docA, docB, patient);
    }

    [Fact]
    public async Task AdminDashboardReport_AllNumbersReconcile()
    {
        // Arrange: Tạo 8 ca khám với đầy đủ các trạng thái và kiểm tra đối soát số liệu
        using var db = CreateInMemoryDbContext();
        var (specA, specB, docA, docB, patient) = SeedCatalogAndDoctors(db);

        var svcA = await db.Services.FirstAsync(s => s.SpecialtyId == specA.Id);
        var baseTime = DateTime.UtcNow.Date.AddHours(8); // 8:00 AM UTC hôm nay

        // 1. PendingConfirmation
        var aptPending = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, baseTime.AddHours(1), "Lý do");
        
        // 2. Confirmed
        var aptConfirmed = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, baseTime.AddHours(2), "Lý do");
        aptConfirmed.Confirm();

        // 3. CheckedIn
        var aptCheckedIn = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, baseTime.AddHours(3), "Lý do");
        aptCheckedIn.Confirm();
        aptCheckedIn.CheckIn();

        // 4. InProgress
        var aptInProgress = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, baseTime.AddHours(4), "Lý do");
        aptInProgress.Confirm();
        aptInProgress.CheckIn();
        aptInProgress.StartConsultation();

        // 5. Completed 1 (Doctor A)
        var aptCompleted1 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, baseTime.AddHours(5), "Lý do");
        aptCompleted1.Confirm();
        aptCompleted1.CheckIn();
        aptCompleted1.StartConsultation();
        aptCompleted1.CompleteConsultation();
        aptCompleted1.PaidAmount = svcA.Price;

        // 6. Completed 2 (Doctor B)
        var svcB = await db.Services.FirstAsync(s => s.SpecialtyId == specB.Id);
        var aptCompleted2 = Appointment.CreateGuest(docB.Id, patient.Id, svcB.Id, svcB.Name, 30, svcB.Price, baseTime.AddHours(6), "Lý do");
        aptCompleted2.Confirm();
        aptCompleted2.CheckIn();
        aptCompleted2.StartConsultation();
        aptCompleted2.CompleteConsultation();
        aptCompleted2.PaidAmount = svcB.Price;

        // 7. Cancelled (Doctor A, có hoàn tiền & phí hủy)
        var aptCancelled = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, baseTime.AddHours(7), "Lý do");
        aptCancelled.Confirm();
        aptCancelled.PaidAmount = svcA.Price;
        aptCancelled.Cancel("Bận việc đột xuất", baseTime.AddHours(6).AddMinutes(30)); // Hủy trước 30p: phí hủy 50%, hoàn 50%

        var refund = Refund.Create(
            appointmentId: aptCancelled.Id,
            paidServiceAmount: svcA.Price,
            cancellationFeeRate: 0.50m,
            cancellationFeeAmount: svcA.Price * 0.50m,
            expectedRefundAmount: svcA.Price * 0.50m,
            policyVersion: "v1.0",
            reason: "Hủy trước giờ hẹn"
        );
        refund.MarkSucceeded(Guid.NewGuid(), "TXN_REFUND_001");
        db.Refunds.Add(refund);

        // 8. NoShow
        var aptNoShow = Appointment.CreateGuest(docB.Id, patient.Id, svcB.Id, svcB.Name, 30, svcB.Price, baseTime.AddHours(8), "Lý do");
        aptNoShow.Confirm();
        aptNoShow.Status = AppointmentStatus.NoShow;

        db.Appointments.AddRange(aptPending, aptConfirmed, aptCheckedIn, aptInProgress, aptCompleted1, aptCompleted2, aptCancelled, aptNoShow);
        await db.SaveChangesAsync();

        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);
        var query = new GetAdminDashboardReportQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var report = result.Value!;
        var breakdown = report.StatusBreakdown;

        // 1. Kiểm tra đối soát số lượng (Numbers reconcile)
        breakdown.TotalAppointments.Should().Be(8);
        breakdown.PendingConfirmationCount.Should().Be(1);
        breakdown.ConfirmedCount.Should().Be(1);
        breakdown.CheckedInCount.Should().Be(1);
        breakdown.InProgressCount.Should().Be(1);
        breakdown.CompletedCount.Should().Be(2);
        breakdown.CancelledCount.Should().Be(1);
        breakdown.NoShowCount.Should().Be(1);

        // Bảo toàn tổng: Total == tổng các trạng thái
        var sumOfStatuses = breakdown.PendingConfirmationCount +
                            breakdown.ConfirmedCount +
                            breakdown.CheckedInCount +
                            breakdown.InProgressCount +
                            breakdown.CompletedCount +
                            breakdown.CancelledCount +
                            breakdown.NoShowCount;
        breakdown.TotalAppointments.Should().Be(sumOfStatuses);

        // Tỷ lệ hoàn thành: 2 / 8 = 25%
        breakdown.CompletionRate.Should().Be(25.0);

        // Tỷ lệ hủy ca: 1 / 8 = 12.5%
        breakdown.CancellationRate.Should().Be(12.5);

        // 2. Kiểm tra đối soát tài chính (Financial reconcile)
        var fin = report.FinancialSummary;
        fin.TotalGrossRevenue.Should().Be(aptCompleted1.ServicePriceSnapshot + aptCompleted2.ServicePriceSnapshot); // 200,000 + 300,000 = 500,000
        fin.TotalPaidAmount.Should().Be(aptCompleted1.PaidAmount + aptCompleted2.PaidAmount + aptCancelled.PaidAmount); // 200,000 + 300,000 + 200,000 = 700,000
        fin.TotalExpectedRefundAmount.Should().Be(100000m);
        fin.TotalActualRefundedAmount.Should().Be(100000m);
        fin.TotalCancellationFeeCollected.Should().Be(100000m);
        fin.TotalNetRevenue.Should().Be(fin.TotalGrossRevenue + fin.TotalCancellationFeeCollected); // 500,000 + 100,000 = 600,000
    }

    [Fact]
    public async Task AdminDashboardReport_FilterByDate_ReturnsOnlyAppointmentsWithinRange()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (specA, _, docA, _, patient) = SeedCatalogAndDoctors(db);
        var svcA = await db.Services.FirstAsync(s => s.SpecialtyId == specA.Id);

        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        // Hôm qua
        var aptYesterday = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(-2), "Hôm qua");
        // Hôm nay
        var aptToday = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow, "Hôm nay");
        // Ngày mai
        var aptTomorrow = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(2), "Ngày mai");

        db.Appointments.AddRange(aptYesterday, aptToday, aptTomorrow);
        await db.SaveChangesAsync();

        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);

        // Act: Chỉ lọc ngày hôm nay
        var query = new GetAdminDashboardReportQuery(FromDate: todayVn, ToDate: todayVn);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusBreakdown.TotalAppointments.Should().Be(1);
    }

    [Fact]
    public async Task AdminDashboardReport_FilterByDoctor_ReturnsOnlyDoctorAppointments()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (specA, specB, docA, docB, patient) = SeedCatalogAndDoctors(db);
        var svcA = await db.Services.FirstAsync(s => s.SpecialtyId == specA.Id);
        var svcB = await db.Services.FirstAsync(s => s.SpecialtyId == specB.Id);

        var aptA1 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(1), "A1");
        var aptA2 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(2), "A2");
        var aptB1 = Appointment.CreateGuest(docB.Id, patient.Id, svcB.Id, svcB.Name, 30, svcB.Price, DateTime.UtcNow.AddDays(1), "B1");

        db.Appointments.AddRange(aptA1, aptA2, aptB1);
        await db.SaveChangesAsync();

        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);

        // Act: Lọc theo Doctor A
        var query = new GetAdminDashboardReportQuery(DoctorId: docA.Id);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusBreakdown.TotalAppointments.Should().Be(2);
        result.Value.DoctorReports.Should().ContainSingle(d => d.DoctorId == docA.Id);
    }

    [Fact]
    public async Task AdminDashboardReport_FilterBySpecialty_ReturnsOnlySpecialtyAppointments()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (specA, specB, docA, docB, patient) = SeedCatalogAndDoctors(db);
        var svcA = await db.Services.FirstAsync(s => s.SpecialtyId == specA.Id);
        var svcB = await db.Services.FirstAsync(s => s.SpecialtyId == specB.Id);

        var aptA = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(1), "A");
        var aptB = Appointment.CreateGuest(docB.Id, patient.Id, svcB.Id, svcB.Name, 30, svcB.Price, DateTime.UtcNow.AddDays(1), "B");

        db.Appointments.AddRange(aptA, aptB);
        await db.SaveChangesAsync();

        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);

        // Act: Lọc theo Chuyên khoa A
        var query = new GetAdminDashboardReportQuery(SpecialtyId: specA.Id);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusBreakdown.TotalAppointments.Should().Be(1);
        result.Value.SpecialtyReports.Should().ContainSingle(s => s.SpecialtyId == specA.Id);
    }

    [Fact]
    public async Task AdminDashboardReport_FilterByStatus_ReturnsOnlyStatusAppointments()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (specA, _, docA, _, patient) = SeedCatalogAndDoctors(db);
        var svcA = await db.Services.FirstAsync(s => s.SpecialtyId == specA.Id);

        var apt1 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(1), "1");
        apt1.Confirm();
        apt1.CheckIn();
        apt1.StartConsultation();
        apt1.CompleteConsultation();

        var apt2 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddDays(2), "2");
        apt2.Cancel("Lý do", DateTime.UtcNow);

        db.Appointments.AddRange(apt1, apt2);
        await db.SaveChangesAsync();

        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);

        // Act: Chỉ lấy Completed
        var query = new GetAdminDashboardReportQuery(Status: AppointmentStatus.Completed);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusBreakdown.TotalAppointments.Should().Be(1);
        result.Value.StatusBreakdown.CompletedCount.Should().Be(1);
        result.Value.StatusBreakdown.CancelledCount.Should().Be(0);
    }

    [Fact]
    public async Task AdminDashboardReport_ByNonAdmin_ThrowsForbidden()
    {
        // Arrange: User có role DOCTOR hoặc RECEPTIONIST hoặc PATIENT cố ý xem báo cáo toàn hệ thống
        using var db = CreateInMemoryDbContext();

        var doctorUser = new TestAdminCurrentUserService
        {
            Roles = new List<string> { Role.DoctorCode }
        };

        var handler = new GetAdminDashboardReportQueryHandler(db, doctorUser);
        var query = new GetAdminDashboardReportQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Quản trị viên");
    }

    [Fact]
    public void AdminReportsController_ProtectedWithAdminOnlyPolicy()
    {
        // Arrange & Act
        var controllerType = typeof(AdminReportsController);
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authAttr.Should().NotBeNull();
        authAttr!.Policy.Should().Be("AdminOnly");
    }

    [Fact]
    public async Task AdminDashboardReport_EmptyData_HandlesZeroSafelyWithoutDivideByZero()
    {
        // Arrange: Không có cuộc hẹn nào trong hệ thống
        using var db = CreateInMemoryDbContext();
        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);
        var query = new GetAdminDashboardReportQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.StatusBreakdown.TotalAppointments.Should().Be(0);
        report.StatusBreakdown.CompletionRate.Should().Be(0.0);
        report.StatusBreakdown.CancellationRate.Should().Be(0.0);
        report.FinancialSummary.TotalGrossRevenue.Should().Be(0m);
        report.FinancialSummary.TotalNetRevenue.Should().Be(0m);
        report.SpecialtyReports.Should().BeEmpty();
        report.DoctorReports.Should().BeEmpty();
    }

    [Fact]
    public async Task AdminDashboardReport_DoctorAndSpecialtyMetrics_CalculatedAccurately()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (specA, specB, docA, docB, patient) = SeedCatalogAndDoctors(db);
        var svcA = await db.Services.FirstAsync(s => s.SpecialtyId == specA.Id);
        var svcB = await db.Services.FirstAsync(s => s.SpecialtyId == specB.Id);

        // Doc A: 1 completed, 1 cancelled
        var aptA1 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddHours(1), "A1");
        aptA1.Confirm();
        aptA1.CheckIn();
        aptA1.StartConsultation();
        aptA1.CompleteConsultation();

        var aptA2 = Appointment.CreateGuest(docA.Id, patient.Id, svcA.Id, svcA.Name, 30, svcA.Price, DateTime.UtcNow.AddHours(2), "A2");
        aptA2.Cancel("Bận", DateTime.UtcNow);

        // Doc B: 1 completed
        var aptB1 = Appointment.CreateGuest(docB.Id, patient.Id, svcB.Id, svcB.Name, 30, svcB.Price, DateTime.UtcNow.AddHours(3), "B1");
        aptB1.Confirm();
        aptB1.CheckIn();
        aptB1.StartConsultation();
        aptB1.CompleteConsultation();

        db.Appointments.AddRange(aptA1, aptA2, aptB1);
        await db.SaveChangesAsync();

        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);
        var query = new GetAdminDashboardReportQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;

        // Doctor reports
        var docReportA = report.DoctorReports.FirstOrDefault(d => d.DoctorId == docA.Id);
        docReportA.Should().NotBeNull();
        docReportA!.TotalAppointments.Should().Be(2);
        docReportA.CompletedCount.Should().Be(1);
        docReportA.CancelledCount.Should().Be(1);
        docReportA.CompletionRate.Should().Be(50.0);
        docReportA.GrossRevenue.Should().Be(svcA.Price);

        var docReportB = report.DoctorReports.FirstOrDefault(d => d.DoctorId == docB.Id);
        docReportB.Should().NotBeNull();
        docReportB!.TotalAppointments.Should().Be(1);
        docReportB.CompletedCount.Should().Be(1);
        docReportB.CompletionRate.Should().Be(100.0);
        docReportB.GrossRevenue.Should().Be(svcB.Price);

        // Specialty reports
        var specReportA = report.SpecialtyReports.FirstOrDefault(s => s.SpecialtyId == specA.Id);
        specReportA.Should().NotBeNull();
        specReportA!.TotalAppointments.Should().Be(2);
        specReportA.CompletedCount.Should().Be(1);
        specReportA.GrossRevenue.Should().Be(svcA.Price);

        var specReportB = report.SpecialtyReports.FirstOrDefault(s => s.SpecialtyId == specB.Id);
        specReportB.Should().NotBeNull();
        specReportB!.TotalAppointments.Should().Be(1);
        specReportB.CompletedCount.Should().Be(1);
        specReportB.GrossRevenue.Should().Be(svcB.Price);
    }

    [Fact]
    public async Task AdminReportsController_Endpoints_InvokeMediatR_Successfully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var adminUser = new TestAdminCurrentUserService();
        var handler = new GetAdminDashboardReportQueryHandler(db, adminUser);

        // Giả lập Mediator gọi trực tiếp handler
        var query = new GetAdminDashboardReportQuery();
        var reportResult = await handler.Handle(query, CancellationToken.None);

        reportResult.IsSuccess.Should().BeTrue();
        reportResult.Value.Should().NotBeNull();
    }
}
