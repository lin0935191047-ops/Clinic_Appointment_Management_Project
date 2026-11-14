using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Consultations.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Consultations.Commands.StartConsultation;

public record StartConsultationCommand(Guid AppointmentId) : IRequest<Result<StartConsultationResponse>>;

public class StartConsultationCommandHandler : IRequestHandler<StartConsultationCommand, Result<StartConsultationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public StartConsultationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<StartConsultationResponse>> Handle(
        StartConsultationCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ Bác sĩ (Doctor) hoặc Quản trị viên (Admin)
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);
        var isAdmin = _currentUserService.IsAdmin;

        if (!isDoctor && !isAdmin)
        {
            return Result<StartConsultationResponse>.Failure(
                "Bạn không có quyền thực hiện thao tác này. Chức năng chỉ dành cho Bác sĩ phụ trách hoặc Quản trị viên.", 
                403);
        }

        // 2. Tra cứu ca khám
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            return Result<StartConsultationResponse>.Failure(
                "Không tìm thấy thông tin ca khám cần bắt đầu.", 
                404);
        }

        // 3. Phân quyền tài nguyên (Assigned Doctor Only): Bác sĩ chỉ được bắt đầu ca khám của chính mình
        if (isDoctor)
        {
            var currentUserId = _currentUserService.UserId;
            var doctorProfile = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == currentUserId, cancellationToken);

            if (doctorProfile == null)
            {
                return Result<StartConsultationResponse>.Failure(
                    "Tài khoản đăng nhập chưa được liên kết với hồ sơ Bác sĩ trong hệ thống.", 
                    403);
            }

            if (appointment.DoctorId != doctorProfile.Id)
            {
                return Result<StartConsultationResponse>.Failure(
                    "Bạn không được phân công phụ trách ca khám này. Chỉ bác sĩ được phân công mới có quyền bắt đầu khám.", 
                    403);
            }
        }

        // 4. Kiểm tra trạng thái: Chỉ cho phép chuyển từ CheckedIn sang InProgress (CheckedIn to InProgress)
        if (appointment.Status == AppointmentStatus.PendingConfirmation)
        {
            return Result<StartConsultationResponse>.Failure(
                "Lịch hẹn chưa được xác nhận, không thể bắt đầu khám.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Confirmed)
        {
            return Result<StartConsultationResponse>.Failure(
                "Bệnh nhân chưa làm thủ tục tiếp nhận tại quầy lễ tân (CheckedIn), không thể bắt đầu khám.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.InProgress)
        {
            return Result<StartConsultationResponse>.Failure(
                "Ca khám đã được bắt đầu trước đó.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<StartConsultationResponse>.Failure(
                "Ca khám đã hoàn thành khám bệnh.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<StartConsultationResponse>.Failure(
                "Không thể bắt đầu ca khám đã bị hủy.", 
                400);
        }

        if (appointment.Status != AppointmentStatus.CheckedIn)
        {
            return Result<StartConsultationResponse>.Failure(
                $"Không thể bắt đầu ca khám ở trạng thái {appointment.Status}. Chỉ có thể bắt đầu khi ca khám ở trạng thái Đã tiếp nhận (CheckedIn).", 
                400);
        }

        // 5. Cập nhật trạng thái InProgress
        var serverNowUtc = DateTime.UtcNow;
        appointment.StartConsultation(serverNowUtc);

        // 6. Ghi nhận lịch sử kiểm toán bất biến
        var metadata = new
        {
            BookingCode = appointment.BookingCode,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            StartAt = appointment.StartAt,
            StartedAtUtc = serverNowUtc
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var auditLog = AuditLog.Create(
            actorType: isDoctor ? "DOCTOR" : "ADMIN",
            actorUserId: _currentUserService.UserId,
            action: "START_CONSULTATION",
            entityType: "Appointment",
            entityId: appointment.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new StartConsultationResponse(
            AppointmentId: appointment.Id,
            BookingCode: appointment.BookingCode,
            Status: appointment.Status,
            StartedAt: serverNowUtc
        );

        return Result<StartConsultationResponse>.Success(response);
    }
}
