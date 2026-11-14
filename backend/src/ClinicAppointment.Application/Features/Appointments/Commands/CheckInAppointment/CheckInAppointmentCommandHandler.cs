using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment;

public class CheckInAppointmentCommandHandler : IRequestHandler<CheckInAppointmentCommand, Result<CheckInAppointmentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CheckInAppointmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CheckInAppointmentResponse>> Handle(
        CheckInAppointmentCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền bảo vệ: Chỉ nhân viên (Receptionist hoặc Admin) mới có quyền tiếp nhận/check-in ca khám
        if (!_currentUserService.IsStaff)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Bạn không có quyền tiếp nhận/check-in ca khám. Chức năng này chỉ dành cho nhân viên Lễ tân hoặc Quản trị viên.", 
                403);
        }

        // 2. Tra cứu ca khám theo Id
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Không tìm thấy thông tin ca khám cần check-in.", 
                404);
        }

        // 3. Kiểm tra trạng thái: Từ chối các trạng thái không hợp lệ (Invalid state denied)
        if (appointment.Status == AppointmentStatus.PendingConfirmation)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Lịch hẹn đang ở trạng thái Chờ xác nhận (PendingConfirmation). Vui lòng xác nhận lịch hẹn trước khi làm thủ tục check-in.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.CheckedIn)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Lịch hẹn đã được check-in tiếp nhận trước đó.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Không thể check-in cho lịch hẹn đã bị hủy.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Không thể check-in cho lịch hẹn đã hoàn thành khám.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.InProgress)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                "Không thể check-in cho lịch hẹn đang trong ca khám.", 
                400);
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return Result<CheckInAppointmentResponse>.Failure(
                $"Không thể check-in cho lịch hẹn ở trạng thái {appointment.Status}. Chỉ có thể check-in cho lịch hẹn Đã xác nhận (Confirmed).", 
                400);
        }

        // 4. Chuyển trạng thái sang CheckedIn (Confirmed to CheckedIn)
        var serverNowUtc = DateTime.UtcNow;
        appointment.CheckIn();
        appointment.UpdatedAt = serverNowUtc;

        // 5. Ghi nhận lịch sử kiểm toán bất biến (Actor/time recorded)
        var actorType = _currentUserService.IsAdmin 
            ? "ADMIN" 
            : (_currentUserService.IsInRole(Role.ReceptionistCode) ? "RECEPTIONIST" : "STAFF");

        var metadata = new
        {
            BookingCode = appointment.BookingCode,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            StartAt = appointment.StartAt,
            CheckedInAt = serverNowUtc,
            Note = request.Note
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var auditLog = AuditLog.Create(
            actorType: actorType,
            actorUserId: _currentUserService.UserId,
            action: "CHECK_IN_APPOINTMENT",
            entityType: "Appointment",
            entityId: appointment.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        // 6. Lưu thay đổi vào CSDL
        await _context.SaveChangesAsync(cancellationToken);

        var response = new CheckInAppointmentResponse(
            AppointmentId: appointment.Id,
            BookingCode: appointment.BookingCode,
            Status: appointment.Status,
            StartAt: appointment.StartAt,
            EndAt: appointment.EndAt,
            CheckedInAt: serverNowUtc,
            Note: request.Note
        );

        return Result<CheckInAppointmentResponse>.Success(response);
    }
}
