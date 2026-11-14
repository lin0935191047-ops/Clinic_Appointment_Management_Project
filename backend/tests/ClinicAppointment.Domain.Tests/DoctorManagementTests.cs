using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Doctors.Commands;
using ClinicAppointment.Application.Features.Doctors.DTOs;
using ClinicAppointment.Application.Features.Doctors.Queries;
using ClinicAppointment.Application.Features.Doctors.Validators;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class DoctorManagementTests
{
    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateDoctor_WithValidData_ShouldCreateDoctor_AndLinkStaffAccountWithDoctorRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NOI", "Nội Khoa", "noi-khoa", isActive: true);
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);
        var request = new CreateDoctorRequest(
            Email: "dr.nguyen@clinic.vn",
            Password: "SecureDoctor@123",
            SpecialtyId: specialty.Id,
            DoctorCode: "DOC-NGUYEN-A",
            FullName: "BS. Nguyễn Văn A",
            Title: "ThS. BS",
            Bio: "Chuyên gia tim mạch 15 năm kinh nghiệm",
            RoomNumber: "P.201",
            IsActive: true
        );

        // Act
        var result = await handler.Handle(new CreateDoctorCommand(request), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DoctorCode.Should().Be("DOC-NGUYEN-A");
        result.FullName.Should().Be("BS. Nguyễn Văn A");
        result.SpecialtyId.Should().Be(specialty.Id);
        result.SpecialtyName.Should().Be("Nội Khoa");
        result.Email.Should().Be("dr.nguyen@clinic.vn");

        // Kiểm tra tài khoản User được tạo kèm Role DOCTOR
        var userInDb = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == result.UserId);

        userInDb.Should().NotBeNull();
        userInDb!.UserRoles.Should().Contain(ur => ur.Role.Code == Role.DoctorCode);
    }

    [Fact]
    public async Task CreateDoctor_WhenUserAlreadyExists_ShouldLinkExistingUser_AndAssignDoctorRole()
    {
        // Arrange: User đã có trong hệ thống nhưng chưa có role DOCTOR
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-NHI", "Nhi Khoa", "nhi-khoa", isActive: true);
        var existingUser = User.Create("existing.staff@clinic.vn", _passwordHasher.HashPassword("Pass@123"));
        db.Specialties.Add(specialty);
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);
        var request = new CreateDoctorRequest(
            Email: "existing.staff@clinic.vn",
            Password: null,
            SpecialtyId: specialty.Id,
            DoctorCode: "DOC-TRAN-B",
            FullName: "BS. Trần Thị B"
        );

        // Act
        var result = await handler.Handle(new CreateDoctorCommand(request), CancellationToken.None);

        // Assert
        result.UserId.Should().Be(existingUser.Id);

        var updatedUser = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == existingUser.Id);

        updatedUser!.UserRoles.Should().Contain(ur => ur.Role.Code == Role.DoctorCode);
    }

    [Fact]
    public async Task CreateDoctor_WhenDoctorCodeAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-MAT", "Mắt", "mat", isActive: true);
        var existingDoc = Doctor.Create(Guid.NewGuid(), specialty.Id, "DOC-DUPLICATE", "Bác sĩ Cũ");
        db.Specialties.Add(specialty);
        db.Doctors.Add(existingDoc);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);
        var duplicateCodeRequest = new CreateDoctorRequest(
            Email: "new.doctor@clinic.vn",
            Password: null,
            SpecialtyId: specialty.Id,
            DoctorCode: "DOC-DUPLICATE", // Trùng code
            FullName: "Bác sĩ Mới"
        );

        // Act
        var act = async () => await handler.Handle(new CreateDoctorCommand(duplicateCodeRequest), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã tồn tại trong hệ thống*");
    }

    [Fact]
    public async Task CreateDoctor_WhenUserAlreadyLinkedToAnotherDoctor_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialty = Specialty.Create("CK-TAI", "Tai Mũi Họng", "tai-mui-hong", isActive: true);
        var user = User.Create("shared.email@clinic.vn", _passwordHasher.HashPassword("Pass@123"));
        var doctor1 = Doctor.Create(user.Id, specialty.Id, "DOC-001", "Bác sĩ 1");

        db.Specialties.Add(specialty);
        db.Users.Add(user);
        db.Doctors.Add(doctor1);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);
        var duplicateUserRequest = new CreateDoctorRequest(
            Email: "shared.email@clinic.vn", // Cố tình liên kết tài khoản đã gắn bác sĩ khác
            Password: null,
            SpecialtyId: specialty.Id,
            DoctorCode: "DOC-002",
            FullName: "Bác sĩ 2"
        );

        // Act
        var act = async () => await handler.Handle(new CreateDoctorCommand(duplicateUserRequest), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã được liên kết với một hồ sơ bác sĩ khác*");
    }

    [Fact]
    public async Task CreateDoctor_WhenSpecialtyNotFoundOrInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var inactiveSpecialty = Specialty.Create("CK-INACTIVE", "Chuyên khoa ẩn", "an", isActive: false);
        db.Specialties.Add(inactiveSpecialty);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);

        // Trường hợp 1: Chuyên khoa không tồn tại
        var notFoundRequest = new CreateDoctorRequest("doc1@clinic.vn", null, Guid.NewGuid(), "DOC-X", "Bác sĩ X");
        var actNotFound = async () => await handler.Handle(new CreateDoctorCommand(notFoundRequest), CancellationToken.None);
        await actNotFound.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không tồn tại*");

        // Trường hợp 2: Chuyên khoa bị vô hiệu hóa
        var inactiveRequest = new CreateDoctorRequest("doc2@clinic.vn", null, inactiveSpecialty.Id, "DOC-Y", "Bác sĩ Y");
        var actInactive = async () => await handler.Handle(new CreateDoctorCommand(inactiveRequest), CancellationToken.None);
        await actInactive.Should().ThrowAsync<InvalidOperationException>().WithMessage("*hiện đang bị vô hiệu hóa*");
    }

    [Fact]
    public async Task GetActiveDoctors_ShouldOnlyReturnActiveDoctors_WithActiveSpecialties()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var activeSpecialty = Specialty.Create("CK-A", "Khoa A", "khoa-a", isActive: true);
        var inactiveSpecialty = Specialty.Create("CK-B", "Khoa B", "khoa-b", isActive: false);
        db.Specialties.AddRange(activeSpecialty, inactiveSpecialty);

        var doc1 = Doctor.Create(Guid.NewGuid(), activeSpecialty.Id, "DOC-1", "Bác sĩ 1", isActive: true);  // Hợp lệ
        var doc2 = Doctor.Create(Guid.NewGuid(), activeSpecialty.Id, "DOC-2", "Bác sĩ 2", isActive: false); // Inactive doctor
        var doc3 = Doctor.Create(Guid.NewGuid(), inactiveSpecialty.Id, "DOC-3", "Bác sĩ 3", isActive: true); // Inactive specialty
        var doc4 = Doctor.Create(Guid.NewGuid(), activeSpecialty.Id, "DOC-4", "Bác sĩ 4", isActive: true);  // Hợp lệ

        db.Doctors.AddRange(doc1, doc2, doc3, doc4);
        await db.SaveChangesAsync();

        var queryHandler = new DoctorQueryHandlers(db);

        // Act: Public catalog query (FR-003 Invariant)
        var result = await queryHandler.Handle(new GetActiveDoctorsQuery(), CancellationToken.None);

        // Assert: Chỉ hiển thị Bác sĩ 1 và Bác sĩ 4
        result.Should().HaveCount(2);
        result.Select(d => d.DoctorCode).Should().Contain(new[] { "DOC-1", "DOC-4" });
        result.Select(d => d.DoctorCode).Should().NotContain(new[] { "DOC-2", "DOC-3" });
    }

    [Fact]
    public async Task GetActiveDoctors_FilterBySpecialtySlug_ShouldReturnMatchingDoctors()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var specialtyTim = Specialty.Create("CK-TIM", "Tim Mạch", "tim-mach", isActive: true);
        var specialtyMat = Specialty.Create("CK-MAT", "Mắt", "mat", isActive: true);
        db.Specialties.AddRange(specialtyTim, specialtyMat);

        var docTim = Doctor.Create(Guid.NewGuid(), specialtyTim.Id, "DOC-TIM-1", "Bác sĩ Tim");
        var docMat = Doctor.Create(Guid.NewGuid(), specialtyMat.Id, "DOC-MAT-1", "Bác sĩ Mắt");
        db.Doctors.AddRange(docTim, docMat);
        await db.SaveChangesAsync();

        var queryHandler = new DoctorQueryHandlers(db);

        // Act: Lọc theo slug "tim-mach"
        var result = await queryHandler.Handle(new GetActiveDoctorsQuery(SpecialtySlug: "tim-mach"), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].DoctorCode.Should().Be("DOC-TIM-1");
    }

    [Fact]
    public async Task UpdateDoctor_ShouldUpdateDetailsAndSpecialty()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var sp1 = Specialty.Create("CK-1", "Khoa 1", "khoa-1", isActive: true);
        var sp2 = Specialty.Create("CK-2", "Khoa 2", "khoa-2", isActive: true);
        var user = User.Create("doc.update@clinic.vn", "hash");
        var doctor = Doctor.Create(user.Id, sp1.Id, "DOC-UP", "BS. Cũ", "BS");

        db.Specialties.AddRange(sp1, sp2);
        db.Users.Add(user);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);
        var updateRequest = new UpdateDoctorRequest(
            SpecialtyId: sp2.Id,
            FullName: "BS. CKI. Mới",
            Title: "BS. CKI",
            Bio: "Tiểu sử cập nhật",
            RoomNumber: "P.305"
        );

        // Act
        var result = await handler.Handle(new UpdateDoctorCommand(doctor.Id, updateRequest), CancellationToken.None);

        // Assert
        result.FullName.Should().Be("BS. CKI. Mới");
        result.SpecialtyId.Should().Be(sp2.Id);
        result.SpecialtyName.Should().Be("Khoa 2");
        result.RoomNumber.Should().Be("P.305");
    }

    [Fact]
    public async Task SetDoctorStatus_ShouldToggleActiveStatus()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctor = Doctor.Create(Guid.NewGuid(), Guid.NewGuid(), "DOC-TOGGLE", "BS. Toggle", isActive: true);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);

        // Act: Tắt
        await handler.Handle(new SetDoctorStatusCommand(doctor.Id, false), CancellationToken.None);
        var inDb = await db.Doctors.FindAsync(doctor.Id);
        inDb!.IsActive.Should().BeFalse();

        // Act: Bật lại
        await handler.Handle(new SetDoctorStatusCommand(doctor.Id, true), CancellationToken.None);
        inDb.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDoctor_WhenHasAppointments_ShouldDeactivateInsteadOfHardDelete()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctor = Doctor.Create(Guid.NewGuid(), Guid.NewGuid(), "DOC-BUSY", "BS. Bận", isActive: true);
        db.Doctors.Add(doctor);

        var apt = Appointment.CreateGuest(
            doctor.Id, Guid.NewGuid(), Guid.NewGuid(),
            "Khám bệnh", 30, 200000m, DateTime.UtcNow.AddDays(1), "Khám tổng quát");
        db.Appointments.Add(apt);
        await db.SaveChangesAsync();

        var handler = new DoctorCommandHandlers(db, _passwordHasher);

        // Act: Xóa bác sĩ có lịch hẹn
        var result = await handler.Handle(new DeleteDoctorCommand(doctor.Id), CancellationToken.None);

        // Assert: Chuyển sang Inactive thay vì xóa vật lý (Soft delete)
        result.Should().BeTrue();
        var inDb = await db.Doctors.FindAsync(doctor.Id);
        inDb.Should().NotBeNull();
        inDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreateDoctorRequestValidator_ShouldValidateFields()
    {
        // Arrange
        var validator = new CreateDoctorRequestValidator();

        var invalidEmail = new CreateDoctorRequest("not-an-email", "Pass@123", Guid.NewGuid(), "DOC-01", "Bác sĩ A");
        var emptyCode = new CreateDoctorRequest("doc@clinic.vn", "Pass@123", Guid.NewGuid(), "", "Bác sĩ A");
        var invalidCodeChars = new CreateDoctorRequest("doc@clinic.vn", "Pass@123", Guid.NewGuid(), "DOC 01@", "Bác sĩ A");
        var emptyName = new CreateDoctorRequest("doc@clinic.vn", "Pass@123", Guid.NewGuid(), "DOC-01", "");
        var valid = new CreateDoctorRequest("doc@clinic.vn", "Pass@123", Guid.NewGuid(), "DOC-01", "BS. Nguyễn Văn A");

        // Act & Assert
        validator.Validate(invalidEmail).IsValid.Should().BeFalse();
        validator.Validate(emptyCode).IsValid.Should().BeFalse();
        validator.Validate(invalidCodeChars).IsValid.Should().BeFalse();
        validator.Validate(emptyName).IsValid.Should().BeFalse();
        validator.Validate(valid).IsValid.Should().BeTrue();
    }
}
