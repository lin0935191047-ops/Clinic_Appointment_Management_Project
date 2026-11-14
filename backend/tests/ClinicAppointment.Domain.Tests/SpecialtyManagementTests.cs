using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Features.Specialties.Commands;
using ClinicAppointment.Application.Features.Specialties.DTOs;
using ClinicAppointment.Application.Features.Specialties.Queries;
using ClinicAppointment.Application.Features.Specialties.Validators;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class SpecialtyManagementTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Theory]
    [InlineData("Khoa Tai Mũi Họng", "khoa-tai-mui-hong")]
    [InlineData("Cơ - Xương - Khớp & Cột Sống!", "co-xuong-khop-cot-song")]
    [InlineData("Nhi Khoa (Trẻ em)", "nhi-khoa-tre-em")]
    [InlineData("Đa Khoa Quốc Tế", "da-khoa-quoc-te")]
    [InlineData("  Chẩn Đoán Hình Ảnh  ", "chan-doan-hinh-anh")]
    public void SlugHelper_GenerateSlug_ShouldConvertVietnameseCharactersProperly(string input, string expectedSlug)
    {
        // Act
        var result = SlugHelper.GenerateSlug(input);

        // Assert
        result.Should().Be(expectedSlug);
    }

    [Fact]
    public async Task CreateSpecialty_WithValidData_ShouldSucceed_AndAutoGenerateSlug()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var handler = new SpecialtyCommandHandlers(db);

        var request = new CreateSpecialtyRequest(
            Code: "CK-NOI",
            Name: "Nội Khoa Tổng Quát",
            Slug: null, // Không truyền slug -> Tự động sinh
            Description: "Khám chữa bệnh nội khoa tổng quát",
            IconUrl: "https://example.com/icons/internal-medicine.svg",
            IsActive: true
        );

        // Act
        var result = await handler.Handle(new CreateSpecialtyCommand(request), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("CK-NOI");
        result.Name.Should().Be("Nội Khoa Tổng Quát");
        result.Slug.Should().Be("noi-khoa-tong-quat"); // Auto generated slug
        result.IsActive.Should().BeTrue();

        var inDb = await db.Specialties.FirstOrDefaultAsync(s => s.Code == "CK-NOI");
        inDb.Should().NotBeNull();
        inDb!.Slug.Should().Be("noi-khoa-tong-quat");
    }

    [Fact]
    public async Task CreateSpecialty_WhenDuplicateCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var existing = Specialty.Create("CK-TIM", "Tim Mạch", "tim-mach");
        db.Specialties.Add(existing);
        await db.SaveChangesAsync();

        var handler = new SpecialtyCommandHandlers(db);
        var duplicateRequest = new CreateSpecialtyRequest("CK-TIM", "Khoa Tim Mạch Khác", "tim-mach-khac");

        // Act
        var act = async () => await handler.Handle(new CreateSpecialtyCommand(duplicateRequest), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã tồn tại trong hệ thống*");
    }

    [Fact]
    public async Task CreateSpecialty_WhenDuplicateSlug_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var existing = Specialty.Create("CK-DA-LIEU", "Da Liễu", "da-lieu");
        db.Specialties.Add(existing);
        await db.SaveChangesAsync();

        var handler = new SpecialtyCommandHandlers(db);
        // Code khác nhưng cùng slug 'da-lieu'
        var duplicateSlugRequest = new CreateSpecialtyRequest("CK-DA-LIEU-2", "Da Liễu Mới", "da-lieu");

        // Act
        var act = async () => await handler.Handle(new CreateSpecialtyCommand(duplicateSlugRequest), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã tồn tại*");
    }

    [Fact]
    public async Task GetActiveSpecialties_ShouldOnlyReturnActiveSpecialties_AndExcludeInactive()
    {
        // Arrange: Tạo 2 chuyên khoa Active và 1 Inactive
        using var db = CreateInMemoryDbContext();
        var sp1 = Specialty.Create("CK-01", "Chuyên khoa A", "chuyen-khoa-a", isActive: true);
        var sp2 = Specialty.Create("CK-02", "Chuyên khoa B", "chuyen-khoa-b", isActive: false); // Inactive
        var sp3 = Specialty.Create("CK-03", "Chuyên khoa C", "chuyen-khoa-c", isActive: true);

        db.Specialties.AddRange(sp1, sp2, sp3);
        await db.SaveChangesAsync();

        var queryHandler = new SpecialtyQueryHandlers(db);

        // Act: Public catalog query
        var result = await queryHandler.Handle(new GetActiveSpecialtiesQuery(), CancellationToken.None);

        // Assert: Chỉ hiển thị các chuyên khoa IsActive = true (FR-002 Invariant)
        result.Should().HaveCount(2);
        result.Select(s => s.Code).Should().Contain(new[] { "CK-01", "CK-03" });
        result.Select(s => s.Code).Should().NotContain("CK-02");
    }

    [Fact]
    public async Task GetSpecialtyBySlugOrId_ShouldFindActiveSpecialty_AndReturnNullForInactive_WhenNotIncluded()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var activeSp = Specialty.Create("CK-MAT", "Mắt", "khoa-mat", isActive: true);
        var inactiveSp = Specialty.Create("CK-RANG", "Răng Hàm Mặt", "rang-ham-mat", isActive: false);
        db.Specialties.AddRange(activeSp, inactiveSp);
        await db.SaveChangesAsync();

        var queryHandler = new SpecialtyQueryHandlers(db);

        // Act & Assert
        var foundActive = await queryHandler.Handle(new GetSpecialtyBySlugOrIdQuery("khoa-mat"), CancellationToken.None);
        foundActive.Should().NotBeNull();
        foundActive!.Name.Should().Be("Mắt");

        var foundInactivePublic = await queryHandler.Handle(new GetSpecialtyBySlugOrIdQuery("rang-ham-mat", IncludeInactive: false), CancellationToken.None);
        foundInactivePublic.Should().BeNull(); // Bị ẩn khỏi public catalog

        var foundInactiveAdmin = await queryHandler.Handle(new GetSpecialtyBySlugOrIdQuery("rang-ham-mat", IncludeInactive: true), CancellationToken.None);
        foundInactiveAdmin.Should().NotBeNull(); // Admin tra cứu được
    }

    [Fact]
    public async Task UpdateSpecialty_ShouldUpdateNameAndSlug_AndValidateDuplicateSlug()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var sp1 = Specialty.Create("CK-NHI", "Nhi Khoa", "nhi-khoa");
        var sp2 = Specialty.Create("CK-SAN", "Sản Phụ Khoa", "san-phu-khoa");
        db.Specialties.AddRange(sp1, sp2);
        await db.SaveChangesAsync();

        var handler = new SpecialtyCommandHandlers(db);

        // Act 1: Cập nhật hợp lệ
        var updateValid = new UpdateSpecialtyRequest("Nhi Khoa & Sơ Sinh", "nhi-khoa-so-sinh", "Mô tả mới");
        var result = await handler.Handle(new UpdateSpecialtyCommand(sp1.Id, updateValid), CancellationToken.None);

        result.Name.Should().Be("Nhi Khoa & Sơ Sinh");
        result.Slug.Should().Be("nhi-khoa-so-sinh");

        // Act 2: Cố tình đổi slug trùng với sp2 -> Phải throw exception
        var updateDuplicateSlug = new UpdateSpecialtyRequest("Tên khác", "san-phu-khoa");
        var actDuplicate = async () => await handler.Handle(new UpdateSpecialtyCommand(sp1.Id, updateDuplicateSlug), CancellationToken.None);
        await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã được sử dụng*");
    }

    [Fact]
    public async Task SetSpecialtyStatus_ShouldToggleActiveStatus()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var sp = Specialty.Create("CK-TEST", "Chuyên khoa thử nghiệm", "test-specialty", isActive: true);
        db.Specialties.Add(sp);
        await db.SaveChangesAsync();

        var handler = new SpecialtyCommandHandlers(db);

        // Act: Vô hiệu hóa
        await handler.Handle(new SetSpecialtyStatusCommand(sp.Id, false), CancellationToken.None);

        // Assert
        var inDb = await db.Specialties.FindAsync(sp.Id);
        inDb!.IsActive.Should().BeFalse();

        // Act: Kích hoạt lại
        await handler.Handle(new SetSpecialtyStatusCommand(sp.Id, true), CancellationToken.None);
        inDb.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSpecialty_WhenHasActiveServicesOrDoctors_ShouldDeactivateInsteadOfHardDelete()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var sp = Specialty.Create("CK-UNG-BUOU", "Ung Bướu", "ung-buou", isActive: true);
        db.Specialties.Add(sp);

        var service = new Service
        {
            SpecialtyId = sp.Id,
            Code = "SRV-UNG-BUOU-1",
            Name = "Khám sàng lọc ung bướu",
            Price = 500000m,
            IsActive = true
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();

        var handler = new SpecialtyCommandHandlers(db);

        // Act: Yêu cầu xóa chuyên khoa đang có dịch vụ liên kết
        var result = await handler.Handle(new DeleteSpecialtyCommand(sp.Id), CancellationToken.None);

        // Assert: Chuyên khoa không bị xóa vật lý (hard delete) mà chuyển sang Inactive (soft delete)
        result.Should().BeTrue();
        var inDb = await db.Specialties.FindAsync(sp.Id);
        inDb.Should().NotBeNull();
        inDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreateSpecialtyRequestValidator_ShouldValidateCodeAndName()
    {
        // Arrange
        var validator = new CreateSpecialtyRequestValidator();

        var emptyCode = new CreateSpecialtyRequest("", "Nội Khoa");
        var invalidCodeChars = new CreateSpecialtyRequest("CK NOI@", "Nội Khoa");
        var emptyName = new CreateSpecialtyRequest("CK-NOI", "");
        var valid = new CreateSpecialtyRequest("CK-NOI", "Nội Khoa", "noi-khoa");

        // Act & Assert
        validator.Validate(emptyCode).IsValid.Should().BeFalse();
        validator.Validate(invalidCodeChars).IsValid.Should().BeFalse();
        validator.Validate(emptyName).IsValid.Should().BeFalse();
        validator.Validate(valid).IsValid.Should().BeTrue();
    }
}
