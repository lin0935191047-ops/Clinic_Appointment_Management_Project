using ClinicAppointment.Application.Common.Models;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;

public record CancelAppointmentBody(
    string? Reason = null,
    string? GrantToken = null
);

public record CancelAppointmentCommand(
    string BookingCode,
    string? Reason = null,
    string? GrantToken = null
) : IRequest<Result<CancelAppointmentResponse>>;
