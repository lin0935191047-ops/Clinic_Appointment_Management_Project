using ClinicAppointment.Domain.Enums;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Otp.DTOs;

public record RequestOtpRequest(
    string BookingCode,
    string PhoneNumber,
    OtpPurpose Purpose);

public record RequestOtpResponse(
    bool Success,
    string Message,
    Guid ChallengeId,
    int CooldownSeconds);

public record VerifyOtpRequest(
    Guid ChallengeId,
    string Otp,
    OtpPurpose Purpose);

public record VerifyOtpResponse(
    bool Success,
    string? Message,
    Guid? ChallengeId,
    Guid? AppointmentId,
    OtpPurpose? Purpose,
    string? GrantToken = null,
    DateTime? GrantExpiresAt = null,
    string TokenType = "ScopedAppointmentGrant");

public class RequestOtpRequestValidator : AbstractValidator<RequestOtpRequest>
{
    public RequestOtpRequestValidator()
    {
        RuleFor(x => x.BookingCode)
            .NotEmpty().WithMessage("Mã đặt lịch là bắt buộc.")
            .MaximumLength(50).WithMessage("Mã đặt lịch không được vượt quá 50 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại liên hệ là bắt buộc.")
            .Matches(@"^(0[3|5|7|8|9])[0-9]{8}$").WithMessage("Số điện thoại di động không hợp lệ.");

        RuleFor(x => x.Purpose)
            .IsInEnum().WithMessage("Mục đích yêu cầu OTP không hợp lệ.");
    }
}

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty().WithMessage("Challenge ID xác thực là bắt buộc.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Mã OTP là bắt buộc.")
            .Length(6).WithMessage("Mã OTP phải gồm đúng 6 chữ số.")
            .Matches(@"^\d{6}$").WithMessage("Mã OTP chỉ được chứa ký số.");

        RuleFor(x => x.Purpose)
            .IsInEnum().WithMessage("Mục đích xác thực không hợp lệ.");
    }
}
