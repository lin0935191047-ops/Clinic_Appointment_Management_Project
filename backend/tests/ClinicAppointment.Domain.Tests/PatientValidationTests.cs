using ClinicAppointment.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PatientValidationTests
{
    [Fact]
    public void Create_WithFullNameAndDateOfBirth_ShouldSucceed()
    {
        // Arrange & Act
        var patient = Patient.Create("Nguyen Van A", "0901234567", new DateOnly(1995, 5, 20));

        // Assert
        patient.FullName.Should().Be("Nguyen Van A");
        patient.PhoneNumber.Should().Be("0901234567");
        patient.DateOfBirth.Should().Be(new DateOnly(1995, 5, 20));
        patient.BirthYear.Should().Be(1995);
        patient.UserId.Should().BeNull(); // Guest booking profile
        patient.PatientCode.Should().StartWith("PAT-");
    }

    [Fact]
    public void Create_WithFullNameAndBirthYearOnly_ShouldSucceed()
    {
        // Arrange & Act
        var patient = Patient.Create("Tran Thi B", "0912345678", null, 1988);

        // Assert
        patient.FullName.Should().Be("Tran Thi B");
        patient.DateOfBirth.Should().BeNull();
        patient.BirthYear.Should().Be(1988);
    }

    [Fact]
    public void Create_MissingBothDateOfBirthAndBirthYear_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Patient.Create("Le Van C", "0923456789", null, null);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh*");
    }

    [Fact]
    public void Create_MismatchedDateOfBirthAndBirthYear_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Patient.Create("Pham Van D", "0934567890", new DateOnly(1990, 1, 1), 1995);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Năm sinh và ngày sinh không khớp nhau*");
    }
}

