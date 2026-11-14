using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Application.Features.Notifications.Services;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Notifications.Queries;

public record GetAppointmentNotificationsQuery(Guid AppointmentId) : IRequest<Result<IReadOnlyList<NotificationDto>>>;

public class GetAppointmentNotificationsQueryHandler : IRequestHandler<GetAppointmentNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAppointmentNotificationService _appointmentNotificationService;
    private readonly ICurrentUserService _currentUserService;

    public GetAppointmentNotificationsQueryHandler(
        IApplicationDbContext context,
        IAppointmentNotificationService appointmentNotificationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _appointmentNotificationService = appointmentNotificationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        GetAppointmentNotificationsQuery request, 
        CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            return Result<IReadOnlyList<NotificationDto>>.Failure("Không tìm thấy thông tin ca khám.", 404);
        }

        // Phân quyền truy cập: Staff, Doctor phụ trách, hoặc Admin
        var isStaff = _currentUserService.IsStaff;
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);

        if (isDoctor && !isStaff)
        {
            var doctorProfile = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken);

            if (doctorProfile == null || appointment.DoctorId != doctorProfile.Id)
            {
                return Result<IReadOnlyList<NotificationDto>>.Failure("Bạn không có quyền truy cập trạng thái thông báo của ca khám này.", 403);
            }
        }
        else if (!isStaff && !_currentUserService.IsAuthenticated)
        {
            return Result<IReadOnlyList<NotificationDto>>.Failure("Yêu cầu xác thực để truy cập trạng thái thông báo.", 401);
        }

        var list = await _appointmentNotificationService.GetNotificationsByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        return Result<IReadOnlyList<NotificationDto>>.Success(list);
    }
}
