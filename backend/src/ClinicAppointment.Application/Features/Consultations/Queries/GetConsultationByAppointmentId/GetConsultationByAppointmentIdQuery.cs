using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Consultations.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Consultations.Queries.GetConsultationByAppointmentId;

public record GetConsultationByAppointmentIdQuery(Guid AppointmentId) : IRequest<Result<ConsultationDto>>;

public class GetConsultationByAppointmentIdQueryHandler : IRequestHandler<GetConsultationByAppointmentIdQuery, Result<ConsultationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetConsultationByAppointmentIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ConsultationDto>> Handle(
        GetConsultationByAppointmentIdQuery request, 
        CancellationToken cancellationToken)
    {
        var isDoctor = _currentUserService.IsInRole(Role.DoctorCode);
        var isAdmin = _currentUserService.IsAdmin;

        if (!isDoctor && !isAdmin)
        {
            return Result<ConsultationDto>.Failure(
                "Bạn không có quyền truy cập hồ sơ bệnh án thăm khám này.", 
                403);
        }

        var consultation = await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Doctor)
            .FirstOrDefaultAsync(c => c.AppointmentId == request.AppointmentId, cancellationToken);

        if (consultation == null)
        {
            return Result<ConsultationDto>.Failure(
                "Không tìm thấy hồ sơ thăm khám của ca khám này.", 
                404);
        }

        // Kiểm tra phân quyền: Bác sĩ chỉ xem kết quả ca khám do mình phụ trách
        if (isDoctor)
        {
            var currentUserId = _currentUserService.UserId;
            var doctorProfile = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == currentUserId, cancellationToken);

            if (doctorProfile == null || consultation.DoctorId != doctorProfile.Id)
            {
                return Result<ConsultationDto>.Failure(
                    "Bạn không được phân công phụ trách ca khám này. Quyền truy cập hồ sơ bệnh án bị từ chối.", 
                    403);
            }
        }

        var dto = new ConsultationDto(
            Id: consultation.Id,
            AppointmentId: consultation.AppointmentId,
            DoctorId: consultation.DoctorId,
            DoctorName: consultation.Doctor?.FullName ?? "Bác sĩ",
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
