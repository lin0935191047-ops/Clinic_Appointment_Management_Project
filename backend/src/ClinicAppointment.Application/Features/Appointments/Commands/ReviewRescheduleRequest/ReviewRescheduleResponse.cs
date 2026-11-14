using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;

public record ReviewRescheduleResponse(
    Guid RequestId,
    Guid AppointmentId,
    string BookingCode,
    RescheduleStatus Status,
    DateTime CurrentStartAtUtc,
    DateTime CurrentEndAtUtc,
    Guid DoctorId,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    string? ReviewNotes,
    string Message
);

public record RescheduleRequestDetailDto(
    Guid RequestId,
    Guid AppointmentId,
    string BookingCode,
    string PatientFullName,
    string PatientPhoneNumber,
    string ServiceName,
    Guid CurrentDoctorId,
    string CurrentDoctorFullName,
    DateTime CurrentStartAtUtc,
    DateTime CurrentEndAtUtc,
    Guid RequestedDoctorId,
    string RequestedDoctorFullName,
    DateTime ProposedStartAtUtc,
    DateTime ProposedEndAtUtc,
    string? Reason,
    RescheduleStatus Status,
    DateTime CreatedAtUtc
);
