using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Queries.GetDoctorAppointments;

public class GetDoctorAppointmentsQueryHandler : IRequestHandler<GetDoctorAppointmentsQuery, Result<PaginatedList<DoctorAssignedAppointmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentifierMaskingService _maskingService;

    public GetDoctorAppointmentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _maskingService = maskingService;
    }

    public async Task<Result<PaginatedList<DoctorAssignedAppointmentDto>>> Handle(
        GetDoctorAppointmentsQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền truy cập: Chỉ Bác sĩ (Doctor) hoặc Quản trị viên (Admin) mới có quyền truy cập
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);
        var isAdmin = _currentUserService.IsAdmin;

        if (!isDoctor && !isAdmin)
        {
            return Result<PaginatedList<DoctorAssignedAppointmentDto>>.Failure(
                "Bạn không có quyền truy cập danh sách ca khám của Bác sĩ. Chức năng này chỉ dành cho Bác sĩ hoặc Quản trị viên.", 
                403);
        }

        // 2. Xác định DoctorId được phép xem (Bảo vệ tuyệt đối Assigned Doctor Only & Chống IDOR)
        Guid? targetDoctorId = null;

        if (isDoctor)
        {
            // Bác sĩ đăng nhập: BẮT BUỘC chỉ được xem ca khám của chính mình dựa vào UserId liên kết
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null || currentUserId == Guid.Empty)
            {
                return Result<PaginatedList<DoctorAssignedAppointmentDto>>.Failure(
                    "Không thể xác định danh tính tài khoản của Bác sĩ.", 
                    401);
            }

            var doctorProfile = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == currentUserId.Value, cancellationToken);

            if (doctorProfile == null)
            {
                return Result<PaginatedList<DoctorAssignedAppointmentDto>>.Failure(
                    "Không tìm thấy hồ sơ Bác sĩ tương ứng với tài khoản đăng nhập của bạn.", 
                    403);
            }

            // Bất kể request có truyền DoctorId nào, luôn ép buộc dùng Id của chính bác sĩ này
            targetDoctorId = doctorProfile.Id;
        }
        else if (isAdmin)
        {
            // Quản trị viên: Có thể xem ca khám của bác sĩ được chỉ định trong request
            if (request.DoctorId.HasValue && request.DoctorId.Value != Guid.Empty)
            {
                targetDoctorId = request.DoctorId.Value;
            }
        }

        // 3. Xây dựng truy vấn cơ sở
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Service)
            .AsQueryable();

        // Ép điều kiện bác sĩ được phân công
        if (targetDoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == targetDoctorId.Value);
        }

        // 4. Lọc theo Ngày khám (Date filter)
        // Múi giờ chuẩn phòng khám là GMT+7 (Asia/Ho_Chi_Minh). 
        // Quy đổi ngày từ 00:00:00 đến 23:59:59.999 GMT+7 sang khoảng thời gian tương ứng theo UTC (-7 hours).
        if (request.Date.HasValue)
        {
            var date = request.Date.Value;
            var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
            var endUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddHours(-7);
            query = query.Where(a => a.StartAt >= startUtc && a.StartAt <= endUtc);
        }

        // 5. Lọc theo Trạng thái (Status filter)
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        // 6. Lọc theo từ khóa tìm kiếm (Keyword: Mã booking, Họ tên bệnh nhân, Số điện thoại)
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(a =>
                a.BookingCode.ToLower().Contains(keyword) ||
                (a.Patient != null && a.Patient.FullName.ToLower().Contains(keyword)) ||
                (a.Patient != null && a.Patient.PhoneNumber.Contains(keyword))
            );
        }

        // 7. Sắp xếp (SortBy)
        var sortBy = request.SortBy?.ToLower().Trim();
        query = sortBy switch
        {
            "time_desc" => query.OrderByDescending(a => a.StartAt),
            "status" => query
                .OrderBy(a => a.Status == AppointmentStatus.CheckedIn ? 1 :
                              a.Status == AppointmentStatus.InProgress ? 2 :
                              a.Status == AppointmentStatus.Confirmed ? 3 :
                              a.Status == AppointmentStatus.PendingConfirmation ? 4 :
                              a.Status == AppointmentStatus.Completed ? 5 : 6)
                .ThenBy(a => a.StartAt),
            _ => query.OrderBy(a => a.StartAt) // Mặc định: theo thời gian khám tăng dần
        };

        // 8. Đếm tổng số bản ghi phục vụ phân trang
        var totalCount = await query.CountAsync(cancellationToken);

        // 9. Phân trang
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var rawItems = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // 10. Chuyển đổi sang DTO và gán số thứ tự tiếp nhận an toàn
        var assignedItems = rawItems.Select((apt, index) =>
        {
            var queueNumber = (pageNumber - 1) * pageSize + index + 1;
            var maskedPhone = _maskingService.MaskPhone(apt.Patient?.PhoneNumber ?? string.Empty);
            
            // Thời điểm check-in tiếp nhận
            DateTime? checkedInAt = (apt.Status == AppointmentStatus.CheckedIn ||
                                     apt.Status == AppointmentStatus.InProgress ||
                                     apt.Status == AppointmentStatus.Completed)
                ? apt.UpdatedAt
                : null;

            return new DoctorAssignedAppointmentDto(
                AppointmentId: apt.Id,
                BookingCode: apt.BookingCode,
                QueueNumber: queueNumber,
                DoctorId: apt.DoctorId,
                DoctorName: apt.Doctor?.FullName ?? "Bác sĩ",
                PatientId: apt.PatientId,
                PatientName: apt.Patient?.FullName ?? "Bệnh nhân",
                PatientCode: apt.Patient?.PatientCode ?? string.Empty,
                DateOfBirth: apt.Patient?.DateOfBirth,
                BirthYear: apt.Patient?.BirthYear,
                Gender: apt.Patient?.Gender,
                MaskedPhoneNumber: maskedPhone,
                ServiceId: apt.ServiceId,
                ServiceName: apt.ServiceNameSnapshot,
                ServiceDurationMinutes: apt.ServiceDurationSnapshot,
                ServicePrice: apt.ServicePriceSnapshot,
                StartAt: apt.StartAt,
                EndAt: apt.EndAt,
                Status: apt.Status,
                ReasonForVisit: apt.ReasonForVisit,
                CheckedInAt: checkedInAt,
                IsGuest: apt.IsGuest
            );
        }).ToList();

        var paginatedList = new PaginatedList<DoctorAssignedAppointmentDto>(assignedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedList<DoctorAssignedAppointmentDto>>.Success(paginatedList);
    }
}
