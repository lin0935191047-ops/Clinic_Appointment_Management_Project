using ClinicAppointment.Application.Features.Doctors.DTOs;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Doctors.Validators;

public class CreateDoctorRequestValidator : AbstractValidator<CreateDoctorRequest>
{
    public CreateDoctorRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email tài khoản bác sĩ không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
            .MaximumLength(254).WithMessage("Email không được vượt quá 254 ký tự.");

        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("Chuyên khoa liên kết không được để trống.");

        RuleFor(x => x.DoctorCode)
            .NotEmpty().WithMessage("Mã bác sĩ không được để trống.")
            .MaximumLength(32).WithMessage("Mã bác sĩ không được vượt quá 32 ký tự.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Mã bác sĩ chỉ được chứa chữ cái in hoa, chữ số, gạch ngang hoặc gạch dưới.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên bác sĩ không được để trống.")
            .MaximumLength(150).WithMessage("Họ và tên không được vượt quá 150 ký tự.");
    }
}

public class UpdateDoctorRequestValidator : AbstractValidator<UpdateDoctorRequest>
{
    public UpdateDoctorRequestValidator()
    {
        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("Chuyên khoa liên kết không được để trống.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên bác sĩ không được để trống.")
            .MaximumLength(150).WithMessage("Họ và tên không được vượt quá 150 ký tự.");
    }
}
