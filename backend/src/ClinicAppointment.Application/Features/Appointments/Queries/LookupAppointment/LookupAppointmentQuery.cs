using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment;

public record PublicRefundSummaryDto(
    decimal ExpectedRefundAmount,
    decimal CancellationFeeAmount,
    decimal CancellationFeeRate,
    string PolicyVersion,
    RefundStatus Status,
    string StatusDescription,
    bool IsCompleted,
    DateTime? ProcessedAtUtc);

public record PublicAppointmentSummaryDto(
    string BookingCode,
    AppointmentStatus Status,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string PatientFullName,
    string DoctorFullName,
    string? DoctorTitle,
    string? DoctorRoomNumber,
    string SpecialtyName,
    string ServiceName,
    int ServiceDurationMinutes,
    decimal ServicePrice,
    decimal PaidAmount,
    string? ReasonForVisit,
    bool CanCancel,
    bool CanReschedule,
    PublicRefundSummaryDto? Refund = null);

public record LookupAppointmentRequest(
    string BookingCode,
    string? GrantToken = null);

public record LookupAppointmentQuery(
    string BookingCode,
    string? GrantToken) : IRequest<Result<PublicAppointmentSummaryDto>>;

public class LookupAppointmentQueryValidator : AbstractValidator<LookupAppointmentQuery>
{
    public LookupAppointmentQueryValidator()
    {
        RuleFor(x => x.BookingCode)
            .NotEmpty().WithMessage("Mã đặt lịch (Booking Code) là bắt buộc.")
            .MaximumLength(50).WithMessage("Mã đặt lịch không được vượt quá 50 ký tự.");

        RuleFor(x => x.GrantToken)
            .NotEmpty().WithMessage("Quyền truy cập (Grant token) từ xác thực OTP là bắt buộc để tra cứu lịch khám.");
    }
}

public class LookupAppointmentQueryHandler : IRequestHandler<LookupAppointmentQuery, Result<PublicAppointmentSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpGrantService _grantService;

    public LookupAppointmentQueryHandler(
        IApplicationDbContext context,
        IOtpGrantService grantService)
    {
        _context = context;
        _grantService = grantService;
    }

    public async Task<Result<PublicAppointmentSummaryDto>> Handle(
        LookupAppointmentQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Acceptance Criteria 1: Bắt buộc phải có Grant token từ xác thực OTP (Không tra cứu chỉ bằng mã booking)
        if (string.IsNullOrWhiteSpace(request.GrantToken))
        {
            return Result<PublicAppointmentSummaryDto>.Failure(
                "Yêu cầu xác thực OTP để tra cứu thông tin ca khám. Vui lòng cung cấp mã quyền truy cập (Grant token).", 
                401);
        }

        var normalizedCode = (request.BookingCode ?? string.Empty).Trim().ToUpperInvariant();

        // 2. Tra cứu ca khám kèm thông tin liên kết
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(a => a.BookingCode == normalizedCode, cancellationToken);

        if (appointment == null)
        {
            return Result<PublicAppointmentSummaryDto>.Failure(
                "Không tìm thấy thông tin ca khám với mã đặt lịch đã cung cấp.", 
                404);
        }

        // 3. Kiểm tra tính hợp lệ của Grant token (Scope Binding, Expiry, Purpose = LOOKUP)
        var grantValidation = await _grantService.ValidateAndConsumeGrantAsync(
            request.GrantToken,
            appointment.Id,
            OtpPurpose.LOOKUP,
            consume: false, // Tra cứu chỉ đọc, không consume ngay để người dùng có thể xem trong thời hạn grant
            cancellationToken);

        if (!grantValidation.IsSuccess)
        {
            return Result<PublicAppointmentSummaryDto>.Failure(
                grantValidation.ErrorMessage ?? "Quyền truy cập tra cứu không hợp lệ hoặc đã hết hạn.", 
                403);
        }

        // 4. Acceptance Criteria 2 & 3: Trả về summary tối thiểu, tuyệt đối KHÔNG trả về Consultation hoặc raw PII
        bool canModify = (appointment.Status == AppointmentStatus.PendingConfirmation || appointment.Status == AppointmentStatus.Confirmed) 
                         && appointment.StartAt > DateTime.UtcNow;

        PublicRefundSummaryDto? refundSummary = null;
        if (appointment.Status == AppointmentStatus.Cancelled && appointment.PaidAmount > 0)
        {
            var refund = await _context.Refunds
                .Where(r => r.AppointmentId == appointment.Id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (refund != null)
            {
                string statusDesc = refund.Status switch
                {
                    RefundStatus.Pending => "Yêu cầu hoàn tiền đang chờ bộ phận kế toán xử lý (Pending)",
                    RefundStatus.Processing => "Khoản hoàn tiền đang được đối soát ngân hàng (Processing)",
                    RefundStatus.Succeeded => "Đã hoàn tiền thành công vào tài khoản của quý khách (Succeeded)",
                    RefundStatus.Failed => "Xử lý hoàn tiền thất bại, vui lòng liên hệ nhân viên phòng khám",
                    _ => "Không áp dụng hoàn tiền"
                };

                refundSummary = new PublicRefundSummaryDto(
                    ExpectedRefundAmount: refund.ExpectedRefundAmount,
                    CancellationFeeAmount: refund.CancellationFeeAmount,
                    CancellationFeeRate: refund.CancellationFeeRate,
                    PolicyVersion: refund.PolicyVersion,
                    Status: refund.Status,
                    StatusDescription: statusDesc,
                    IsCompleted: refund.Status == RefundStatus.Succeeded,
                    ProcessedAtUtc: refund.ProcessedAt
                );
            }
        }

        var summary = new PublicAppointmentSummaryDto(
            BookingCode: appointment.BookingCode,
            Status: appointment.Status,
            StartAtUtc: appointment.StartAt,
            EndAtUtc: appointment.EndAt,
            PatientFullName: appointment.Patient?.FullName ?? "Bệnh nhân",
            DoctorFullName: appointment.Doctor?.FullName ?? "Bác sĩ phụ trách",
            DoctorTitle: appointment.Doctor?.Title,
            DoctorRoomNumber: appointment.Doctor?.RoomNumber,
            SpecialtyName: appointment.Doctor?.Specialty?.Name ?? string.Empty,
            ServiceName: appointment.ServiceNameSnapshot,
            ServiceDurationMinutes: appointment.ServiceDurationSnapshot,
            ServicePrice: appointment.ServicePriceSnapshot,
            PaidAmount: appointment.PaidAmount,
            ReasonForVisit: appointment.ReasonForVisit,
            CanCancel: canModify,
            CanReschedule: canModify,
            Refund: refundSummary);

        return Result<PublicAppointmentSummaryDto>.Success(summary, 200);
    }
}
