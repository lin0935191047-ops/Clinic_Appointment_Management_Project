using System.Reflection;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Queries.GetDoctorAppointments;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class DoctorAssignedAppointmentsTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (User UserDocA, Doctor DoctorA, User UserDocB, Doctor DoctorB, List<Appointment> Appointments) SeedData(ApplicationDbContext db)
    {
        var spec = Specialty.Create("CK-NOI", "Nội Tổng Quát", "noi-tong-quat");
        db.Specialties.Add(spec);

        var userDocA = User.Create("dr.minh@clinic.vn", "hash_pw_1");
        var userDocB = User.Create("dr.hoa@clinic.vn", "hash_pw_2");
        db.Users.AddRange(userDocA, userDocB);

        var docA = Doctor.Create(userDocA.Id, spec.Id, "DOC-INT-01", "BS. Lê Minh");
        docA.Title = "ThS. Bác sĩ";
        docA.RoomNumber = "Phòng 101";

        var docB = Doctor.Create(userDocB.Id, spec.Id, "DOC-INT-02", "BS. Hoàng Hoa");
        docB.Title = "CKI";
        docB.RoomNumber = "Phòng 102";

        db.Doctors.AddRange(docA, docB);

        var svc = Service.Create(spec.Id, "DV-NOI-01", "Khám nội chuyên khoa", 30, 250000m);
        db.Services.Add(svc);

        var p1 = Patient.Create("Đặng Đình Trọng", "0911222333", new DateOnly(1991, 3, 10), 1991);
        var p2 = Patient.Create("Trần Thị Thu", "0922333444", new DateOnly(1994, 7, 15), 1994);
        var p3 = Patient.Create("Nguyễn Hải Nam", "0933444555", new DateOnly(1989, 12, 1), 1989);
        var p4 = Patient.Create("Vũ Ngọc Ánh", "0944555666", new DateOnly(2001, 5, 25), 2001);
        db.Patients.AddRange(p1, p2, p3, p4);

        // Ngày thử nghiệm: 15/11/2026. 
        // 08:00 AM VN (01:00 AM UTC), 09:00 AM VN (02:00 AM UTC), 10:00 AM VN (03:00 AM UTC)
        var dateTestUtc = new DateTime(2026, 11, 15, 1, 0, 0, DateTimeKind.Utc);

        // 3 ca khám của Bác sĩ A
        var apt1 = Appointment.CreateGuest(docA.Id, p1.Id, svc.Id, svc.Name, svc.DurationMinutes, svc.Price, dateTestUtc, "Kiểm tra định kỳ");
        apt1.Confirm();
        apt1.CheckIn(); // CheckedIn

        var apt2 = Appointment.CreateGuest(docA.Id, p2.Id, svc.Id, svc.Name, svc.DurationMinutes, svc.Price, dateTestUtc.AddHours(1), "Đau đầu chóng mặt");
        apt2.Confirm(); // Confirmed

        var apt3 = Appointment.CreateGuest(docA.Id, p3.Id, svc.Id, svc.Name, svc.DurationMinutes, svc.Price, dateTestUtc.AddHours(2), "Khám họng");
        apt3.Confirm();
        apt3.CheckIn();
        apt3.Status = AppointmentStatus.InProgress; // InProgress

        // 1 ca khám của Bác sĩ B (cùng ngày 15/11/2026 lúc 08:30)
        var apt4 = Appointment.CreateGuest(docB.Id, p4.Id, svc.Id, svc.Name, svc.DurationMinutes, svc.Price, dateTestUtc.AddMinutes(30), "Khám tiêu hóa");
        apt4.Confirm();
        apt4.CheckIn();

        db.Appointments.AddRange(apt1, apt2, apt3, apt4);
        db.SaveChanges();

        return (userDocA, docA, userDocB, docB, new List<Appointment> { apt1, apt2, apt3, apt4 });
    }

    [Fact]
    public async Task DoctorOnly_Enforced_PatientDenied()
    {
        // Arrange: Người dùng là Bệnh nhân
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, patientUser, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bạn không có quyền truy cập danh sách ca khám của Bác sĩ");
    }

    [Fact]
    public async Task DoctorOnly_Enforced_AnonymousDenied()
    {
        // Arrange: Khách ẩn danh
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var anonUser = new TestCurrentUserService
        {
            UserId = null,
            Roles = new List<string>(),
            IsAuthenticated = false
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, anonUser, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task DoctorUser_WithoutProfile_ReturnsForbidden()
    {
        // Arrange: Có Role DOCTOR nhưng UserId chưa liên kết hồ sơ Bác sĩ nào trong DB
        using var db = CreateInMemoryDbContext();
        SeedData(db);

        var doctorWithoutProfile = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorWithoutProfile, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Không tìm thấy hồ sơ Bác sĩ tương ứng");
    }

    [Fact]
    public async Task AssignedDoctorOnly_DoctorSeesOnlyOwnAppointments()
    {
        // Arrange: Bác sĩ A đăng nhập
        using var db = CreateInMemoryDbContext();
        var (userDocA, docA, _, docB, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: Bác sĩ A chỉ thấy đúng 3 ca khám của mình, tuyệt đối không thấy ca của Bác sĩ B
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Value.Should().NotBeNull();
        result.Value!.TotalItems.Should().Be(3);
        result.Value.Items.Should().OnlyContain(x => x.DoctorId == docA.Id);
        result.Value.Items.Should().NotContain(x => x.DoctorId == docB.Id);
    }

    [Fact]
    public async Task AssignedDoctorOnly_CannotTamperDoctorIdToViewOtherDoctorAppointments()
    {
        // Arrange: Bác sĩ A cố tình truyền DoctorId của Bác sĩ B trong query param (Tấn công IDOR)
        using var db = CreateInMemoryDbContext();
        var (userDocA, docA, _, docB, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());
        var maliciousQuery = new GetDoctorAppointmentsQuery(DoctorId: docB.Id);

        // Act
        var result = await handler.Handle(maliciousQuery, CancellationToken.None);

        // Assert: Hệ thống kiên quyết ép buộc chỉ trả về ca khám của Bác sĩ A
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(3);
        result.Value.Items.Should().OnlyContain(x => x.DoctorId == docA.Id);
        result.Value.Items.Should().NotContain(x => x.DoctorId == docB.Id);
    }

    [Fact]
    public async Task AdminUser_CanFilterByDoctorId()
    {
        // Arrange: Quản trị viên (ADMIN) xem lịch của Bác sĩ B
        using var db = CreateInMemoryDbContext();
        var (_, _, _, docB, _) = SeedData(db);

        var adminUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, adminUser, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery(DoctorId: docB.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(1);
        result.Value.Items.First().DoctorId.Should().Be(docB.Id);
        result.Value.Items.First().PatientName.Should().Be("Vũ Ngọc Ánh");
    }

    [Fact]
    public async Task FilterByDate_ReturnsAppointmentsForThatDay()
    {
        // Arrange: Bác sĩ A lọc theo ngày 15/11/2026
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery(Date: new DateOnly(2026, 11, 15));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(3);
        result.Value.Items.Select(x => x.PatientName).Should().Contain(new[] { "Đặng Đình Trọng", "Trần Thị Thu", "Nguyễn Hải Nam" });
    }

    [Fact]
    public async Task FilterByStatus_ReturnsMatchingAppointments()
    {
        // Arrange: Bác sĩ A lọc các ca đã tiếp nhận CheckedIn
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());
        var query = new GetDoctorAppointmentsQuery(Status: AppointmentStatus.CheckedIn);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(1);
        result.Value.Items.First().PatientName.Should().Be("Đặng Đình Trọng");
        result.Value.Items.First().Status.Should().Be(AppointmentStatus.CheckedIn);
    }

    [Fact]
    public async Task FilterByKeyword_MatchesPatientNameAndBookingCode()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, apts) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());

        // Act 1: Tìm theo tên bệnh nhân "Thu"
        var resultName = await handler.Handle(new GetDoctorAppointmentsQuery(Keyword: "Thu"), CancellationToken.None);

        // Act 2: Tìm theo mã booking của ca 1
        var resultBooking = await handler.Handle(new GetDoctorAppointmentsQuery(Keyword: apts[0].BookingCode), CancellationToken.None);

        // Assert
        resultName.IsSuccess.Should().BeTrue();
        resultName.Value!.TotalItems.Should().Be(1);
        resultName.Value.Items.First().PatientName.Should().Be("Trần Thị Thu");

        resultBooking.IsSuccess.Should().BeTrue();
        resultBooking.Value!.TotalItems.Should().Be(1);
        resultBooking.Value.Items.First().BookingCode.Should().Be(apts[0].BookingCode);
    }

    [Fact]
    public async Task Pagination_WorksCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());

        // Act: Bác sĩ A có 3 ca, lấy trang 1 với pageSize = 2
        var result = await handler.Handle(new GetDoctorAppointmentsQuery(PageNumber: 1, PageSize: 2), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItems.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task SortByTime_AscendingAndDescending_OrdersCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());

        // Act 1: Sắp xếp tăng dần (mặc định)
        var resultAsc = await handler.Handle(new GetDoctorAppointmentsQuery(SortBy: "time_asc"), CancellationToken.None);

        // Act 2: Sắp xếp giảm dần
        var resultDesc = await handler.Handle(new GetDoctorAppointmentsQuery(SortBy: "time_desc"), CancellationToken.None);

        // Assert
        resultAsc.IsSuccess.Should().BeTrue();
        resultAsc.Value!.Items.First().PatientName.Should().Be("Đặng Đình Trọng"); // Ca 08:00

        resultDesc.IsSuccess.Should().BeTrue();
        resultDesc.Value!.Items.First().PatientName.Should().Be("Nguyễn Hải Nam"); // Ca 10:00
    }

    [Fact]
    public async Task SortByStatus_PrioritizesCheckedInAndInProgress()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());

        // Act
        var result = await handler.Handle(new GetDoctorAppointmentsQuery(SortBy: "status"), CancellationToken.None);

        // Assert: CheckedIn đứng đầu, sau đó đến InProgress, rồi đến Confirmed
        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items.ToList();
        items[0].Status.Should().Be(AppointmentStatus.CheckedIn);
        items[1].Status.Should().Be(AppointmentStatus.InProgress);
        items[2].Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task QueueNumber_AssignedSequentially()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());

        // Act
        var result = await handler.Handle(new GetDoctorAppointmentsQuery(), CancellationToken.None);

        // Assert: Số thứ tự được đánh liên tục 1, 2, 3
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(x => x.QueueNumber).Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public async Task MinimumPatientData_PhoneIsMasked_NoPiiLeakage()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (userDocA, _, _, _, _) = SeedData(db);

        var doctorAUser = new TestCurrentUserService
        {
            UserId = userDocA.Id,
            Roles = new List<string> { Role.DoctorCode },
            IsAuthenticated = true
        };

        var handler = new GetDoctorAppointmentsQueryHandler(db, doctorAUser, new IdentifierMaskingService());

        // Act
        var result = await handler.Handle(new GetDoctorAppointmentsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstItem = result.Value!.Items.First();

        // 1. Số điện thoại được che giấu mặt nạ
        firstItem.MaskedPhoneNumber.Should().NotBeNullOrWhiteSpace();
        firstItem.MaskedPhoneNumber.Should().Contain("***");
        firstItem.MaskedPhoneNumber.Should().NotBe("0911222333");

        // 2. Không chứa các trường PII nhạy cảm ngoài phạm vi ca khám
        var dtoProperties = typeof(DoctorAssignedAppointmentDto).GetProperties().Select(p => p.Name).ToList();
        dtoProperties.Should().NotContain("NationalId");
        dtoProperties.Should().NotContain("Passport");
        dtoProperties.Should().NotContain("HealthInsuranceCard");
        dtoProperties.Should().NotContain("Password");
        dtoProperties.Should().NotContain("Otp");
    }

    [Fact]
    public void DoctorAppointmentsController_ProtectedWithStaffOrDoctorPolicy()
    {
        var controllerType = typeof(DoctorAppointmentsController);
        var classAuth = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        classAuth.Should().NotBeNull();
        classAuth!.Policy.Should().Be("StaffOrDoctor");

        var getMethod = controllerType.GetMethod(nameof(DoctorAppointmentsController.GetAssignedAppointments));
        getMethod.Should().NotBeNull();
    }
}
