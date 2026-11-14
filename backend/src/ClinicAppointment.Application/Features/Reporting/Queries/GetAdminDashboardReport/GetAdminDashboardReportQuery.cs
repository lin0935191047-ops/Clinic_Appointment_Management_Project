using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Reporting.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Reporting.Queries.GetAdminDashboardReport;

public record GetAdminDashboardReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    Guid? DoctorId = null,
    Guid? SpecialtyId = null,
    AppointmentStatus? Status = null
) : IRequest<Result<AdminDashboardReportDto>>;

public class GetAdminDashboardReportQueryHandler : IRequestHandler<GetAdminDashboardReportQuery, Result<AdminDashboardReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAdminDashboardReportQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AdminDashboardReportDto>> Handle(
        GetAdminDashboardReportQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Báo cáo quản trị chỉ dành cho Quản trị viên (ADMIN)
        if (!_currentUserService.IsAdmin)
        {
            return Result<AdminDashboardReportDto>.Failure(
                "Bạn không có quyền truy cập báo cáo quản trị. Chức năng chỉ dành cho Quản trị viên (Admin).", 
                403);
        }

        // 2. Chuẩn hóa khoảng thời gian tìm kiếm theo giờ Việt Nam (UTC+7)
        DateTime? startUtc = null;
        DateTime? endUtc = null;

        if (request.FromDate.HasValue)
        {
            startUtc = request.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        }

        if (request.ToDate.HasValue)
        {
            endUtc = request.ToDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddHours(-7);
        }

        // 3. Khởi tạo truy vấn lọc đa chiều (Date/status/doctor/specialty filters)
        var query = _context.Appointments
            .Include(a => a.Doctor)
                .ThenInclude(d => d!.Specialty)
            .AsNoTracking();

        if (startUtc.HasValue)
        {
            query = query.Where(a => a.StartAt >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(a => a.StartAt <= endUtc.Value);
        }

        if (request.DoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);
        }

        if (request.SpecialtyId.HasValue)
        {
            query = query.Where(a => a.Doctor != null && a.Doctor.SpecialtyId == request.SpecialtyId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        var appointments = await query.ToListAsync(cancellationToken);

        // 4. Phân rã trạng thái & Đối soát số lượng (Numbers reconcile)
        int pending = appointments.Count(a => a.Status == AppointmentStatus.PendingConfirmation);
        int confirmed = appointments.Count(a => a.Status == AppointmentStatus.Confirmed);
        int checkedIn = appointments.Count(a => a.Status == AppointmentStatus.CheckedIn);
        int inProgress = appointments.Count(a => a.Status == AppointmentStatus.InProgress);
        int completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        int cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        int noShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow);
        int total = appointments.Count;

        // Đối soát bảo toàn: total phải bằng tổng các trạng thái
        double completionRate = total > 0 ? Math.Round((double)completed / total * 100, 2) : 0;
        double cancellationRate = total > 0 ? Math.Round((double)cancelled / total * 100, 2) : 0;

        var statusBreakdown = new AppointmentStatusBreakdownDto(
            TotalAppointments: total,
            PendingConfirmationCount: pending,
            ConfirmedCount: confirmed,
            CheckedInCount: checkedIn,
            InProgressCount: inProgress,
            CompletedCount: completed,
            CancelledCount: cancelled,
            NoShowCount: noShow,
            CompletionRate: completionRate,
            CancellationRate: cancellationRate
        );

        // 5. Thống kê tài chính & Đối soát doanh thu
        var appointmentIds = appointments.Select(a => a.Id).ToList();

        var refunds = await _context.Refunds
            .Where(r => appointmentIds.Contains(r.AppointmentId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        decimal totalGrossRevenue = appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .Sum(a => a.ServicePriceSnapshot);

        decimal totalPaidAmount = appointments.Sum(a => a.PaidAmount);

        decimal totalExpectedRefund = refunds.Sum(r => r.ExpectedRefundAmount);

        decimal totalActualRefunded = refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.ExpectedRefundAmount);

        decimal totalCancellationFee = refunds.Sum(r => r.CancellationFeeAmount);

        // Doanh thu thuần = Doanh thu từ ca hoàn tất + Phí hủy lịch thu được
        decimal totalNetRevenue = totalGrossRevenue + totalCancellationFee;

        var financialSummary = new FinancialSummaryDto(
            TotalGrossRevenue: totalGrossRevenue,
            TotalPaidAmount: totalPaidAmount,
            TotalExpectedRefundAmount: totalExpectedRefund,
            TotalActualRefundedAmount: totalActualRefunded,
            TotalCancellationFeeCollected: totalCancellationFee,
            TotalNetRevenue: totalNetRevenue
        );

        // 6. Thống kê theo chuyên khoa (Specialty Reports)
        var specialtyReports = appointments
            .Where(a => a.Doctor?.Specialty != null)
            .GroupBy(a => new { a.Doctor!.SpecialtyId, a.Doctor.Specialty!.Name })
            .Select(g =>
            {
                int total = g.Count();
                int completedApts = g.Count(a => a.Status == AppointmentStatus.Completed);
                int cancelledApts = g.Count(a => a.Status == AppointmentStatus.Cancelled);
                double rate = total > 0 ? Math.Round((double)completedApts / total * 100, 2) : 0.0;
                decimal revenue = g.Where(a => a.Status == AppointmentStatus.Completed).Sum(a => a.ServicePriceSnapshot);

                return new SpecialtyReportDto(
                    SpecialtyId: g.Key.SpecialtyId,
                    SpecialtyName: g.Key.Name,
                    TotalAppointments: total,
                    CompletedCount: completedApts,
                    CancelledCount: cancelledApts,
                    CompletionRate: rate,
                    GrossRevenue: revenue
                );
            })
            .OrderByDescending(s => s.TotalAppointments)
            .ToList();

        // 7. Thống kê theo bác sĩ (Doctor Reports)
        var doctorReports = appointments
            .Where(a => a.Doctor != null)
            .GroupBy(a => new { a.DoctorId, a.Doctor!.FullName, SpecialtyName = a.Doctor.Specialty?.Name ?? "Chung" })
            .Select(g =>
            {
                int total = g.Count();
                int completedApts = g.Count(a => a.Status == AppointmentStatus.Completed);
                int cancelledApts = g.Count(a => a.Status == AppointmentStatus.Cancelled);
                double rate = total > 0 ? Math.Round((double)completedApts / total * 100, 2) : 0.0;
                decimal revenue = g.Where(a => a.Status == AppointmentStatus.Completed).Sum(a => a.ServicePriceSnapshot);

                return new DoctorReportDto(
                    DoctorId: g.Key.DoctorId,
                    DoctorName: g.Key.FullName,
                    SpecialtyName: g.Key.SpecialtyName,
                    TotalAppointments: total,
                    CompletedCount: completedApts,
                    CancelledCount: cancelledApts,
                    CompletionRate: rate,
                    GrossRevenue: revenue
                );
            })
            .OrderByDescending(d => d.TotalAppointments)
            .ToList();

        var report = new AdminDashboardReportDto(
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            DoctorId: request.DoctorId,
            SpecialtyId: request.SpecialtyId,
            StatusBreakdown: statusBreakdown,
            FinancialSummary: financialSummary,
            SpecialtyReports: specialtyReports,
            DoctorReports: doctorReports,
            GeneratedAtUtc: DateTime.UtcNow
        );

        return Result<AdminDashboardReportDto>.Success(report);
    }
}
