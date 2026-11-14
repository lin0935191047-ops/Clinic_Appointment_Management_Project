using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result<CancelAppointmentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpGrantService _grantService;
    private readonly ICancellationPolicyService _policyService;

    public CancelAppointmentCommandHandler(
        IApplicationDbContext context,
        IOtpGrantService grantService,
        ICancellationPolicyService policyService)
    {
        _context = context;
        _grantService = grantService;
        _policyService = policyService;
    }

    public async Task<Result<CancelAppointmentResponse>> Handle(
        CancelAppointmentCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Acceptance Criteria 1: Bắt buộc phải có Grant token từ xác thực OTP
        if (string.IsNullOrWhiteSpace(request.GrantToken))
        {
            return Result<CancelAppointmentResponse>.Failure(
                "Yêu cầu xác thực OTP để hủy lịch khám. Vui lòng cung cấp mã quyền truy cập (Grant token).", 
                401);
        }

        var normalizedCode = (request.BookingCode ?? string.Empty).Trim().ToUpperInvariant();

        // 2. Tra cứu thông tin ca khám
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.BookingCode == normalizedCode, cancellationToken);

        if (appointment == null)
        {
            return Result<CancelAppointmentResponse>.Failure(
                "Không tìm thấy thông tin ca khám cần hủy.", 
                404);
        }

        // 3. Acceptance Criteria 3: Kiểm tra trạng thái cuộc hẹn
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<CancelAppointmentResponse>.Failure(
                "Lịch khám này đã bị hủy trước đó.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<CancelAppointmentResponse>.Failure(
                "Không thể hủy ca khám đã hoàn thành.", 
                400);
        }

        if (appointment.Status != AppointmentStatus.PendingConfirmation && appointment.Status != AppointmentStatus.Confirmed)
        {
            return Result<CancelAppointmentResponse>.Failure(
                $"Không thể hủy ca khám ở trạng thái {appointment.Status}.", 
                400);
        }

        // 4. Acceptance Criteria 1: Xác thực và tiêu thụ Scoped Grant Token (Purpose: CANCEL, One-time use)
        var grantValidation = await _grantService.ValidateAndConsumeGrantAsync(
            request.GrantToken,
            appointment.Id,
            OtpPurpose.CANCEL,
            consume: true,
            cancellationToken);

        if (!grantValidation.IsSuccess)
        {
            return Result<CancelAppointmentResponse>.Failure(
                grantValidation.ErrorMessage ?? "Quyền truy cập (Grant token) không hợp lệ hoặc đã hết hạn.", 
                403);
        }

        // 5. Acceptance Criteria 2: Kiểm tra Server Clock Timestamp & Đánh giá chính sách hủy (Self-cancel before StartAt)
        var serverAcceptedAtUtc = DateTime.UtcNow;
        var evaluation = _policyService.Evaluate(appointment.StartAt, appointment.PaidAmount, serverAcceptedAtUtc);

        if (!evaluation.CanCancel)
        {
            return Result<CancelAppointmentResponse>.Failure(
                evaluation.DenialReason ?? "Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ.", 
                400);
        }

        // 6. Acceptance Criteria 4: Thực thi trong Database Transaction và giải phóng slot (Slot released transactionally)
        bool supportsTransaction = _context.Database.ProviderName != null &&
            !_context.Database.ProviderName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

        IDbContextTransaction? transaction = null;
        if (supportsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            // 6.1 Chuyển trạng thái Appointment sang Cancelled
            var cancelReason = string.IsNullOrWhiteSpace(request.Reason) 
                ? "Bệnh nhân chủ động hủy lịch" 
                : request.Reason.Trim();

            appointment.Cancel(cancelReason, serverAcceptedAtUtc);

            // 6.2 Tạo bản ghi Refund nếu đã phát sinh thanh toán (PaidAmount > 0)
            Refund? refund = null;
            var refundStatus = RefundStatus.NotApplicable;

            if (appointment.PaidAmount > 0)
            {
                refundStatus = RefundStatus.Pending;
                refund = Refund.Create(
                    appointmentId: appointment.Id,
                    paidServiceAmount: appointment.PaidAmount,
                    cancellationFeeRate: evaluation.CancellationFeeRate,
                    cancellationFeeAmount: evaluation.CancellationFeeAmount,
                    expectedRefundAmount: evaluation.ExpectedRefundAmount,
                    reason: cancelReason,
                    status: RefundStatus.Pending,
                    policyVersion: "v1.0"
                );
                _context.Refunds.Add(refund);
            }

            // 6.3 Ghi nhận lịch sử kiểm toán (Audit Log)
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var metadata = new
            {
                BookingCode = appointment.BookingCode,
                CancellationAcceptedAt = serverAcceptedAtUtc,
                MinutesBeforeAppointment = evaluation.MinutesBeforeAppointment,
                PaidAmount = appointment.PaidAmount,
                CancellationFeeRate = evaluation.CancellationFeeRate,
                RefundRate = evaluation.RefundRate,
                CancellationFeeAmount = evaluation.CancellationFeeAmount,
                ExpectedRefundAmount = evaluation.ExpectedRefundAmount,
                PolicyVersion = "v1.0",
                RefundStatus = refundStatus.ToString(),
                Reason = cancelReason
            };

            var auditLog = AuditLog.Create(
                actorType: appointment.IsGuest ? "GUEST" : "PATIENT",
                actorUserId: null,
                action: "CANCEL_APPOINTMENT",
                entityType: "Appointment",
                entityId: appointment.Id,
                metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
            );

            _context.AuditLogs.Add(auditLog);

            // 6.4 Lưu thay đổi và Commit Transaction
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            // 7. Tạo thông điệp phản hồi rõ ràng, phân biệt giữa số tiền dự kiến hoàn và trạng thái xử lý hoàn tiền
            string message;
            if (appointment.PaidAmount > 0)
            {
                message = $"Lịch hẹn đã được hủy thành công. Số tiền hoàn dự kiến là {evaluation.ExpectedRefundAmount:N0} đ (tỷ lệ hoàn: {evaluation.RefundRate * 100:0}%, phí hủy: {evaluation.CancellationFeeAmount:N0} đ). Yêu cầu hoàn tiền đang ở trạng thái Chờ xử lý (Pending).";
            }
            else
            {
                message = "Lịch hẹn đã được hủy thành công. Ca khám chưa phát sinh thanh toán nên không có khoản tiền hoàn.";
            }

            var response = new CancelAppointmentResponse(
                AppointmentId: appointment.Id,
                BookingCode: appointment.BookingCode,
                Status: appointment.Status,
                CancellationAcceptedAtUtc: serverAcceptedAtUtc,
                CancellationReason: appointment.CancellationReason,
                PaidAmount: appointment.PaidAmount,
                CancellationFeeRate: evaluation.CancellationFeeRate,
                RefundRate: evaluation.RefundRate,
                CancellationFeeAmount: evaluation.CancellationFeeAmount,
                ExpectedRefundAmount: evaluation.ExpectedRefundAmount,
                RefundStatus: refundStatus,
                Message: message,
                PolicyVersion: "v1.0",
                IsRefundCompleted: refundStatus == RefundStatus.Succeeded
            );

            return Result<CancelAppointmentResponse>.Success(response, 200);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }
}
