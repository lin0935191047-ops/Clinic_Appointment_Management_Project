using ClinicAppointment.Application.Common.Models;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment;

public record ConfirmAppointmentCommand(
    Guid AppointmentId,
    string? Note = null
) : IRequest<Result<ConfirmAppointmentResponse>>;
