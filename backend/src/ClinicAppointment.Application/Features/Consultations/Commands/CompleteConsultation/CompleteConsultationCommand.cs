using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Consultations.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Consultations.Commands.CompleteConsultation;

public record CompleteConsultationCommand(
    Guid AppointmentId,
    string? Conclusion = null,
    string? FollowUpInstructions = null
) : IRequest<Result<CompleteConsultationResponse>>;

public class CompleteConsultationCommandValidator : AbstractValidator<CompleteConsultationCommand>
{
    public CompleteConsultationCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId không được để trống.");

        RuleFor(x => x.Conclusion)
            .MaximumLength(2000).WithMessage("Kết luận không được vượt quá 2000 ký tự.");

        RuleFor(x => x.FollowUpInstructions)
            .MaximumLength(2000).WithMessage("Hướng dẫn dặn dò không được vượt quá 2000 ký tự.");
    }
}

public class CompleteConsultationCommandHandler : IRequestHandler<CompleteConsultationCommand, Result<CompleteConsultationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteConsultationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CompleteConsultationResponse>> Handle(
        CompleteConsultationCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền vai trò: Chỉ Bác sĩ hoặc Quản trị viên
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);
        var isAdmin = _currentUserService.IsAdmin;

        if (!isDoctor && !isAdmin)
        {
            return Result<CompleteConsultationResponse>.Failure(
                "Bạn không có quyền hoàn tất ca khám. Chức năng chỉ dành cho Bác sĩ phụ trách hoặc Quản trị viên.", 
                403);
        }

        // 2. Tra cứu ca khám
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            return Result<CompleteConsultationResponse>.Failure(
                "Không tìm thấy thông tin ca khám cần hoàn tất.", 
                404);
        }

        // 3. Phân quyền tài nguyên: Bác sĩ được phân công (Assigned Doctor Only) chống IDOR
        if (isDoctor)
        {
            var currentUserId = _currentUserService.UserId;
            var doctorProfile = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == currentUserId, cancellationToken);

            if (doctorProfile == null)
            {
                return Result<CompleteConsultationResponse>.Failure(
                    "Tài khoản đăng nhập chưa được liên kết với hồ sơ Bác sĩ trong hệ thống.", 
                    403);
            }

            if (appointment.DoctorId != doctorProfile.Id)
            {
                return Result<CompleteConsultationResponse>.Failure(
                    "Bạn không được phân công phụ trách ca khám này. Chỉ bác sĩ được phân công mới có quyền hoàn tất ca khám.", 
                    403);
            }
        }

        // 4. Kiểm tra trạng thái ca khám: Phải ở trạng thái InProgress
        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<CompleteConsultationResponse>.Failure(
                "Ca khám đã được hoàn tất trước đó và đã khóa hồ sơ.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<CompleteConsultationResponse>.Failure(
                "Không thể hoàn tất ca khám đã bị hủy.", 
                400);
        }

        if (appointment.Status != AppointmentStatus.InProgress)
        {
            return Result<CompleteConsultationResponse>.Failure(
                $"Không thể hoàn tất ca khám ở trạng thái {appointment.Status}. Ca khám phải đang diễn ra (InProgress) mới có thể hoàn tất.", 
                400);
        }

        // 5. Kiểm tra hồ sơ thăm khám lâm sàng (Consultation record)
        var consultation = await _context.Consultations
            .FirstOrDefaultAsync(c => c.AppointmentId == request.AppointmentId, cancellationToken);

        if (consultation == null)
        {
            return Result<CompleteConsultationResponse>.Failure(
                "Ca khám chưa được ghi chép kết quả thăm khám và chẩn đoán y khoa. Vui lòng ghi chép kết quả khám trước khi hoàn tất.", 
                400);
        }

        var serverNowUtc = DateTime.UtcNow;

        // 6. Cập nhật kết luận và lời dặn nếu được bổ sung lúc hoàn tất
        if (!string.IsNullOrWhiteSpace(request.Conclusion))
        {
            consultation.Conclusion = request.Conclusion.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.FollowUpInstructions))
        {
            consultation.FollowUpInstructions = request.FollowUpInstructions.Trim();
        }

        consultation.UpdatedAt = serverNowUtc;

        // 7. Chuyển trạng thái Appointment sang Completed
        appointment.CompleteConsultation(serverNowUtc);

        // 8. Ghi nhận lịch sử kiểm toán bất biến (Bảo mật: Không log nội dung chẩn đoán/bệnh án vào AuditLog)
        var metadata = new
        {
            AppointmentId = appointment.Id,
            BookingCode = appointment.BookingCode,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            HasConclusion = !string.IsNullOrWhiteSpace(consultation.Conclusion),
            HasFollowUpInstructions = !string.IsNullOrWhiteSpace(consultation.FollowUpInstructions),
            CompletedAtUtc = serverNowUtc
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var auditLog = AuditLog.Create(
            actorType: isDoctor ? "DOCTOR" : "ADMIN",
            actorUserId: _currentUserService.UserId,
            action: "COMPLETE_CONSULTATION",
            entityType: "Appointment",
            entityId: appointment.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CompleteConsultationResponse(
            AppointmentId: appointment.Id,
            BookingCode: appointment.BookingCode,
            Status: appointment.Status,
            DoctorId: appointment.DoctorId,
            DoctorName: appointment.Doctor?.FullName ?? "Bác sĩ",
            PatientId: appointment.PatientId,
            PatientFullName: appointment.Patient?.FullName ?? "Bệnh nhân",
            Symptoms: consultation.Symptoms,
            Diagnosis: consultation.Diagnosis,
            ExaminationNotes: consultation.ExaminationNotes,
            Conclusion: consultation.Conclusion,
            FollowUpInstructions: consultation.FollowUpInstructions,
            ConsultedAt: consultation.ConsultedAt,
            CompletedAt: serverNowUtc
        );

        return Result<CompleteConsultationResponse>.Success(response);
    }
}
