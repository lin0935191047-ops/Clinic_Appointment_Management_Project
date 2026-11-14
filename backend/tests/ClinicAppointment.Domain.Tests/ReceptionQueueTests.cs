using System.Reflection;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Queries.GetReceptionQueue;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class ReceptionQueueTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Doctor DoctorA, Doctor DoctorB, Service ServiceA, List<Appointment> Appointments) SeedData(ApplicationDbContext db)
    {
        var spec = Specialty.Create("CK-TIM", "Tim Mạch", "tim-mach");
        db.Specialties.Add(spec);

        var docUserA = User.Create("dr.tuan@clinic.vn", "hash");
        var docUserB = User.Create("dr.lan@clinic.vn", "hash");
        db.Users.AddRange(docUserA, docUserB);

        var docA = Doctor.Create(docUserA.Id, spec.Id, "DOC-CARD-01", "BS. Trần Tuấn");
        docA.Title = "ThS. Bác sĩ";
        docA.RoomNumber = "Phòng 201";

        var docB = Doctor.Create(docUserB.Id, spec.Id, "DOC-CARD-02", "BS. Nguyễn Lan");
        docB.Title = "CKI";
        docB.RoomNumber = "Phòng 202";

        db.Doctors.AddRange(docA, docB);

        var svcA = Service.Create(spec.Id, "DV-TIM-01", "Điện tâm đồ ECG", 30, 200000m);
        db.Services.Add(svcA);

        var p1 = Patient.Create("Nguyễn Văn An", "0912345678", new DateOnly(1990, 1, 1), 1990);
        var p2 = Patient.Create("Lê Thị Bình", "0987654321", new DateOnly(1985, 5, 12), 1985);
        var p3 = Patient.Create("Trần Quốc Cường", "0933111222", new DateOnly(1998, 11, 20), 1998);
        var p4 = Patient.Create("Phạm Mai Dung", "0944333555", new DateOnly(2000, 7, 7), 2000);
        db.Patients.AddRange(p1, p2, p3, p4);

        // Ngày thử nghiệm: 15/11/2026. 
        // 08:00 AM VN (01:00 AM UTC), 09:00 AM VN (02:00 AM UTC), 10:00 AM VN (03:00 AM UTC)
        var dateTestUtc = new DateTime(2026, 11, 15, 1, 0, 0, DateTimeKind.Utc);

        var apt1 = Appointment.CreateGuest(docA.Id, p1.Id, svcA.Id, svcA.Name, svcA.DurationMinutes, svcA.Price, dateTestUtc, "Khám tức ngực");
        apt1.Confirm();
        apt1.CheckIn(); // CheckedIn

        var apt2 = Appointment.CreateGuest(docA.Id, p2.Id, svcA.Id, svcA.Name, svcA.DurationMinutes, svcA.Price, dateTestUtc.AddHours(1), "Đau thắt tim");
        apt2.Confirm(); // Confirmed

        var apt3 = Appointment.CreateGuest(docA.Id, p3.Id, svcA.Id, svcA.Name, svcA.DurationMinutes, svcA.Price, dateTestUtc.AddHours(2), "Tái khám huyết áp");
        apt3.Confirm();
        apt3.CheckIn();
        apt3.Status = AppointmentStatus.InProgress; // InProgress

        // Ngày khác: 16/11/2026 cho Bác sĩ B
        var dateOtherUtc = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Utc);
        var apt4 = Appointment.CreateGuest(docB.Id, p4.Id, svcA.Id, svcA.Name, svcA.DurationMinutes, svcA.Price, dateOtherUtc, "Kiểm tra nhịp tim");
        apt4.Confirm();

        db.Appointments.AddRange(apt1, apt2, apt3, apt4);
        db.SaveChanges();

        return (docA, docB, svcA, new List<Appointment> { apt1, apt2, apt3, apt4 });
    }

    [Fact]
    public async Task StaffOnly_EnforcedForReceptionQueue_PatientDenied()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, patientUser, new IdentifierMaskingService());
        var query = new GetReceptionQueueQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bạn không có quyền truy cập hàng đợi tiếp nhận");
    }

    [Fact]
    public async Task StaffOnly_EnforcedForReceptionQueue_AnonymousDenied()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var anonUser = new TestCurrentUserService
        {
            UserId = null,
            Roles = new List<string>(),
            IsAuthenticated = false
        };

        var handler = new GetReceptionQueueQueryHandler(db, anonUser, new IdentifierMaskingService());
        var query = new GetReceptionQueueQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ReceptionistAndAdmin_AllowedAccessToQueue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());
        var query = new GetReceptionQueueQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Thành công 200 OK
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.TotalItems.Should().Be(4);
    }

    [Fact]
    public async Task FilterByDoctor_ReturnsOnlyDoctorAppointments()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (docA, docB, _, _) = SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act 1: Lọc theo Bác sĩ A (có 3 ca)
        var resultA = await handler.Handle(new GetReceptionQueueQuery(DoctorId: docA.Id), CancellationToken.None);

        // Act 2: Lọc theo Bác sĩ B (có 1 ca)
        var resultB = await handler.Handle(new GetReceptionQueueQuery(DoctorId: docB.Id), CancellationToken.None);

        // Assert
        resultA.IsSuccess.Should().BeTrue();
        resultA.Value!.TotalItems.Should().Be(3);
        resultA.Value.Items.Should().OnlyContain(item => item.DoctorId == docA.Id);

        resultB.IsSuccess.Should().BeTrue();
        resultB.Value!.TotalItems.Should().Be(1);
        resultB.Value.Items.Should().OnlyContain(item => item.DoctorId == docB.Id);
    }

    [Fact]
    public async Task FilterByDate_ReturnsOnlyAppointmentsOnThatDate()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act: Lọc theo ngày 15/11/2026 (có 3 ca của Doc A)
        var query = new GetReceptionQueueQuery(Date: new DateOnly(2026, 11, 15));
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(3);
        result.Value.Items.Select(x => x.PatientName).Should().Contain(new[] { "Nguyễn Văn An", "Lê Thị Bình", "Trần Quốc Cường" });
    }

    [Fact]
    public async Task FilterByStatus_ReturnsOnlyMatchingStatusAppointments()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act: Lọc các ca đã tiếp nhận (CheckedIn)
        var result = await handler.Handle(new GetReceptionQueueQuery(Status: AppointmentStatus.CheckedIn), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(1);
        result.Value.Items.First().PatientName.Should().Be("Nguyễn Văn An");
        result.Value.Items.First().Status.Should().Be(AppointmentStatus.CheckedIn);
    }

    [Fact]
    public async Task FilterByKeyword_MatchesPatientNameAndBookingCode()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (_, _, _, apts) = SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act 1: Tìm theo tên bệnh nhân "Bình"
        var resultName = await handler.Handle(new GetReceptionQueueQuery(Keyword: "Bình"), CancellationToken.None);

        // Act 2: Tìm theo mã booking của ca 1
        var resultBooking = await handler.Handle(new GetReceptionQueueQuery(Keyword: apts[0].BookingCode), CancellationToken.None);

        // Assert
        resultName.IsSuccess.Should().BeTrue();
        resultName.Value!.TotalItems.Should().Be(1);
        resultName.Value.Items.First().PatientName.Should().Be("Lê Thị Bình");

        resultBooking.IsSuccess.Should().BeTrue();
        resultBooking.Value!.TotalItems.Should().Be(1);
        resultBooking.Value.Items.First().BookingCode.Should().Be(apts[0].BookingCode);
    }

    [Fact]
    public async Task SortByTime_AscendingAndDescending_OrdersCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act 1: Mặc định hoặc "time_asc" (ngày 15 trước ngày 16)
        var resultAsc = await handler.Handle(new GetReceptionQueueQuery(SortBy: "time_asc"), CancellationToken.None);

        // Act 2: "time_desc" (ngày 16 trước ngày 15)
        var resultDesc = await handler.Handle(new GetReceptionQueueQuery(SortBy: "time_desc"), CancellationToken.None);

        // Assert
        resultAsc.IsSuccess.Should().BeTrue();
        resultAsc.Value!.Items.First().PatientName.Should().Be("Nguyễn Văn An"); // Ca sớm nhất (08:00 15/11)

        resultDesc.IsSuccess.Should().BeTrue();
        resultDesc.Value!.Items.First().PatientName.Should().Be("Phạm Mai Dung"); // Ca muộn nhất (09:00 16/11)
    }

    [Fact]
    public async Task SortByStatus_PrioritizesCheckedInAndInProgress()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act: Sắp xếp theo trạng thái hàng đợi khám
        var result = await handler.Handle(new GetReceptionQueueQuery(SortBy: "status"), CancellationToken.None);

        // Assert: CheckedIn đứng đầu, sau đó đến InProgress, rồi Confirmed
        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items.ToList();
        items[0].Status.Should().Be(AppointmentStatus.CheckedIn);
        items[1].Status.Should().Be(AppointmentStatus.InProgress);
    }

    [Fact]
    public async Task Pagination_WorksCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act: Lấy trang 1 với pageSize = 2
        var result = await handler.Handle(new GetReceptionQueueQuery(PageNumber: 1, PageSize: 2), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(4);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueueNumber_AssignedSequentially()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (docA, _, _, _) = SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act: Xem hàng đợi của Bác sĩ A ngày 15/11
        var result = await handler.Handle(new GetReceptionQueueQuery(DoctorId: docA.Id, Date: new DateOnly(2026, 11, 15)), CancellationToken.None);

        // Assert: Số thứ tự tiếp nhận được đánh số liên tục 1, 2, 3
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(x => x.QueueNumber).Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public async Task DataAccessLimited_PhoneIsMasked_NoPiiLeakage()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new GetReceptionQueueQueryHandler(db, staff, new IdentifierMaskingService());

        // Act
        var result = await handler.Handle(new GetReceptionQueueQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstItem = result.Value!.Items.First();

        // 1. Số điện thoại phải được che giấu mặt nạ
        firstItem.MaskedPhoneNumber.Should().NotBeNullOrWhiteSpace();
        firstItem.MaskedPhoneNumber.Should().Contain("***");
        firstItem.MaskedPhoneNumber.Should().NotBe("0912345678");

        // 2. Room number và title của bác sĩ có sẵn để lễ tân hướng dẫn bệnh nhân
        firstItem.DoctorTitle.Should().NotBeNullOrWhiteSpace();
        firstItem.RoomNumber.Should().NotBeNullOrWhiteSpace();

        // 3. Không chứa các trường bệnh án chuyên sâu hay thông tin bảo mật
        var dtoProperties = typeof(ReceptionQueueItemDto).GetProperties().Select(p => p.Name).ToList();
        dtoProperties.Should().NotContain("NationalId");
        dtoProperties.Should().NotContain("RawPhone");
        dtoProperties.Should().NotContain("Password");
        dtoProperties.Should().NotContain("Otp");
        dtoProperties.Should().NotContain("Diagnosis");
        dtoProperties.Should().NotContain("Prescription");
    }

    [Fact]
    public void ReceptionAppointmentsController_Endpoints_ProtectedWithStaffOnlyPolicy()
    {
        var controllerType = typeof(ReceptionAppointmentsController);
        var classAuth = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        classAuth.Should().NotBeNull();
        classAuth!.Policy.Should().Be("StaffOnly");

        var getQueueMethod = controllerType.GetMethod(nameof(ReceptionAppointmentsController.GetReceptionQueue));
        getQueueMethod.Should().NotBeNull();
    }
}
