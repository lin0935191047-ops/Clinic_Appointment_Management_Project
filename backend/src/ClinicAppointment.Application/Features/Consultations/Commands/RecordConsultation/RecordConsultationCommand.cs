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

namespace ClinicAppointment.Application.Features.Consultations.Commands.RecordConsultation;

public record RecordConsultationCommand(
    Guid AppointmentId,
    string Symptoms,
    string Diagnosis,
    string? ExaminationNotes = null,
    string? Conclusion = null,
    string? FollowUpInstructions = null
) : IRequest<Result<ConsultationDto>>;

public class RecordConsultationCommandValidator : AbstractValidator<RecordConsultationCommand>
{
    public RecordConsultationCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId không được để trống.");

        RuleFor(x => x.Symptoms)
            .NotEmpty().WithMessage("Triệu chứng ban đầu của bệnh nhân là bắt buộc.")
            .MaximumLength(2000).WithMessage("Triệu chứng không được vượt quá 2000 ký tự.");

        RuleFor(x => x.Diagnosis)
            .NotEmpty().WithMessage("Chẩn đoán y khoa của bác sĩ là bắt buộc.")
            .MaximumLength(2000).WithMessage("Chẩn đoán y khoa không được vượt quá 2000 ký tự.");

        RuleFor(x => x.ExaminationNotes)
            .MaximumLength(4000).WithMessage("Nội dung thăm khám không được vượt quá 4000 ký tự.");

        RuleFor(x => x.Conclusion)
            .MaximumLength(2000).WithMessage("Kết luận không được vượt quá 2000 ký tự.");

        RuleFor(x => x.FollowUpInstructions)
            .MaximumLength(2000).WithMessage("Hướng dẫn dặn dò không được vượt quá 2000 ký tự.");
    }
}

public class RecordConsultationCommandHandler : IRequestHandler<RecordConsultationCommand, Result<ConsultationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RecordConsultationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ConsultationDto>> Handle(
        RecordConsultationCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ Bác sĩ hoặc Quản trị viên
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);
        var isAdmin = _currentUserService.IsAdmin;

        if (!isDoctor && !isAdmin)
        {
            return Result<ConsultationDto>.Failure(
                "Bạn không có quyền ghi nhận kết quả khám bệnh. Chức năng chỉ dành cho Bác sĩ phụ trách hoặc Quản trị viên.", 
                403);
        }

        // 2. Tra cứu ca khám
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment == null)
        {
            return Result<ConsultationDto>.Failure(
                "Không tìm thấy thông tin ca khám cần ghi nhận kết quả.", 
                404);
        }

        // 3. Phân quyền tài nguyên (Consultation protected & Assigned Doctor Only)
        Guid currentDoctorId = appointment.DoctorId;
        if (isDoctor)
        {
            var currentUserId = _currentUserService.UserId;
            var doctorProfile = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == currentUserId, cancellationToken);

            if (doctorProfile == null)
            {
                return Result<ConsultationDto>.Failure(
                    "Tài khoản đăng nhập chưa được liên kết với hồ sơ Bác sĩ trong hệ thống.", 
                    403);
            }

            if (appointment.DoctorId != doctorProfile.Id)
            {
                return Result<ConsultationDto>.Failure(
                    "Bạn không được phân công phụ trách ca khám này. Chỉ bác sĩ được phân công mới có quyền ghi nhận kết quả khám.", 
                    403);
            }

            currentDoctorId = doctorProfile.Id;
        }

        // 4. Kiểm tra trạng thái ca khám: Phải ở trạng thái đang khám (InProgress)
        if (appointment.Status == AppointmentStatus.PendingConfirmation || 
            appointment.Status == AppointmentStatus.Confirmed || 
            appointment.Status == AppointmentStatus.CheckedIn)
        {
            return Result<ConsultationDto>.Failure(
                "Ca khám chưa được bắt đầu (InProgress). Vui lòng bắt đầu ca khám trước khi ghi chép kết quả.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<ConsultationDto>.Failure(
                "Không thể ghi nhận kết quả khám cho ca khám đã bị hủy.", 
                400);
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<ConsultationDto>.Failure(
                "Ca khám đã hoàn tất (Completed) và đã khóa hồ sơ bệnh án. Không thể chỉnh sửa kết quả khám.", 
                400);
        }

        // 5. Kiểm tra hoặc tạo bản ghi Consultation
        var existingConsultation = await _context.Consultations
            .FirstOrDefaultAsync(c => c.AppointmentId == request.AppointmentId, cancellationToken);

        var serverNowUtc = DateTime.UtcNow;
        Consultation consultation;

        if (existingConsultation != null)
        {
            // Cập nhật thông tin thăm khám
            existingConsultation.Update(
                symptoms: request.Symptoms,
                diagnosis: request.Diagnosis,
                examinationNotes: request.ExaminationNotes,
                conclusion: request.Conclusion,
                followUpInstructions: request.FollowUpInstructions
            );
            consultation = existingConsultation;
        }
        else
        {
            // Tạo mới bản ghi Consultation
            consultation = Consultation.Create(
                appointmentId: appointment.Id,
                doctorId: currentDoctorId,
                symptoms: request.Symptoms,
                diagnosis: request.Diagnosis,
                examinationNotes: request.ExaminationNotes,
                conclusion: request.Conclusion,
                followUpInstructions: request.FollowUpInstructions,
                consultedAtUtc: serverNowUtc
            );
            _context.Consultations.Add(consultation);
        }

        // 6. Ghi nhận lịch sử kiểm toán bất biến (Bảo mật: Tuyệt đối không log nội dung bệnh án/chẩn đoán y khoa vào AuditLog)
        var metadata = new
        {
            AppointmentId = appointment.Id,
            BookingCode = appointment.BookingCode,
            DoctorId = currentDoctorId,
            PatientId = appointment.PatientId,
            HasSymptoms = !string.IsNullOrWhiteSpace(request.Symptoms),
            HasDiagnosis = !string.IsNullOrWhiteSpace(request.Diagnosis),
            RecordedAtUtc = serverNowUtc
        };

        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var auditLog = AuditLog.Create(
            actorType: isDoctor ? "DOCTOR" : "ADMIN",
            actorUserId: _currentUserService.UserId,
            action: "RECORD_CONSULTATION",
            entityType: "Consultation",
            entityId: consultation.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ConsultationDto(
            Id: consultation.Id,
            AppointmentId: consultation.AppointmentId,
            DoctorId: consultation.DoctorId,
            DoctorName: appointment.Doctor?.FullName ?? "Bác sĩ",
            Symptoms: consultation.Symptoms,
            ExaminationNotes: consultation.ExaminationNotes,
            Diagnosis: consultation.Diagnosis,
            Conclusion: consultation.Conclusion,
            FollowUpInstructions: consultation.FollowUpInstructions,
            ConsultedAt: consultation.ConsultedAt,
            CreatedAt: consultation.CreatedAt,
            UpdatedAt: consultation.UpdatedAt
        );

        return Result<ConsultationDto>.Success(dto);
    }
}
