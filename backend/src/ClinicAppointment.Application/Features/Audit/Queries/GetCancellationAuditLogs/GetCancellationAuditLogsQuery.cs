using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Audit.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Audit.Queries.GetCancellationAuditLogs;

public record GetCancellationAuditLogsQuery(
    string? BookingCode = null,
    string? ActorType = null,
    DateTime? FromDateUtc = null,
    DateTime? ToDateUtc = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<CancellationAuditDetailDto>>>;

public class GetCancellationAuditLogsQueryHandler : IRequestHandler<GetCancellationAuditLogsQuery, Result<PaginatedList<CancellationAuditDetailDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCancellationAuditLogsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaginatedList<CancellationAuditDetailDto>>> Handle(
        GetCancellationAuditLogsQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ Staff (Receptionist / Admin) mới được truy cập danh sách audit logs
        if (!_currentUserService.IsStaff)
        {
            return Result<PaginatedList<CancellationAuditDetailDto>>.Failure(
                "Bạn không có quyền truy cập nhật ký kiểm toán. Yêu cầu quyền Receptionist hoặc Admin.", 
                403);
        }

        var query = _context.AuditLogs
            .Include(a => a.ActorUser)
            .Where(a => a.Action == "CANCEL_APPOINTMENT" || a.Action == "PROCESS_REFUND")
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.ActorType))
        {
            var normalizedActor = request.ActorType.Trim().ToUpperInvariant();
            query = query.Where(a => a.ActorType == normalizedActor);
        }

        if (!string.IsNullOrWhiteSpace(request.BookingCode))
        {
            var normalizedCode = request.BookingCode.Trim().ToUpperInvariant();
            query = query.Where(a => a.MetadataRedacted != null && a.MetadataRedacted.ToUpper().Contains(normalizedCode));
        }

        if (request.FromDateUtc.HasValue)
        {
            query = query.Where(a => a.OccurredAt >= request.FromDateUtc.Value);
        }

        if (request.ToDateUtc.HasValue)
        {
            query = query.Where(a => a.OccurredAt <= request.ToDateUtc.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page <= 0 ? 1 : request.Page;
        int pageSize = request.PageSize <= 0 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var logs = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = logs.Select(log => ParseAuditLog(log)).ToList();

        var paginated = new PaginatedList<CancellationAuditDetailDto>(dtos, totalCount, page, pageSize);
        return Result<PaginatedList<CancellationAuditDetailDto>>.Success(paginated, 200);
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
