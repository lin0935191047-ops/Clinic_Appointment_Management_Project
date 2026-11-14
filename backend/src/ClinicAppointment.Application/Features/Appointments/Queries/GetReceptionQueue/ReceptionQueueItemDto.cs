using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Appointments.Queries.GetReceptionQueue;

/// <summary>
/// DTO thông tin ca khám hiển thị trên hàng đợi tiếp nhận của Lễ tân (Reception Queue Item)
/// Giới hạn dữ liệu an toàn phục vụ điều phối và tiếp đón bệnh nhân tại quầy.
/// </summary>
public record ReceptionQueueItemDto(
    Guid AppointmentId,
    string BookingCode,
    int QueueNumber,
    Guid DoctorId,
    string DoctorName,
    string? DoctorTitle,
    string? RoomNumber,
    Guid PatientId,
    string PatientName,
    string PatientCode,
    DateOnly? DateOfBirth,
    int? BirthYear,
    string? Gender,
    string MaskedPhoneNumber,
    Guid ServiceId,
    string ServiceName,
    int ServiceDurationMinutes,
    decimal ServicePrice,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status,
    string? ReasonForVisit,
    DateTime? CheckedInAt,
    bool IsGuest
);
