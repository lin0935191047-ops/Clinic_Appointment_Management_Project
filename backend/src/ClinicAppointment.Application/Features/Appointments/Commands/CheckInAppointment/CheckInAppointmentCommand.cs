using ClinicAppointment.Application.Common.Models;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment;

/// <summary>
/// Lệnh tiếp nhận và check-in ca khám tại quầy lễ tân
/// </summary>
/// <param name="AppointmentId">Định danh duy nhất của ca khám</param>
/// <param name="Note">Ghi chú của lễ tân khi tiếp nhận bệnh nhân tại quầy (tùy chọn)</param>
public record CheckInAppointmentCommand(Guid AppointmentId, string? Note = null) : IRequest<Result<CheckInAppointmentResponse>>;
