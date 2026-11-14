using ClinicAppointment.Application.Features.Auth.DTOs;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Auth.Validators;

public class RegisterPatientRequestValidator : AbstractValidator<RegisterPatientRequest>
{
    public RegisterPatientRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
            .MaximumLength(254).WithMessage("Email không được vượt quá 254 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(200).WithMessage("Họ và tên không được vượt quá 200 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^0\d{9}$").WithMessage("Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.");

        RuleFor(x => x)
            .Must(x => x.DateOfBirth.HasValue || x.BirthYear.HasValue)
            .WithMessage("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.");

        When(x => x.DateOfBirth.HasValue && x.BirthYear.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.DateOfBirth!.Value.Year == x.BirthYear!.Value)
                .WithMessage("Năm sinh và ngày sinh không khớp nhau.");
        });
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token không được để trống.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu mới phải chứa ít nhất một chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu mới phải chứa ít nhất một chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu mới phải chứa ít nhất một chữ số.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Mã xác thực không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu mới phải chứa ít nhất một chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu mới phải chứa ít nhất một chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu mới phải chứa ít nhất một chữ số.");
    }
}
