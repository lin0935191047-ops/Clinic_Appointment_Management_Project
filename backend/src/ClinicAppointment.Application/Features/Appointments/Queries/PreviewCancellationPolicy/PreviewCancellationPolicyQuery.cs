using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Queries.PreviewCancellationPolicy;

public record CancellationPreviewDto(
    string BookingCode,
    DateTime AppointmentStartAtUtc,
    DateTime ServerAcceptedAtUtc,
    double MinutesBeforeAppointment,
    bool CanCancel,
    string? DenialReason,
    decimal PaidAmount,
    decimal CancellationFeeRate,
    decimal RefundRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount,
    string PolicyExplanation
);

public record PreviewCancellationPolicyQuery(
    string BookingCode,
    string? GrantToken = null
) : IRequest<Result<CancellationPreviewDto>>;

public class PreviewCancellationPolicyQueryHandler : IRequestHandler<PreviewCancellationPolicyQuery, Result<CancellationPreviewDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpGrantService _grantService;
    private readonly ICancellationPolicyService _policyService;

    public PreviewCancellationPolicyQueryHandler(
        IApplicationDbContext context,
        IOtpGrantService grantService,
        ICancellationPolicyService policyService)
    {
        _context = context;
        _grantService = grantService;
        _policyService = policyService;
    }

    public async Task<Result<CancellationPreviewDto>> Handle(
        PreviewCancellationPolicyQuery request, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.GrantToken))
        {
            return Result<CancellationPreviewDto>.Failure(
                "Yêu cầu xác thực OTP để xem chính sách hoàn tiền của ca khám. Vui lòng cung cấp mã quyền truy cập (Grant token).", 
                401);
        }

        var normalizedCode = (request.BookingCode ?? string.Empty).Trim().ToUpperInvariant();
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.BookingCode == normalizedCode, cancellationToken);

        if (appointment == null)
        {
            return Result<CancellationPreviewDto>.Failure("Không tìm thấy thông tin ca khám.", 404);
        }

        // Xác thực Grant token (cho phép grant CANCEL hoặc LOOKUP khi chỉ preview)
        var grantValidation = await _grantService.ValidateAndConsumeGrantAsync(
            request.GrantToken,
            appointment.Id,
            OtpPurpose.CANCEL,
            consume: false, // Preview không tiêu thụ grant
            cancellationToken);

        if (!grantValidation.IsSuccess)
        {
            // Thử kiểm tra grant LOOKUP nếu client truyền grant LOOKUP
            var lookupGrantValidation = await _grantService.ValidateAndConsumeGrantAsync(
                request.GrantToken,
                appointment.Id,
                OtpPurpose.LOOKUP,
                consume: false,
                cancellationToken);

            if (!lookupGrantValidation.IsSuccess)
            {
                return Result<CancellationPreviewDto>.Failure(
                    grantValidation.ErrorMessage ?? "Quyền truy cập (Grant token) không hợp lệ hoặc đã hết hạn.", 
                    403);
            }
        }

        var evaluation = _policyService.Evaluate(appointment.StartAt, appointment.PaidAmount);

        string explanation;
        if (!evaluation.CanCancel)
        {
            explanation = "Ca khám đã bắt đầu hoặc trong quá khứ. Bạn không thể tự hủy lịch khám này.";
        }
        else if (evaluation.MinutesBeforeAppointment >= 60.0)
        {
            explanation = $"Hủy trước giờ hẹn {Math.Floor(evaluation.MinutesBeforeAppointment)} phút (>= 60 phút): Hoàn 100% phí dịch vụ đã thanh toán ({evaluation.ExpectedRefundAmount:N0} đ), phí hủy 0%.";
        }
        else
        {
            explanation = $"Hủy trước giờ hẹn {Math.Floor(evaluation.MinutesBeforeAppointment)} phút (< 60 phút): Hoàn 50% phí dịch vụ đã thanh toán ({evaluation.ExpectedRefundAmount:N0} đ), phí hủy 50% ({evaluation.CancellationFeeAmount:N0} đ).";
        }

        var result = new CancellationPreviewDto(
            BookingCode: appointment.BookingCode,
            AppointmentStartAtUtc: appointment.StartAt,
            ServerAcceptedAtUtc: evaluation.ServerAcceptedAtUtc,
            MinutesBeforeAppointment: evaluation.MinutesBeforeAppointment,
            CanCancel: evaluation.CanCancel,
            DenialReason: evaluation.DenialReason,
            PaidAmount: appointment.PaidAmount,
            CancellationFeeRate: evaluation.CancellationFeeRate,
            RefundRate: evaluation.RefundRate,
            CancellationFeeAmount: evaluation.CancellationFeeAmount,
            ExpectedRefundAmount: evaluation.ExpectedRefundAmount,
            PolicyExplanation: explanation
        );

        return Result<CancellationPreviewDto>.Success(result, 200);
    }
}
