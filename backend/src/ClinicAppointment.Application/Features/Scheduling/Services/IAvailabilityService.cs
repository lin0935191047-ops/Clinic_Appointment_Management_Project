using ClinicAppointment.Application.Features.Scheduling.DTOs;

namespace ClinicAppointment.Application.Features.Scheduling.Services;

public interface IAvailabilityService
{
    /// <summary>
    /// Tính toán các khung giờ/slot khả dụng của bác sĩ trong ngày cụ thể (FR-021, FR-035, FR-054)
    /// </summary>
    Task<DoctorAvailabilityDto> GetDoctorAvailabilityAsync(
        Guid doctorId,
        DateOnly date,
        Guid? serviceId = null,
        bool onlyAvailable = false,
        CancellationToken cancellationToken = default);
}
