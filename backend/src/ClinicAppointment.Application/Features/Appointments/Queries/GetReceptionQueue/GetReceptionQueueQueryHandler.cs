using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Queries.GetReceptionQueue;

public class GetReceptionQueueQueryHandler : IRequestHandler<GetReceptionQueueQuery, Result<PaginatedList<ReceptionQueueItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentifierMaskingService _maskingService;

    public GetReceptionQueueQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _maskingService = maskingService;
    }

    public async Task<Result<PaginatedList<ReceptionQueueItemDto>>> Handle(
        GetReceptionQueueQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền bảo vệ: Chỉ nhân viên (Receptionist hoặc Admin) mới có quyền truy cập hàng đợi tiếp nhận
        if (!_currentUserService.IsStaff)
        {
            return Result<PaginatedList<ReceptionQueueItemDto>>.Failure(
                "Bạn không có quyền truy cập hàng đợi tiếp nhận tại quầy lễ tân. Chức năng này chỉ dành cho nhân viên.", 
                403);
        }

        // 2. Xây dựng truy vấn cơ sở
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Service)
            .AsQueryable();

        // 3. Lọc theo Bác sĩ (DoctorId)
        if (request.DoctorId.HasValue && request.DoctorId.Value != Guid.Empty)
        {
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);
        }

        // 4. Lọc theo Ngày khám (Date)
        // Múi giờ chuẩn phòng khám là GMT+7 (Asia/Ho_Chi_Minh). 
        // Quy đổi ngày từ 00:00:00 đến 23:59:59.999 GMT+7 sang khoảng thời gian tương ứng theo UTC (-7 hours).
        if (request.Date.HasValue)
        {
            var date = request.Date.Value;
            var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
            var endUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddHours(-7);
            query = query.Where(a => a.StartAt >= startUtc && a.StartAt <= endUtc);
        }

        // 5. Lọc theo Trạng thái (Status)
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        // 6. Lọc theo Từ khóa tìm kiếm (Keyword: Mã booking, Họ tên bệnh nhân, Số điện thoại, Tên bác sĩ)
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(a =>
                a.BookingCode.ToLower().Contains(keyword) ||
                (a.Patient != null && a.Patient.FullName.ToLower().Contains(keyword)) ||
                (a.Patient != null && a.Patient.PhoneNumber.Contains(keyword)) ||
                (a.Doctor != null && a.Doctor.FullName.ToLower().Contains(keyword))
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
        var queueItems = rawItems.Select((apt, index) =>
        {
            var queueNumber = (pageNumber - 1) * pageSize + index + 1;
            var maskedPhone = _maskingService.MaskPhone(apt.Patient?.PhoneNumber ?? string.Empty);
            
            // Thời điểm check-in: nếu đã CheckedIn/InProgress/Completed thì lấy UpdatedAt của ca khám
            DateTime? checkedInAt = (apt.Status == AppointmentStatus.CheckedIn ||
                                     apt.Status == AppointmentStatus.InProgress ||
                                     apt.Status == AppointmentStatus.Completed)
                ? apt.UpdatedAt
                : null;

            return new ReceptionQueueItemDto(
                AppointmentId: apt.Id,
                BookingCode: apt.BookingCode,
                QueueNumber: queueNumber,
                DoctorId: apt.DoctorId,
                DoctorName: apt.Doctor?.FullName ?? "Bác sĩ",
                DoctorTitle: apt.Doctor?.Title,
                RoomNumber: apt.Doctor?.RoomNumber,
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

        var paginatedList = new PaginatedList<ReceptionQueueItemDto>(queueItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedList<ReceptionQueueItemDto>>.Success(paginatedList);
    }
}
