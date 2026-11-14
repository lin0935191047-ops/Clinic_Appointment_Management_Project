using FluentValidation;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;

public class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.BookingCode)
            .NotEmpty().WithMessage("Mã đặt lịch (Booking Code) là bắt buộc.")
            .MaximumLength(50).WithMessage("Mã đặt lịch không được vượt quá 50 ký tự.");

        RuleFor(x => x.GrantToken)
            .NotEmpty().WithMessage("Quyền truy cập (Grant token) từ xác thực OTP là bắt buộc để hủy lịch khám.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Lý do hủy lịch không được vượt quá 500 ký tự.");
    }
}
