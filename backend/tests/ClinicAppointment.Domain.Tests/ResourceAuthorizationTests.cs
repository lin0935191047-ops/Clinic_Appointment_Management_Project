using System.Security.Claims;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
    public bool IsAuthenticated { get; set; }
    public bool IsInRole(string role) => Roles.Contains(role);
    public Guid? PatientProfileId { get; set; }
    public Guid? DoctorProfileId { get; set; }

    public bool IsAdmin => IsInRole(Role.AdminCode);
    public bool IsStaff => IsInRole(Role.AdminCode) || IsInRole(Role.ReceptionistCode);
    public bool IsDoctor => IsInRole(Role.DoctorCode);
    public bool IsPatient => IsInRole(Role.PatientCode);
}

public class ResourceAuthorizationTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CanAccessPatient_WhenUnauthenticated_ShouldReturnFalse()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var currentUser = new TestCurrentUserService { IsAuthenticated = false };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessPatientAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessPatient_WhenStaff_ShouldReturnTrue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.ReceptionistCode }
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessPatientAsync(Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessPatient_WhenPatientAccessesOwnRecord_ShouldReturnTrue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var patientId = Guid.NewGuid();
        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.PatientCode },
            PatientProfileId = patientId
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessPatientAsync(patientId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessPatient_WhenPatientAccessesOtherPatientRecord_ShouldReturnFalse_PreventIDOR()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var myPatientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.PatientCode },
            PatientProfileId = myPatientId
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessPatientAsync(otherPatientId);

        // Assert: Chống IDOR - Bệnh nhân A không được xem hồ sơ Bệnh nhân B
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAppointment_WhenStaff_ShouldReturnTrue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var apt = Appointment.CreateGuest(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Khám Nội", 30, 200000m, DateTime.UtcNow.AddHours(2), "Khám tổng quát");
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.AdminCode }
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessAppointmentAsync(apt.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAppointment_WhenDoctorIsAssigned_ShouldReturnTrue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctorId = Guid.NewGuid();
        var apt = Appointment.CreateGuest(
            doctorId, Guid.NewGuid(), Guid.NewGuid(),
            "Khám Tim Mạch", 45, 300000m, DateTime.UtcNow.AddHours(3), "Tái khám tim");
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.DoctorCode },
            DoctorProfileId = doctorId
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessAppointmentAsync(apt.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAppointment_WhenDoctorIsNotAssigned_ShouldReturnFalse_PreventIDOR()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctorAId = Guid.NewGuid();
        var doctorBId = Guid.NewGuid();

        var apt = Appointment.CreateGuest(
            doctorAId, Guid.NewGuid(), Guid.NewGuid(),
            "Khám Tai Mũi Họng", 30, 250000m, DateTime.UtcNow.AddHours(4), "Đau họng");
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.DoctorCode },
            DoctorProfileId = doctorBId // Bác sĩ B cố truy cập ca khám của Bác sĩ A
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessAppointmentAsync(apt.Id);

        // Assert: Chống IDOR - Bác sĩ không được xem lịch phân công của bác sĩ khác
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessAppointment_WhenPatientOwnsAppointment_ShouldReturnTrue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var patientId = Guid.NewGuid();
        var apt = Appointment.CreateGuest(
            Guid.NewGuid(), patientId, Guid.NewGuid(),
            "Khám Da Liễu", 30, 200000m, DateTime.UtcNow.AddDays(1), "Dị ứng");
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.PatientCode },
            PatientProfileId = patientId
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessAppointmentAsync(apt.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessAppointment_WhenPatientDoesNotOwnAppointment_ShouldReturnFalse_PreventIDOR()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var patientAId = Guid.NewGuid();
        var patientBId = Guid.NewGuid();

        var apt = Appointment.CreateGuest(
            Guid.NewGuid(), patientAId, Guid.NewGuid(),
            "Khám Mắt", 30, 180000m, DateTime.UtcNow.AddDays(2), "Đo thị lực");
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.PatientCode },
            PatientProfileId = patientBId // Bệnh nhân B cố truy cập lịch hẹn của Bệnh nhân A
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessAppointmentAsync(apt.Id);

        // Assert: Chống IDOR - Bệnh nhân B không được xem lịch hẹn của Bệnh nhân A
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessDoctorSchedule_WhenDoctorAccessesOwnSchedule_ShouldReturnTrue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctorId = Guid.NewGuid();
        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.DoctorCode },
            DoctorProfileId = doctorId
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessDoctorScheduleAsync(doctorId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessDoctorSchedule_WhenOtherDoctorOrPatient_ShouldReturnFalse()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctorAId = Guid.NewGuid();
        var doctorBId = Guid.NewGuid();

        var currentUser = new TestCurrentUserService
        {
            IsAuthenticated = true,
            Roles = new[] { Role.DoctorCode },
            DoctorProfileId = doctorBId
        };
        var authService = new ResourceAuthorizationService(currentUser, db);

        // Act
        var result = await authService.CanAccessDoctorScheduleAsync(doctorAId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CurrentUserService_ShouldExtractClaims_FromHttpContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var email = "staff@clinic.vn";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, Role.ReceptionistCode),
            new("patient_profile_id", patientId.ToString()),
            new("doctor_profile_id", doctorId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUserService = new CurrentUserService(httpContextAccessor);

        // Assert
        currentUserService.IsAuthenticated.Should().BeTrue();
        currentUserService.UserId.Should().Be(userId);
        currentUserService.Email.Should().Be(email);
        currentUserService.PatientProfileId.Should().Be(patientId);
        currentUserService.DoctorProfileId.Should().Be(doctorId);
        currentUserService.IsStaff.Should().BeTrue();
        currentUserService.IsAdmin.Should().BeFalse();
        currentUserService.IsInRole(Role.ReceptionistCode).Should().BeTrue();
    }
}
