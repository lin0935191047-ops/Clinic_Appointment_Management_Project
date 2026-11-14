using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Enums;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Queries.GetReceptionQueue;

/// <summary>
/// Query lấy danh sách hàng đợi khám theo Bác sĩ và Ngày khám dành cho Lễ tân
/// </summary>
public record GetReceptionQueueQuery(
    Guid? DoctorId = null,
    DateOnly? Date = null,
    AppointmentStatus? Status = null,
    string? Keyword = null,
    string? SortBy = null, // "time_asc" (default), "time_desc", "status"
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<ReceptionQueueItemDto>>>;
