using FluentValidation;

namespace ClinicAppointment.Application.Features.Patients.Commands;

public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên bệnh nhân không được để trống.")
            .MaximumLength(200).WithMessage("Họ và tên bệnh nhân không được vượt quá 200 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại liên hệ là bắt buộc.")
            .Matches(@"^[0-9+\s\-()]{9,15}$").WithMessage("Số điện thoại không đúng định dạng hợp lệ.");

        RuleFor(x => x)
            .Must(HaveDateOfBirthOrBirthYear)
            .WithMessage("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.");

        RuleFor(x => x)
            .Must(BirthYearMatchesDateOfBirth)
            .WithMessage("Năm sinh và ngày sinh không khớp nhau.");

        When(x => x.Email != null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Địa chỉ email không hợp lệ.")
                .MaximumLength(254).WithMessage("Email không được vượt quá 254 ký tự.");
        });

        When(x => x.Gender != null, () =>
        {
            RuleFor(x => x.Gender)
                .MaximumLength(30).WithMessage("Giới tính không vượt quá 30 ký tự.");
        });
    }

    private static bool HaveDateOfBirthOrBirthYear(CreatePatientCommand cmd)
    {
        return cmd.DateOfBirth.HasValue || cmd.BirthYear.HasValue;
    }

    private static bool BirthYearMatchesDateOfBirth(CreatePatientCommand cmd)
    {
        if (cmd.DateOfBirth.HasValue && cmd.BirthYear.HasValue)
        {
            return cmd.DateOfBirth.Value.Year == cmd.BirthYear.Value;
        }
        return true;
    }
}
