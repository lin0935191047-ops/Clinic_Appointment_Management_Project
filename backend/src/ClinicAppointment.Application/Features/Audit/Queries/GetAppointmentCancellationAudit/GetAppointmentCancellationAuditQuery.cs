using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Audit.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Audit.Queries.GetAppointmentCancellationAudit;

public record GetAppointmentCancellationAuditQuery(string BookingCode) : IRequest<Result<AppointmentCancellationAuditHistoryDto>>;

public class GetAppointmentCancellationAuditQueryHandler : IRequestHandler<GetAppointmentCancellationAuditQuery, Result<AppointmentCancellationAuditHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAppointmentCancellationAuditQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AppointmentCancellationAuditHistoryDto>> Handle(
        GetAppointmentCancellationAuditQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ Staff (Receptionist / Admin) mới được xem audit trail
        if (!_currentUserService.IsStaff)
        {
            return Result<AppointmentCancellationAuditHistoryDto>.Failure(
                "Bạn không có quyền truy cập lịch sử kiểm toán. Yêu cầu quyền Receptionist hoặc Admin.", 
                403);
        }

        if (string.IsNullOrWhiteSpace(request.BookingCode))
        {
            return Result<AppointmentCancellationAuditHistoryDto>.Failure(
                "Mã đặt lịch không được để trống.", 
                400);
        }

        var normalizedCode = request.BookingCode.Trim().ToUpperInvariant();

        // 2. Tra cứu ca khám
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.BookingCode == normalizedCode, cancellationToken);

        if (appointment == null)
        {
            return Result<AppointmentCancellationAuditHistoryDto>.Failure(
                "Không tìm thấy ca khám với mã đặt lịch đã cung cấp.", 
                404);
        }

        // 3. Lấy danh sách hoàn tiền của ca khám (để lấy EntityId của Refund)
        var refundIds = await _context.Refunds
            .Where(r => r.AppointmentId == appointment.Id)
            .Select(r => (Guid?)r.Id)
            .ToListAsync(cancellationToken);

        // 4. Truy vấn các bản ghi AuditLog liên quan: CANCEL_APPOINTMENT và PROCESS_REFUND
        var logs = await _context.AuditLogs
            .Include(a => a.ActorUser)
            .Where(a => 
                (a.EntityType == "Appointment" && a.EntityId == appointment.Id) ||
                (a.EntityType == "Refund" && refundIds.Contains(a.EntityId)) ||
                (a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id))
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);

        // 5. Chuyển đổi sang DTO an toàn
        var auditTrail = logs.Select(log => ParseAuditLog(log)).ToList();

        var historyDto = new AppointmentCancellationAuditHistoryDto(
            BookingCode: appointment.BookingCode,
            AppointmentId: appointment.Id,
            Status: appointment.Status,
            CancellationAcceptedAtUtc: appointment.CancellationAcceptedAt,
            CancellationReason: appointment.CancellationReason,
            ServiceName: appointment.ServiceNameSnapshot,
            PaidAmount: appointment.PaidAmount,
            AuditTrail: auditTrail
        );

        return Result<AppointmentCancellationAuditHistoryDto>.Success(historyDto, 200);
    }

    private static CancellationAuditDetailDto ParseAuditLog(AuditLog log)
    {
        string? bookingCode = null;
        string? serviceName = null;
        string? cancellationReason = null;
        decimal? paidAmount = null;
        decimal? feeRate = null;
        decimal? refundRate = null;
        decimal? feeAmount = null;
        decimal? expectedRefund = null;
        string? policyVersion = null;
        string? refundDecision = null;
        string? refundStatus = null;
        string? providerReference = null;
        string? failureReason = null;
        string? note = null;

        if (!string.IsNullOrWhiteSpace(log.MetadataRedacted))
        {
            try
            {
                using var doc = JsonDocument.Parse(log.MetadataRedacted);
                var root = doc.RootElement;

                if (root.TryGetProperty("BookingCode", out var prop)) bookingCode = prop.GetString();
                if (root.TryGetProperty("ServiceNameSnapshot", out prop)) serviceName = prop.GetString();
                if (root.TryGetProperty("Reason", out prop)) cancellationReason = prop.GetString();
                if (root.TryGetProperty("PaidAmount", out prop)) paidAmount = prop.GetDecimal();
                if (root.TryGetProperty("CancellationFeeRate", out prop)) feeRate = prop.GetDecimal();
                if (root.TryGetProperty("RefundRate", out prop)) refundRate = prop.GetDecimal();
                if (root.TryGetProperty("CancellationFeeAmount", out prop)) feeAmount = prop.GetDecimal();
                if (root.TryGetProperty("ExpectedRefundAmount", out prop)) expectedRefund = prop.GetDecimal();
                if (root.TryGetProperty("PolicyVersion", out prop)) policyVersion = prop.GetString();
                if (root.TryGetProperty("RefundDecision", out prop)) refundDecision = prop.GetString();
                if (root.TryGetProperty("RefundStatus", out prop)) refundStatus = prop.GetString();
                if (root.TryGetProperty("ProviderReference", out prop)) providerReference = prop.GetString();
                if (root.TryGetProperty("FailureReason", out prop)) failureReason = prop.GetString();
                if (root.TryGetProperty("Note", out prop)) note = prop.GetString();
            }
            catch
            {
                // Json parsing fallback nếu metadata không chuẩn
            }
        }

        return new CancellationAuditDetailDto(
            LogId: log.Id,
            Action: log.Action,
            ActorType: log.ActorType,
            ActorUserId: log.ActorUserId,
            ActorDisplayName: log.ActorUser?.Email,
            EntityType: log.EntityType,
            EntityId: log.EntityId,
            OccurredAtUtc: log.OccurredAt,
            BookingCode: bookingCode,
            ServiceName: serviceName,
            CancellationReason: cancellationReason,
            PaidAmount: paidAmount,
            CancellationFeeRate: feeRate,
            RefundRate: refundRate,
            CancellationFeeAmount: feeAmount,
            ExpectedRefundAmount: expectedRefund,
            PolicyVersion: policyVersion,
            RefundDecision: refundDecision,
            RefundStatus: refundStatus,
            ProviderReference: providerReference,
            FailureReason: failureReason,
            Note: note,
            MetadataRedactedJson: log.MetadataRedacted ?? "{}"
        );
    }
}
