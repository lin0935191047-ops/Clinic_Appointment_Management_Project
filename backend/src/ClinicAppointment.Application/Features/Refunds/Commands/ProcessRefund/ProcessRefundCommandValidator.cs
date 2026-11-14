using ClinicAppointment.Domain.Enums;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Refunds.Commands.ProcessRefund;

public class ProcessRefundCommandValidator : AbstractValidator<ProcessRefundCommand>
{
    public ProcessRefundCommandValidator()
    {
        RuleFor(x => x.RefundId)
            .NotEmpty().WithMessage("Mã yêu cầu hoàn tiền là bắt buộc.");

        RuleFor(x => x.TargetStatus)
            .Must(s => s == RefundStatus.Processing || s == RefundStatus.Succeeded || s == RefundStatus.Failed)
            .WithMessage("Trạng thái đích không hợp lệ. Chỉ chấp nhận Processing, Succeeded hoặc Failed.");

        When(x => x.TargetStatus == RefundStatus.Failed, () =>
        {
            RuleFor(x => x.FailureReason)
                .NotEmpty().WithMessage("Lý do hoàn tiền thất bại là bắt buộc khi chuyển trạng thái sang Failed.")
                .MaximumLength(500).WithMessage("Lý do thất bại không được vượt quá 500 ký tự.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ProviderReference), () =>
        {
            RuleFor(x => x.ProviderReference)
                .MaximumLength(150).WithMessage("Mã tham chiếu ngân hàng/phiếu chi không được vượt quá 150 ký tự.");
        });
    }
}
