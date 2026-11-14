using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Notifications.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment;

public class ConfirmAppointmentCommandHandler : IRequestHandler<ConfirmAppointmentCommand, Result<ConfirmAppointmentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppointmentNotificationService? _appointmentNotificationService;

    public ConfirmAppointmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAppointmentNotificationService? appointmentNotificationService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _appointmentNotificationService = appointmentNotificationService;
    }

    public async Task<Result<ConfirmAppointmentResponse>> Handle(
        ConfirmAppointmentCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Kiểm tra quyền hạn: Chỉ Receptionist hoặc Admin mới được xác nhận lịch hẹn
        if (!_currentUserService.IsStaff)
        {
            return Result<ConfirmAppointmentResponse>.Failure(
                "Bạn không có quyền xác nhận lịch hẹn. Chức năng này chỉ dành cho nhân viên Lễ tân hoặc Quản trị viên.", 
                403);
        }

        // 2. Tra cứu ca khám theo Id
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            return Result<ConfirmAppointmentResponse>.Failure(
                "Không tìm thấy thông tin ca khám cần xác nhận.", 
                404);
        }

        // 3. Kiểm tra trạng thái: Chỉ cho phép xác nhận khi ca khám đang ở trạng thái PendingConfirmation
        if (appointment.Status != AppointmentStatus.PendingConfirmation)
        {
            return Result<ConfirmAppointmentResponse>.Failure(
                $"Không thể xác nhận lịch hẹn ở trạng thái {appointment.Status}. Chỉ có thể xác nhận lịch hẹn ở trạng thái Chờ xác nhận (PendingConfirmation).", 
                400);
        }

        // 4. Cập nhật trạng thái sang Confirmed
        appointment.Confirm();

        // 5. Ghi nhận lịch sử kiểm toán (Audit Log)
        var actorType = _currentUserService.IsAdmin 
            ? "ADMIN" 
            : (_currentUserService.IsInRole(Role.ReceptionistCode) ? "RECEPTIONIST" : "STAFF");

        var metadata = new
        {
            BookingCode = appointment.BookingCode,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            StartAt = appointment.StartAt,
            ConfirmedAt = appointment.UpdatedAt ?? DateTime.UtcNow,
            Note = request.Note
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var auditLog = AuditLog.Create(
            actorType: actorType,
            actorUserId: _currentUserService.UserId,
            action: "CONFIRM_APPOINTMENT",
            entityType: "Appointment",
            entityId: appointment.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        // 6. Lưu thay đổi vào CSDL
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Kích hoạt thông báo xác nhận lịch hẹn (Outbox pattern)
        if (_appointmentNotificationService != null)
        {
            try
            {
                await _appointmentNotificationService.NotifyAppointmentConfirmedAsync(
                    appointment.Id, 
                    dispatchImmediately: true, 
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // Resilient outbox: không làm gián đoạn response nếu việc dispatch gặp sự cố
            }
        }

        var response = new ConfirmAppointmentResponse(
            AppointmentId: appointment.Id,
            BookingCode: appointment.BookingCode,
            Status: appointment.Status,
            StartAt: appointment.StartAt,
            EndAt: appointment.EndAt,
            ConfirmedAt: appointment.UpdatedAt ?? DateTime.UtcNow,
            Note: request.Note
        );

        return Result<ConfirmAppointmentResponse>.Success(response, 200);
    }
}
