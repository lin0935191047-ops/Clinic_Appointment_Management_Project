using ClinicAppointment.Application.Features.Patients.Commands;
using ClinicAppointment.Domain.Entities;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PatientProfileTests
{
    private readonly CreatePatientCommandValidator _validator = new();

    #region Domain Entity Tests (FEAT-011 Invariants)

    [Fact]
    public void Patient_Create_GuestMode_UserIdShouldBeNull()
    {
        var patient = Patient.Create("Nguyen Van An", "0912345678", new DateOnly(1990, 8, 15));

        patient.UserId.Should().BeNull();
        patient.FullName.Should().Be("Nguyen Van An");
        patient.NormalizedFullName.Should().Be("NGUYEN VAN AN");
        patient.DateOfBirth.Should().Be(new DateOnly(1990, 8, 15));
        patient.BirthYear.Should().Be(1990);
        patient.RecordStatus.Should().Be("ACTIVE");
        patient.PatientCode.Should().StartWith("PAT-");
    }

    [Fact]
    public void Patient_Create_WithRegisteredUser_UserIdShouldBeAssigned()
    {
        var userId = Guid.NewGuid();
        var patient = Patient.Create("Tran Thi Mai", "0987654321", null, 1985, userId: userId);

        patient.UserId.Should().Be(userId);
        patient.DateOfBirth.Should().BeNull();
        patient.BirthYear.Should().Be(1985);
        patient.FullName.Should().Be("Tran Thi Mai");
        patient.NormalizedFullName.Should().Be("TRAN THI MAI");
    }

    [Fact]
    public void Patient_NormalizeFullName_ShouldCollapseExtraWhitespace_AndUppercase()
    {
        var normalized = Patient.NormalizeFullName("   nguyen     thi    hong   loan   ");
        normalized.Should().Be("NGUYEN THI HONG LOAN");
    }

    [Fact]
    public void Patient_LinkToUser_WhenInitiallyNull_ShouldSucceed()
    {
        var patient = Patient.Create("Le Van B", "0909090909", new DateOnly(2000, 1, 1));
        var newUserId = Guid.NewGuid();

        patient.LinkToUser(newUserId);

        patient.UserId.Should().Be(newUserId);
    }

    [Fact]
    public void Patient_LinkToUser_WhenAlreadyLinkedToAnother_ShouldThrow()
    {
        var existingUserId = Guid.NewGuid();
        var patient = Patient.Create("Le Van B", "0909090909", new DateOnly(2000, 1, 1), userId: existingUserId);

        var act = () => patient.LinkToUser(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*đã được liên kết với một tài khoản khác*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Patient_Create_EmptyFullName_ShouldThrowArgumentException(string? invalidName)
    {
        var act = () => Patient.Create(invalidName!, "0912345678", new DateOnly(1990, 1, 1));
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Họ và tên bệnh nhân không được để trống*");
    }

    [Fact]
    public void Patient_Create_WithoutDobAndWithoutBirthYear_ShouldThrow()
    {
        var act = () => Patient.Create("Hoang Van C", "0912345678", null, null);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh*");
    }

    [Fact]
    public void Patient_Create_MismatchedDobAndBirthYear_ShouldThrow()
    {
        var act = () => Patient.Create("Hoang Van C", "0912345678", new DateOnly(1992, 4, 10), 1999);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Năm sinh và ngày sinh không khớp nhau*");
    }

    #endregion

    #region FluentValidation Tests for CreatePatientCommand

    [Fact]
    public void Validator_ValidCommand_WithDobOnly_ShouldPass()
    {
        var command = new CreatePatientCommand(
            FullName: "Phan Van Dat",
            PhoneNumber: "0912345678",
            DateOfBirth: new DateOnly(1987, 10, 20)
        );

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_ValidCommand_WithBirthYearOnly_ShouldPass()
    {
        var command = new CreatePatientCommand(
            FullName: "Phan Thi Hoa",
            PhoneNumber: "0988776655",
            BirthYear: 1960
        );

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_MissingBothDobAndBirthYear_ShouldFail()
    {
        var command = new CreatePatientCommand(
            FullName: "Nguyen Khong Tuoi",
            PhoneNumber: "0912345678"
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.");
    }

    [Fact]
    public void Validator_MismatchedDobAndBirthYear_ShouldFail()
    {
        var command = new CreatePatientCommand(
            FullName: "Nguyen Sai Nam",
            PhoneNumber: "0912345678",
            DateOfBirth: new DateOnly(1990, 5, 1),
            BirthYear: 1995
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Năm sinh và ngày sinh không khớp nhau.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_EmptyFullName_ShouldFail(string name)
    {
        var command = new CreatePatientCommand(
            FullName: name,
            PhoneNumber: "0912345678",
            DateOfBirth: new DateOnly(1990, 1, 1)
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public void Validator_InvalidPhoneNumber_ShouldFail(string phone)
    {
        var command = new CreatePatientCommand(
            FullName: "Nguyen Van A",
            PhoneNumber: phone,
            DateOfBirth: new DateOnly(1990, 1, 1)
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion
}
