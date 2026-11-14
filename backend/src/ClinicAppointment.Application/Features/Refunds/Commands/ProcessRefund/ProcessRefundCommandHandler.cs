using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Refunds.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Refunds.Commands.ProcessRefund;

public class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, Result<RefundDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ProcessRefundCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<RefundDetailDto>> Handle(
        ProcessRefundCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ nhân viên (Receptionist / Admin) mới được xử lý hoàn tiền
        if (!_currentUserService.IsStaff)
        {
            return Result<RefundDetailDto>.Failure("Bạn không có quyền xử lý hoàn tiền. Yêu cầu quyền Receptionist hoặc Admin.", 403);
        }

        var staffUserId = _currentUserService.UserId ?? Guid.Empty;
        if (staffUserId == Guid.Empty)
        {
            return Result<RefundDetailDto>.Failure("Không xác định được danh tính nhân viên đang thực hiện thao tác.", 401);
        }

        // 2. Tra cứu bản ghi hoàn tiền
        var refund = await _context.Refunds
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Doctor)
            .FirstOrDefaultAsync(r => r.Id == request.RefundId, cancellationToken);

        if (refund == null)
        {
            return Result<RefundDetailDto>.Failure("Không tìm thấy thông tin bản ghi hoàn tiền.", 404);
        }

        // 3. Nghiệp vụ: Khoản hoàn tiền đã hoàn tất (Succeeded) không được sửa đổi lại để tránh double refund
        if (refund.Status == RefundStatus.Succeeded)
        {
            return Result<RefundDetailDto>.Failure("Khoản hoàn tiền này đã được xác nhận hoàn tất thành công trước đó, không thể chỉnh sửa trạng thái.", 400);
        }

        var previousStatus = refund.Status;
        var nowUtc = DateTime.UtcNow;

        // 4. Cập nhật trạng thái
        try
        {
            switch (request.TargetStatus)
            {
                case RefundStatus.Processing:
                    refund.Process(staffUserId, request.ProviderReference);
                    break;

                case RefundStatus.Succeeded:
                    refund.MarkSucceeded(staffUserId, request.ProviderReference, nowUtc);
                    break;

                case RefundStatus.Failed:
                    if (string.IsNullOrWhiteSpace(request.FailureReason))
                    {
                        return Result<RefundDetailDto>.Failure("Lý do hoàn tiền thất bại là bắt buộc khi chuyển trạng thái sang Failed.", 400);
                    }
                    refund.MarkFailed(staffUserId, request.FailureReason, nowUtc);
                    break;

                default:
                    return Result<RefundDetailDto>.Failure("Trạng thái đích không được hỗ trợ xử lý.", 400);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result<RefundDetailDto>.Failure(ex.Message, 400);
        }

        // 5. Ghi nhận lịch sử kiểm toán an toàn (Audit Log)
        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var metadata = new
        {
            PreviousStatus = previousStatus.ToString(),
            TargetStatus = refund.Status.ToString(),
            BookingCode = refund.Appointment?.BookingCode,
            ExpectedRefundAmount = refund.ExpectedRefundAmount,
            PaidServiceAmount = refund.PaidServiceAmount,
            CancellationFeeAmount = refund.CancellationFeeAmount,
            PolicyVersion = refund.PolicyVersion,
            ProviderReference = refund.ProviderReference,
            FailureReason = refund.FailureReason,
            Note = request.Note?.Trim()
        };

        var auditLog = AuditLog.Create(
            actorType: "STAFF",
            actorUserId: staffUserId,
            action: "PROCESS_REFUND",
            entityType: "Refund",
            entityId: refund.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        // 6. Trả về kết quả
        string statusDesc = refund.Status switch
        {
            RefundStatus.Pending => "Chờ xử lý hoàn tiền (Pending)",
            RefundStatus.Processing => "Đang đối soát ngân hàng / xử lý chi tiền (Processing)",
            RefundStatus.Succeeded => "Đã hoàn tiền thành công (Succeeded)",
            RefundStatus.Failed => "Xử lý hoàn tiền thất bại (Failed)",
            _ => "Không áp dụng"
        };

        var dto = new RefundDetailDto(
            Id: refund.Id,
            AppointmentId: refund.AppointmentId,
            BookingCode: refund.Appointment?.BookingCode ?? string.Empty,
            PatientFullName: refund.Appointment?.Patient?.FullName ?? "Bệnh nhân",
            DoctorFullName: refund.Appointment?.Doctor?.FullName ?? "Bác sĩ phụ trách",
            ServiceName: refund.Appointment?.ServiceNameSnapshot ?? string.Empty,
            AppointmentStartAtUtc: refund.Appointment?.StartAt ?? DateTime.MinValue,
            PaidServiceAmount: refund.PaidServiceAmount,
            CancellationFeeRate: refund.CancellationFeeRate,
            CancellationFeeAmount: refund.CancellationFeeAmount,
            ExpectedRefundAmount: refund.ExpectedRefundAmount,
            PolicyVersion: refund.PolicyVersion,
            Status: refund.Status,
            StatusDescription: statusDesc,
            IsCompleted: refund.Status == RefundStatus.Succeeded,
            Reason: refund.Reason,
            ProviderReference: refund.ProviderReference,
            ProcessedByUserId: refund.ProcessedByUserId,
            FailureReason: refund.FailureReason,
            ProcessedAtUtc: refund.ProcessedAt,
            CreatedAtUtc: refund.CreatedAt
        );

        return Result<RefundDetailDto>.Success(dto, 200);
    }
}
