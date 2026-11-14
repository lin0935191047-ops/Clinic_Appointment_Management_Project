using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Features.Notifications.Services;

public class AppointmentNotificationService : IAppointmentNotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AppointmentNotificationService> _logger;

    public AppointmentNotificationService(
        IApplicationDbContext context,
        INotificationService notificationService,
        ILogger<AppointmentNotificationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<NotificationDto?> NotifyBookingCreatedAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
        {
            _logger.LogWarning("Không tìm thấy ca khám {Id} để gửi thông báo BOOKING_CREATED.", appointmentId);
            return null;
        }

        var recipient = appointment.Patient?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Bệnh nhân trong ca khám {Id} không có số điện thoại. Bỏ qua thông báo.", appointmentId);
            return null;
        }

        // 1. Chống gửi trùng lặp thông báo cho cùng sự kiện (No duplicate event notification)
        var isDuplicate = await _context.Notifications.AnyAsync(
            n => n.AppointmentId == appointmentId && 
                 n.TemplateCode == "BOOKING_CREATED" && 
                 n.Channel == NotificationChannel.Sms && 
                 (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Sent),
            cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Thông báo BOOKING_CREATED cho lịch hẹn {Id} đã tồn tại. Bỏ qua để chống trùng lặp.", appointmentId);
            return null;
        }

        // 2. Chuẩn bị nội dung tối thiểu an toàn (Minimal content - No diagnosis/identifier)
        var timeVn = appointment.StartAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");
        var patientName = appointment.Patient?.FullName ?? "Quý khách";
        var doctorName = appointment.Doctor?.FullName ?? "Bác sĩ phụ trách";

        var content = $"Phòng khám đã tiếp nhận yêu cầu đặt hẹn mã {appointment.BookingCode} của Quý khách {patientName}. Thời gian dự kiến: {timeVn}. Bác sĩ: {doctorName}. Nhân viên y tế sẽ liên hệ xác nhận sớm nhất.";

        var command = new EnqueueNotificationCommand(
            Recipient: recipient,
            Channel: NotificationChannel.Sms,
            TemplateCode: "BOOKING_CREATED",
            Content: content,
            Subject: "Tiếp nhận đặt lịch khám",
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId
        );

        var enqueueResult = await _notificationService.EnqueueNotificationAsync(command, cancellationToken);
        if (!enqueueResult.IsSuccess)
        {
            _logger.LogWarning("Không thể xếp hàng thông báo BOOKING_CREATED: {Error}", enqueueResult.Error);
            return null;
        }

        if (dispatchImmediately)
        {
            await _notificationService.DispatchNotificationAsync(enqueueResult.Value!.Id, cancellationToken);
            var updated = await _notificationService.GetNotificationByIdAsync(enqueueResult.Value.Id, cancellationToken);
            return updated.Value;
        }

        return enqueueResult.Value;
    }

    public async Task<NotificationDto?> NotifyAppointmentConfirmedAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            return null;

        var recipient = appointment.Patient?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(recipient))
            return null;

        // Chống gửi trùng lặp sự kiện xác nhận
        var isDuplicate = await _context.Notifications.AnyAsync(
            n => n.AppointmentId == appointmentId && 
                 n.TemplateCode == "APPOINTMENT_CONFIRMED" && 
                 n.Channel == NotificationChannel.Sms && 
                 (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Sent),
            cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Thông báo APPOINTMENT_CONFIRMED cho lịch hẹn {Id} đã tồn tại. Bỏ qua chống trùng lặp.", appointmentId);
            return null;
        }

        var timeVn = appointment.StartAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");
        var patientName = appointment.Patient?.FullName ?? "Quý khách";
        var doctorName = appointment.Doctor?.FullName ?? "Bác sĩ phụ trách";
        var room = appointment.Doctor?.RoomNumber ?? "Phòng khám";

        var content = $"Lịch khám mã {appointment.BookingCode} của Quý khách {patientName} đã được XÁC NHẬN thành công. Thời gian: {timeVn}. Bác sĩ: {doctorName}, {room}. Địa chỉ: 123 Nguyễn Thị Minh Khai, Q.1, TP.HCM.";

        var command = new EnqueueNotificationCommand(
            Recipient: recipient,
            Channel: NotificationChannel.Sms,
            TemplateCode: "APPOINTMENT_CONFIRMED",
            Content: content,
            Subject: "Xác nhận lịch khám thành công",
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId
        );

        var enqueueResult = await _notificationService.EnqueueNotificationAsync(command, cancellationToken);
        if (!enqueueResult.IsSuccess)
            return null;

        if (dispatchImmediately)
        {
            await _notificationService.DispatchNotificationAsync(enqueueResult.Value!.Id, cancellationToken);
            var updated = await _notificationService.GetNotificationByIdAsync(enqueueResult.Value.Id, cancellationToken);
            return updated.Value;
        }

        return enqueueResult.Value;
    }

    public async Task<NotificationDto?> NotifyRescheduleApprovedAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            return null;

        var recipient = appointment.Patient?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(recipient))
            return null;

        var isDuplicate = await _context.Notifications.AnyAsync(
            n => n.AppointmentId == appointmentId && 
                 n.TemplateCode == "RESCHEDULE_APPROVED" && 
                 n.Channel == NotificationChannel.Sms && 
                 (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Sent),
            cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Thông báo RESCHEDULE_APPROVED cho lịch hẹn {Id} đã tồn tại. Bỏ qua chống trùng lặp.", appointmentId);
            return null;
        }

        var timeVn = appointment.StartAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");
        var patientName = appointment.Patient?.FullName ?? "Quý khách";
        var doctorName = appointment.Doctor?.FullName ?? "Bác sĩ phụ trách";

        var content = $"Yêu cầu đổi lịch khám mã {appointment.BookingCode} của Quý khách {patientName} đã được DUYỆT thành công. Thời gian khám mới: {timeVn}. Bác sĩ: {doctorName}.";

        var command = new EnqueueNotificationCommand(
            Recipient: recipient,
            Channel: NotificationChannel.Sms,
            TemplateCode: "RESCHEDULE_APPROVED",
            Content: content,
            Subject: "Duyệt đổi lịch hẹn khám",
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId
        );

        var enqueueResult = await _notificationService.EnqueueNotificationAsync(command, cancellationToken);
        if (!enqueueResult.IsSuccess)
            return null;

        if (dispatchImmediately)
        {
            await _notificationService.DispatchNotificationAsync(enqueueResult.Value!.Id, cancellationToken);
            var updated = await _notificationService.GetNotificationByIdAsync(enqueueResult.Value.Id, cancellationToken);
            return updated.Value;
        }

        return enqueueResult.Value;
    }

    public async Task<NotificationDto?> NotifyAppointmentCancelledAsync(
        Guid appointmentId, 
        decimal refundAmount, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            return null;

        var recipient = appointment.Patient?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(recipient))
            return null;

        var isDuplicate = await _context.Notifications.AnyAsync(
            n => n.AppointmentId == appointmentId && 
                 n.TemplateCode == "APPOINTMENT_CANCELLED" && 
                 n.Channel == NotificationChannel.Sms && 
                 (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Sent),
            cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Thông báo APPOINTMENT_CANCELLED cho lịch hẹn {Id} đã tồn tại. Bỏ qua chống trùng lặp.", appointmentId);
            return null;
        }

        var refundStr = refundAmount > 0 ? $" Số tiền hoàn dự kiến: {refundAmount:N0} VND." : string.Empty;
        var content = $"Lịch khám mã {appointment.BookingCode} đã được HỦY thành công theo yêu cầu.{refundStr} Cảm ơn Quý khách.";

        var command = new EnqueueNotificationCommand(
            Recipient: recipient,
            Channel: NotificationChannel.Sms,
            TemplateCode: "APPOINTMENT_CANCELLED",
            Content: content,
            Subject: "Thông báo hủy lịch khám",
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId
        );

        var enqueueResult = await _notificationService.EnqueueNotificationAsync(command, cancellationToken);
        if (!enqueueResult.IsSuccess)
            return null;

        if (dispatchImmediately)
        {
            await _notificationService.DispatchNotificationAsync(enqueueResult.Value!.Id, cancellationToken);
            var updated = await _notificationService.GetNotificationByIdAsync(enqueueResult.Value.Id, cancellationToken);
            return updated.Value;
        }

        return enqueueResult.Value;
    }

    public async Task<NotificationDto?> NotifyAppointmentReminderAsync(
        Guid appointmentId, 
        bool dispatchImmediately = true, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            return null;

        var recipient = appointment.Patient?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(recipient))
            return null;

        var isDuplicate = await _context.Notifications.AnyAsync(
            n => n.AppointmentId == appointmentId && 
                 n.TemplateCode == "APPOINTMENT_REMINDER" && 
                 n.Channel == NotificationChannel.Sms && 
                 (n.Status == NotificationStatus.Pending || n.Status == NotificationStatus.Sent),
            cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Thông báo APPOINTMENT_REMINDER cho lịch hẹn {Id} đã tồn tại. Bỏ qua chống trùng lặp.", appointmentId);
            return null;
        }

        var timeVn = appointment.StartAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");
        var patientName = appointment.Patient?.FullName ?? "Quý khách";
        var doctorName = appointment.Doctor?.FullName ?? "Bác sĩ phụ trách";

        var content = $"Nhắc hẹn: Quý khách {patientName} có lịch khám mã {appointment.BookingCode} lúc {timeVn} với Bác sĩ {doctorName}. Quý khách vui lòng đến trước 15 phút để làm thủ tục tiếp đón.";

        var command = new EnqueueNotificationCommand(
            Recipient: recipient,
            Channel: NotificationChannel.Sms,
            TemplateCode: "APPOINTMENT_REMINDER",
            Content: content,
            Subject: "Nhắc hẹn khám bệnh",
            AppointmentId: appointment.Id,
            PatientId: appointment.PatientId
        );

        var enqueueResult = await _notificationService.EnqueueNotificationAsync(command, cancellationToken);
        if (!enqueueResult.IsSuccess)
            return null;

        if (dispatchImmediately)
        {
            await _notificationService.DispatchNotificationAsync(enqueueResult.Value!.Id, cancellationToken);
            var updated = await _notificationService.GetNotificationByIdAsync(enqueueResult.Value.Id, cancellationToken);
            return updated.Value;
        }

        return enqueueResult.Value;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsByAppointmentIdAsync(
        Guid appointmentId, 
        CancellationToken cancellationToken = default)
    {
        return await _notificationService.GetNotificationsByAppointmentIdAsync(appointmentId, cancellationToken);
    }
}
