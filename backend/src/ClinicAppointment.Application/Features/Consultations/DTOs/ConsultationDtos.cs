using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Consultations.DTOs;

public record StartConsultationResponse(
    Guid AppointmentId,
    string BookingCode,
    AppointmentStatus Status,
    DateTime StartedAt
);

public record ConsultationDto(
    Guid Id,
    Guid AppointmentId,
    Guid DoctorId,
    string DoctorName,
    string Symptoms,
    string? ExaminationNotes,
    string Diagnosis,
    string? Conclusion,
    string? FollowUpInstructions,
    DateTime? ConsultedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record RecordConsultationRequest(
    string Symptoms,
    string Diagnosis,
    string? ExaminationNotes = null,
    string? Conclusion = null,
    string? FollowUpInstructions = null
);
