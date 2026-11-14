using ClinicAppointment.Application.Features.Services.Commands;
using ClinicAppointment.Application.Features.Services.DTOs;
using ClinicAppointment.Application.Features.Services.Queries;
using ClinicAppointment.Application.Features.Services.Validators;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class ServiceManagementTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateService_WithValidData_ShouldCreateServiceSuccessfully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-MAT", "Mắt", "mat", isActive: true);
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);
        var request = new CreateServiceRequest(
            SpecialtyId: specialty.Id,
            Code: "SER-DO-THI-LUC",
            Name: "Đo thị lực và khúc xạ",
            DurationMinutes: 20,
            Price: 150000m,
            Currency: "VND",
            Description: "Đo thị lực chuyên sâu với bảng đo điện tử",
            IsActive: true
        );

        // Act
        var result = await handler.Handle(new CreateServiceCommand(request), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("SER-DO-THI-LUC");
        result.Name.Should().Be("Đo thị lực và khúc xạ");
        result.DurationMinutes.Should().Be(20);
        result.Price.Should().Be(150000m);
        result.SpecialtyId.Should().Be(specialty.Id);
        result.SpecialtyName.Should().Be("Mắt");
        result.SpecialtySlug.Should().Be("mat");
        result.IsActive.Should().BeTrue();

        var serviceInDb = await db.Services.FirstOrDefaultAsync(s => s.Code == "SER-DO-THI-LUC");
        serviceInDb.Should().NotBeNull();
        serviceInDb!.Price.Should().Be(150000m);
    }

    [Fact]
    public void Validator_ShouldRejectInvalidDurationAndNegativePrice()
    {
        var validator = new CreateServiceRequestValidator();

        var requestZeroDuration = new CreateServiceRequest(
            SpecialtyId: Guid.NewGuid(),
            Code: "SRV-TEST",
            Name: "Dịch vụ test",
            DurationMinutes: 0,
            Price: 100000m
        );
        validator.TestValidate(requestZeroDuration)
            .ShouldHaveValidationErrorFor(x => x.DurationMinutes);

        var requestNegativePrice = new CreateServiceRequest(
            SpecialtyId: Guid.NewGuid(),
            Code: "SRV-TEST",
            Name: "Dịch vụ test",
            DurationMinutes: 30,
            Price: -5000m
        );
        validator.TestValidate(requestNegativePrice)
            .ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void ServiceEntity_ShouldEnforceInvariantsOnCreation()
    {
        var specialtyId = Guid.NewGuid();

        // Duration <= 0 -> ArgumentException
        var actDuration = () => Service.Create(specialtyId, "SRV-01", "Dịch vụ A", durationMinutes: 0, price: 100000m);
        actDuration.Should().Throw<ArgumentException>()
            .WithMessage("*lớn hơn 0 phút*");

        // Price < 0 -> ArgumentException
        var actPrice = () => Service.Create(specialtyId, "SRV-01", "Dịch vụ A", durationMinutes: 30, price: -100m);
        actPrice.Should().Throw<ArgumentException>()
            .WithMessage("*không được âm*");
    }

    [Fact]
    public async Task CreateService_WithDuplicateCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-TAI", "Tai Mũi Họng", "tai-mui-hong", isActive: true);
        db.Specialties.Add(specialty);

        var existingService = Service.Create(specialty.Id, "SRV-NOI-SOI", "Nội soi tai", 15, 200000m);
        db.Services.Add(existingService);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);
        var duplicateReq = new CreateServiceRequest(
            SpecialtyId: specialty.Id,
            Code: "srv-noi-soi", // lowercase cũng phải bị từ chối
            Name: "Nội soi tai mũi họng nâng cao",
            DurationMinutes: 30,
            Price: 350000m
        );

        // Act
        var act = async () => await handler.Handle(new CreateServiceCommand(duplicateReq), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã tồn tại*");
    }

    [Fact]
    public async Task CreateService_WithInactiveSpecialty_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-DA-LIEU", "Da Liễu", "da-lieu", isActive: false);
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);
        var request = new CreateServiceRequest(
            SpecialtyId: specialty.Id,
            Code: "SRV-KHAM-DA",
            Name: "Khám da liễu",
            DurationMinutes: 30,
            Price: 200000m
        );

        // Act
        var act = async () => await handler.Handle(new CreateServiceCommand(request), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vô hiệu hóa*");
    }

    [Fact]
    public async Task GetActiveServices_ShouldOnlyReturnActiveServicesOfActiveSpecialties()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var activeSpecialty = Specialty.Create("CK-RANG", "Nha Khoa", "nha-khoa", isActive: true);
        var inactiveSpecialty = Specialty.Create("CK-TAM-THAN", "Tâm Thần", "tam-than", isActive: false);
        db.Specialties.AddRange(activeSpecialty, inactiveSpecialty);

        // 1. Dịch vụ hợp lệ (Active trong Chuyên khoa Active) -> PHẢI HIỂN THỊ
        var activeSrv1 = Service.Create(activeSpecialty.Id, "SRV-CAO-VOI", "Cạo vôi răng", 30, 250000m, isActive: true);
        // 2. Dịch vụ Inactive trong Chuyên khoa Active -> KHÔNG HIỂN THỊ
        var inactiveSrv = Service.Create(activeSpecialty.Id, "SRV-TAY-TRANG", "Tẩy trắng răng", 60, 1500000m, isActive: false);
        // 3. Dịch vụ Active trong Chuyên khoa Inactive -> KHÔNG HIỂN THỊ (FR-004)
        var hiddenSrv = Service.Create(inactiveSpecialty.Id, "SRV-TRI-LIEU", "Trị liệu tâm lý", 45, 500000m, isActive: true);

        db.Services.AddRange(activeSrv1, inactiveSrv, hiddenSrv);
        await db.SaveChangesAsync();

        var queryHandler = new ServiceQueryHandlers(db);

        // Act
        var result = await queryHandler.Handle(new GetActiveServicesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("SRV-CAO-VOI");
        result.First().SpecialtyName.Should().Be("Nha Khoa");
    }

    [Fact]
    public async Task GetActiveServices_FilteredBySlug_ShouldReturnOnlyMatchingSpecialtyServices()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var spec1 = Specialty.Create("CK-NHI", "Nhi Khoa", "nhi-khoa", isActive: true);
        var spec2 = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.AddRange(spec1, spec2);

        var srvNhi = Service.Create(spec1.Id, "SRV-KHAM-NHI", "Khám nhi tổng quát", 20, 150000m, isActive: true);
        var srvNoi = Service.Create(spec2.Id, "SRV-KHAM-NOI", "Khám nội tổng quát", 20, 180000m, isActive: true);
        db.Services.AddRange(srvNhi, srvNoi);
        await db.SaveChangesAsync();

        var queryHandler = new ServiceQueryHandlers(db);

        // Act
        var result = await queryHandler.Handle(new GetActiveServicesQuery(SpecialtySlug: "nhi-khoa"), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("SRV-KHAM-NHI");
        result.First().SpecialtySlug.Should().Be("nhi-khoa");
    }

    [Fact]
    public async Task UpdateService_WithValidData_ShouldUpdateServiceDetails()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var spec = Specialty.Create("CK-NGOAI", "Ngoại Khoa", "ngoai-khoa", isActive: true);
        db.Specialties.Add(spec);
        var srv = Service.Create(spec.Id, "SRV-TIEU-PHAU", "Tiểu phẫu cắt chỉ", 15, 100000m);
        db.Services.Add(srv);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);
        var updateReq = new UpdateServiceRequest(
            SpecialtyId: spec.Id,
            Name: "Tiểu phẫu cắt chỉ và thay băng vô trùng",
            DurationMinutes: 25,
            Price: 160000m,
            Description: "Cập nhật quy trình thay băng",
            IsActive: true
        );

        // Act
        var result = await handler.Handle(new UpdateServiceCommand(srv.Id, updateReq), CancellationToken.None);

        // Assert
        result.Name.Should().Be("Tiểu phẫu cắt chỉ và thay băng vô trùng");
        result.DurationMinutes.Should().Be(25);
        result.Price.Should().Be(160000m);
        result.Description.Should().Be("Cập nhật quy trình thay băng");
    }

    [Fact]
    public async Task SetServiceStatus_ShouldToggleActiveStatus()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(spec);
        var srv = Service.Create(spec.Id, "SRV-NOI", "Khám nội", 20, 100000m, isActive: true);
        db.Services.Add(srv);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);

        // Act
        var result = await handler.Handle(new SetServiceStatusCommand(srv.Id, false), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var srvInDb = await db.Services.FindAsync(srv.Id);
        srvInDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteService_WhenNoAppointments_ShouldHardDelete()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(spec);
        var srv = Service.Create(spec.Id, "SRV-TEMP", "Dịch vụ tạm", 15, 50000m);
        db.Services.Add(srv);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);

        // Act
        var result = await handler.Handle(new DeleteServiceCommand(srv.Id), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var srvInDb = await db.Services.FindAsync(srv.Id);
        srvInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteService_WhenHasAppointments_ShouldSoftDeleteBySettingInactive()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var spec = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(spec);
        var srv = Service.Create(spec.Id, "SRV-KHAM", "Khám tổng quát", 30, 200000m, isActive: true);
        db.Services.Add(srv);

        // Giả lập đã có Appointment sử dụng Service này
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var appointment = Appointment.CreateGuest(
            doctorId: doctorId,
            patientId: patientId,
            serviceId: srv.Id,
            serviceName: srv.Name,
            serviceDuration: 30,
            servicePrice: 200000m,
            startAtUtc: DateTime.UtcNow.AddDays(1),
            reasonForVisit: "Khám định kỳ"
        );
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var handler = new ServiceCommandHandlers(db);

        // Act: Thử xóa dịch vụ đã có lịch hẹn
        var result = await handler.Handle(new DeleteServiceCommand(srv.Id), CancellationToken.None);

        // Assert: Không được xóa vật lý để giữ snapshot & foreign key, chuyển thành Inactive
        result.Should().BeTrue();
        var srvInDb = await db.Services.FindAsync(srv.Id);
        srvInDb.Should().NotBeNull();
        srvInDb!.IsActive.Should().BeFalse();
    }
}
