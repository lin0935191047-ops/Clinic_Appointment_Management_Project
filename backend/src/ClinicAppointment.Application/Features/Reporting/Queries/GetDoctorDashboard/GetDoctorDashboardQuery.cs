using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Reporting.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Reporting.Queries.GetDoctorDashboard;

/// <summary>
/// Query lấy dữ liệu Dashboard và lịch khám trong ngày của Bác sĩ (Doctor Dashboard)
/// </summary>
public record GetDoctorDashboardQuery(
    DateOnly? Date = null,
    Guid? DoctorId = null
) : IRequest<Result<DoctorDashboardDto>>;

public class GetDoctorDashboardQueryHandler : IRequestHandler<GetDoctorDashboardQuery, Result<DoctorDashboardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentifierMaskingService _maskingService;

    public GetDoctorDashboardQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _maskingService = maskingService;
    }

    public async Task<Result<DoctorDashboardDto>> Handle(
        GetDoctorDashboardQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền truy cập & Bảo vệ phạm vi phân công (Assigned scope enforced)
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);
        var isAdmin = _currentUserService.IsAdmin;

        if (!isDoctor && !isAdmin)
        {
            return Result<DoctorDashboardDto>.Failure(
                "Bạn không có quyền truy cập Dashboard của Bác sĩ. Chức năng này chỉ dành cho Bác sĩ hoặc Quản trị viên.",
                403);
        }

        Doctor? doctor = null;

        if (isDoctor)
        {
            // Bác sĩ đăng nhập: BẮT BUỘC chỉ được xem Dashboard của chính mình, chống IDOR tuyệt đối
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null || currentUserId == Guid.Empty)
            {
                return Result<DoctorDashboardDto>.Failure(
                    "Không thể xác định danh tính tài khoản của Bác sĩ.",
                    401);
            }

            doctor = await _context.Doctors
                .Include(d => d.Specialty)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == currentUserId.Value, cancellationToken);

            if (doctor == null)
            {
                return Result<DoctorDashboardDto>.Failure(
                    "Không tìm thấy hồ sơ Bác sĩ tương ứng với tài khoản đăng nhập của bạn.",
                    403);
            }
        }
        else if (isAdmin)
        {
            // Quản trị viên: Có thể xem dashboard của bác sĩ theo request.DoctorId hoặc lấy bác sĩ đầu tiên
            if (request.DoctorId.HasValue)
            {
                doctor = await _context.Doctors
                    .Include(d => d.Specialty)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == request.DoctorId.Value, cancellationToken);

                if (doctor == null)
                {
                    return Result<DoctorDashboardDto>.Failure(
                        $"Không tìm thấy Bác sĩ với mã '{request.DoctorId.Value}'.",
                        404);
                }
            }
            else
            {
                doctor = await _context.Doctors
                    .Include(d => d.Specialty)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

                if (doctor == null)
                {
                    return Result<DoctorDashboardDto>.Failure(
                        "Hệ thống chưa có dữ liệu Bác sĩ nào để hiển thị Dashboard.",
                        404);
                }
            }
        }

        if (doctor == null)
        {
            return Result<DoctorDashboardDto>.Failure("Không thể xác định thông tin Bác sĩ.", 400);
        }

        // 2. Xác định ngày khám (Mặc định là ngày hôm nay theo múi giờ Việt Nam UTC+7)
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        // Quy đổi ngày sang khoảng thời gian UTC tương ứng trên server
        var startUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        var endUtc = targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddHours(-7);

        // 3. Truy vấn các ca khám của bác sĩ trong ngày
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctor.Id && a.StartAt >= startUtc && a.StartAt <= endUtc)
            .OrderBy(a => a.StartAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var appointmentIds = appointments.Select(a => a.Id).ToList();

        // Kiểm tra các ca đã có ghi chép bệnh án lâm sàng
        var consultedAppointmentIds = await _context.Consultations
            .AsNoTracking()
            .Where(c => appointmentIds.Contains(c.AppointmentId))
            .Select(c => c.AppointmentId)
            .ToListAsync(cancellationToken);

        var consultedSet = consultedAppointmentIds.ToHashSet();

        // 4. Thống kê chỉ số (Today Schedule Metrics)
        int total = appointments.Count;
        int pending = appointments.Count(a => a.Status == AppointmentStatus.PendingConfirmation);
        int confirmed = appointments.Count(a => a.Status == AppointmentStatus.Confirmed);
        int checkedIn = appointments.Count(a => a.Status == AppointmentStatus.CheckedIn);
        int inProgress = appointments.Count(a => a.Status == AppointmentStatus.InProgress);
        int completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        int cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        int noShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow);

        double completionRate = total > 0 ? Math.Round((double)completed / total * 100, 2) : 0.0;

        var metrics = new DoctorDashboardMetricsDto(
            TotalAppointments: total,
            PendingConfirmationCount: pending,
            ConfirmedCount: confirmed,
            CheckedInCount: checkedIn,
            InProgressCount: inProgress,
            CompletedCount: completed,
            CancelledCount: cancelled,
            NoShowCount: noShow,
            CompletionRate: completionRate
        );

        // 5. Chuyển đổi danh sách ca khám hôm nay (Tối thiểu hóa thông tin, mask số điện thoại)
        var appointmentItems = appointments.Select((a, idx) =>
        {
            var vnStart = a.StartAt.AddHours(7);
            var vnEnd = a.EndAt.AddHours(7);
            var rawPhone = a.Patient?.PhoneNumber ?? string.Empty;
            var maskedPhone = _maskingService.MaskPhone(rawPhone);

            return new DoctorDashboardAppointmentItemDto(
                AppointmentId: a.Id,
                BookingCode: a.BookingCode,
                QueueNumber: idx + 1,
                PatientId: a.PatientId,
                PatientName: a.Patient?.FullName ?? "Bệnh nhân",
                MaskedPhoneNumber: maskedPhone,
                ServiceName: a.ServiceNameSnapshot,
                DurationMinutes: a.ServiceDurationSnapshot,
                StartAtUtc: a.StartAt,
                EndAtUtc: a.EndAt,
                LocalStartTime: vnStart.ToString("HH:mm"),
                LocalEndTime: vnEnd.ToString("HH:mm"),
                Status: a.Status,
                ReasonForVisit: a.ReasonForVisit,
                HasConsultation: consultedSet.Contains(a.Id),
                CheckedInAtUtc: a.Status == AppointmentStatus.CheckedIn || a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.Completed
                    ? a.UpdatedAt
                    : null
            );
        }).ToList();

        // 6. Hàng đợi bệnh nhân đang chờ (CheckedIn) và Ca đang khám dở (InProgress)
        var waitingQueue = appointmentItems
            .Where(a => a.Status == AppointmentStatus.CheckedIn)
            .OrderBy(a => a.StartAtUtc)
            .ToList();

        var currentActive = appointmentItems
            .FirstOrDefault(a => a.Status == AppointmentStatus.InProgress);

        var dashboard = new DoctorDashboardDto(
            DoctorId: doctor.Id,
            DoctorName: doctor.FullName,
            SpecialtyName: doctor.Specialty?.Name ?? "Chung",
            RoomNumber: doctor.RoomNumber,
            Date: targetDate,
            Metrics: metrics,
            TodayAppointments: appointmentItems,
            WaitingQueue: waitingQueue,
            CurrentActiveAppointment: currentActive,
            GeneratedAtUtc: DateTime.UtcNow
        );

        return Result<DoctorDashboardDto>.Success(dashboard);
    }
}
