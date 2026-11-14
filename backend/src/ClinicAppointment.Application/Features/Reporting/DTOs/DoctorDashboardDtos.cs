using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Features.Reporting.DTOs;

/// <summary>
/// Chi tiết ca khám hiển thị trên Dashboard của Bác sĩ trong ngày
/// </summary>
public record DoctorDashboardAppointmentItemDto(
    Guid AppointmentId,
    string BookingCode,
    int QueueNumber,
    Guid PatientId,
    string PatientName,
    string MaskedPhoneNumber,
    string ServiceName,
    int DurationMinutes,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string LocalStartTime,
    string LocalEndTime,
    AppointmentStatus Status,
    string? ReasonForVisit,
    bool HasConsultation,
    DateTime? CheckedInAtUtc
);

/// <summary>
/// Thống kê chỉ số số lượng ca khám của Bác sĩ trong ngày
/// </summary>
public record DoctorDashboardMetricsDto(
    int TotalAppointments,
    int PendingConfirmationCount,
    int ConfirmedCount,
    int CheckedInCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    double CompletionRate
);

/// <summary>
/// DTO toàn diện cho Dashboard Bác sĩ (Doctor Dashboard)
/// </summary>
public record DoctorDashboardDto(
    Guid DoctorId,
    string DoctorName,
    string SpecialtyName,
    string? RoomNumber,
    DateOnly Date,
    DoctorDashboardMetricsDto Metrics,
    IReadOnlyList<DoctorDashboardAppointmentItemDto> TodayAppointments,
    IReadOnlyList<DoctorDashboardAppointmentItemDto> WaitingQueue,
    DoctorDashboardAppointmentItemDto? CurrentActiveAppointment,
    DateTime GeneratedAtUtc
);
