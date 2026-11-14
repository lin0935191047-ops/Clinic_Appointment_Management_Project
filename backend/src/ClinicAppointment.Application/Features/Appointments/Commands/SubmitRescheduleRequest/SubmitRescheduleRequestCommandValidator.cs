using FluentValidation;

namespace ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest;

public class SubmitRescheduleRequestCommandValidator : AbstractValidator<SubmitRescheduleRequestCommand>
{
    public SubmitRescheduleRequestCommandValidator()
    {
        RuleFor(x => x.BookingCode)
            .NotEmpty().WithMessage("Mã đặt lịch (Booking Code) là bắt buộc.")
            .MaximumLength(50).WithMessage("Mã đặt lịch không được vượt quá 50 ký tự.");

        RuleFor(x => x.GrantToken)
            .NotEmpty().WithMessage("Quyền truy cập (Grant token) từ xác thực OTP là bắt buộc để gửi yêu cầu đổi lịch.");

        RuleFor(x => x.ProposedStartAtUtc)
            .NotEmpty().WithMessage("Thời gian đề xuất đổi lịch là bắt buộc.")
            .Must(t => t > DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Thời gian đề xuất đổi lịch phải ở tương lai.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Lý do đổi lịch không được vượt quá 500 ký tự.");
    }
}
