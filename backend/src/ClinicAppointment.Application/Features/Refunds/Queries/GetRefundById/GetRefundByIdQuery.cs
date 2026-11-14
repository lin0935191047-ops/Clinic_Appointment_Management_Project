using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Refunds.DTOs;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Refunds.Queries.GetRefundById;

public record GetRefundByIdQuery(Guid Id) : IRequest<Result<RefundDetailDto>>;

public class GetRefundByIdQueryHandler : IRequestHandler<GetRefundByIdQuery, Result<RefundDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRefundByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RefundDetailDto>> Handle(
        GetRefundByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var refund = await _context.Refunds
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (refund == null)
        {
            return Result<RefundDetailDto>.Failure("Không tìm thấy thông tin bản ghi hoàn tiền.", 404);
        }

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
