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

public record CompleteConsultationRequest(
    string? Conclusion = null,
    string? FollowUpInstructions = null
);

public record CompleteConsultationResponse(
    Guid AppointmentId,
    string BookingCode,
    AppointmentStatus Status,
    Guid DoctorId,
    string DoctorName,
    Guid PatientId,
    string PatientFullName,
    string Symptoms,
    string Diagnosis,
    string? ExaminationNotes,
    string? Conclusion,
    string? FollowUpInstructions,
    DateTime? ConsultedAt,
    DateTime CompletedAt
);
