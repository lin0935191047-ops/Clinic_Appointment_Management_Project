using ClinicAppointment.Application.Features.Specialties.DTOs;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Specialties.Validators;

public class CreateSpecialtyRequestValidator : AbstractValidator<CreateSpecialtyRequest>
{
    public CreateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã chuyên khoa không được để trống.")
            .MaximumLength(50).WithMessage("Mã chuyên khoa không được vượt quá 50 ký tự.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Mã chuyên khoa chỉ được chứa chữ cái in hoa, chữ số, dấu gạch ngang hoặc gạch dưới.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chuyên khoa không được để trống.")
            .MaximumLength(100).WithMessage("Tên chuyên khoa không được vượt quá 100 ký tự.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug!)
                .MaximumLength(120).WithMessage("Slug không được vượt quá 120 ký tự.")
                .Matches(@"^[a-z0-9-]+$").WithMessage("Slug chỉ được chứa chữ cái thường, số và dấu gạch ngang.");
        });
    }
}

public class UpdateSpecialtyRequestValidator : AbstractValidator<UpdateSpecialtyRequest>
{
    public UpdateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chuyên khoa không được để trống.")
            .MaximumLength(100).WithMessage("Tên chuyên khoa không được vượt quá 100 ký tự.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug!)
                .MaximumLength(120).WithMessage("Slug không được vượt quá 120 ký tự.")
                .Matches(@"^[a-z0-9-]+$").WithMessage("Slug chỉ được chứa chữ cái thường, số và dấu gạch ngang.");
        });
    }
}
