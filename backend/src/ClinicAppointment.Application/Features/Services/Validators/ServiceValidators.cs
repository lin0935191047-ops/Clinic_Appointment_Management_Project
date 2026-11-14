using ClinicAppointment.Application.Features.Services.DTOs;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Services.Validators;

public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("Chuyên khoa liên kết không được để trống.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã dịch vụ không được để trống.")
            .MaximumLength(50).WithMessage("Mã dịch vụ không được vượt quá 50 ký tự.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Mã dịch vụ chỉ được chứa chữ cái in hoa, chữ số, gạch ngang hoặc gạch dưới.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên dịch vụ không được để trống.")
            .MaximumLength(150).WithMessage("Tên dịch vụ không được vượt quá 150 ký tự.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Thời lượng dịch vụ phải lớn hơn 0 phút.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Giá dịch vụ không được âm.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Đơn vị tiền tệ không được để trống.")
            .Length(3).WithMessage("Mã tiền tệ phải gồm 3 ký tự (ví dụ: VND, USD).");
    }
}

public class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("Chuyên khoa liên kết không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên dịch vụ không được để trống.")
            .MaximumLength(150).WithMessage("Tên dịch vụ không được vượt quá 150 ký tự.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Thời lượng dịch vụ phải lớn hơn 0 phút.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Giá dịch vụ không được âm.");
    }
}
