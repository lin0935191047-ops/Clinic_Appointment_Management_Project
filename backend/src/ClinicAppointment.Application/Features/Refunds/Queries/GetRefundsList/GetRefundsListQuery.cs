using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Refunds.DTOs;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Refunds.Queries.GetRefundsList;

public record GetRefundsListQuery(
    RefundStatus? Status = null,
    string? BookingCode = null,
    DateTime? FromDateUtc = null,
    DateTime? ToDateUtc = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<RefundDetailDto>>>;

public class GetRefundsListQueryHandler : IRequestHandler<GetRefundsListQuery, Result<PaginatedList<RefundDetailDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetRefundsListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<RefundDetailDto>>> Handle(
        GetRefundsListQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Refunds
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Doctor)
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.BookingCode))
        {
            var normalizedCode = request.BookingCode.Trim().ToUpperInvariant();
            query = query.Where(r => r.Appointment != null && r.Appointment.BookingCode.ToUpper().Contains(normalizedCode));
        }

        if (request.FromDateUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= request.FromDateUtc.Value);
        }

        if (request.ToDateUtc.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= request.ToDateUtc.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page <= 0 ? 1 : request.Page;
        int pageSize = request.PageSize <= 0 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var refunds = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = refunds.Select(r =>
        {
            string statusDesc = r.Status switch
            {
                RefundStatus.Pending => "Chờ xử lý hoàn tiền (Pending)",
                RefundStatus.Processing => "Đang đối soát ngân hàng / xử lý chi tiền (Processing)",
                RefundStatus.Succeeded => "Đã hoàn tiền thành công (Succeeded)",
                RefundStatus.Failed => "Xử lý hoàn tiền thất bại (Failed)",
                _ => "Không áp dụng"
            };

            return new RefundDetailDto(
                Id: r.Id,
                AppointmentId: r.AppointmentId,
                BookingCode: r.Appointment?.BookingCode ?? string.Empty,
                PatientFullName: r.Appointment?.Patient?.FullName ?? "Bệnh nhân",
                DoctorFullName: r.Appointment?.Doctor?.FullName ?? "Bác sĩ phụ trách",
                ServiceName: r.Appointment?.ServiceNameSnapshot ?? string.Empty,
                AppointmentStartAtUtc: r.Appointment?.StartAt ?? DateTime.MinValue,
                PaidServiceAmount: r.PaidServiceAmount,
                CancellationFeeRate: r.CancellationFeeRate,
                CancellationFeeAmount: r.CancellationFeeAmount,
                ExpectedRefundAmount: r.ExpectedRefundAmount,
                PolicyVersion: r.PolicyVersion,
                Status: r.Status,
                StatusDescription: statusDesc,
                IsCompleted: r.Status == RefundStatus.Succeeded,
                Reason: r.Reason,
                ProviderReference: r.ProviderReference,
                ProcessedByUserId: r.ProcessedByUserId,
                FailureReason: r.FailureReason,
                ProcessedAtUtc: r.ProcessedAt,
                CreatedAtUtc: r.CreatedAt
            );
        }).ToList();

        var paginated = new PaginatedList<RefundDetailDto>(dtos, totalCount, page, pageSize);
        return Result<PaginatedList<RefundDetailDto>>.Success(paginated, 200);
    }
}
