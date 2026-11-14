using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Enums;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Queries.GetDoctorAppointments;

/// <summary>
/// Query lấy danh sách ca khám được phân công cho Bác sĩ (Assigned doctor only)
/// Hỗ trợ lọc theo ngày, trạng thái, và phân trang.
/// </summary>
public record GetDoctorAppointmentsQuery(
    Guid? DoctorId = null, // Tùy chọn (dành cho Admin quản lý; với Bác sĩ hệ thống luôn tự động ép theo hồ sơ cá nhân)
    DateOnly? Date = null,
    AppointmentStatus? Status = null,
    string? Keyword = null,
    string? SortBy = null, // "time_asc" (default), "time_desc", "status"
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<DoctorAssignedAppointmentDto>>>;
