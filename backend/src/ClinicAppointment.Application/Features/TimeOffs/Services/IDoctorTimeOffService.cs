using ClinicAppointment.Application.Features.TimeOffs.DTOs;

namespace ClinicAppointment.Application.Features.TimeOffs.Services;

public interface IDoctorTimeOffService
{
    /// <summary>
    /// Kiểm tra xem khoảng thời gian chỉ định có bị trùng vào ngày nghỉ hoặc thời gian bận của bác sĩ không (FR-035)
    /// </summary>
    Task<bool> HasTimeOffOrBlockConflictAsync(
        Guid doctorId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các cuộc hẹn khám hiện có của bác sĩ bị ảnh hưởng (rơi vào khoảng thời gian nghỉ/bận)
    /// Đảm bảo tính bất biến: Không âm thầm xóa/hủy cuộc hẹn của bệnh nhân (FR-055)
    /// </summary>
    Task<List<AppointmentConflictDto>> GetAffectedAppointmentsAsync(
        Guid doctorId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default);
}
