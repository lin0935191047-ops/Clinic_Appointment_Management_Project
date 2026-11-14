using ClinicAppointment.Application.Common.Models;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest;

public record SubmitRescheduleRequestBody(
    DateTime ProposedStartAtUtc,
    string? GrantToken = null,
    Guid? RequestedDoctorId = null,
    string? Reason = null
);

public record SubmitRescheduleRequestCommand(
    string BookingCode,
    DateTime ProposedStartAtUtc,
    string? GrantToken = null,
    Guid? RequestedDoctorId = null,
    string? Reason = null
) : IRequest<Result<SubmitRescheduleRequestResponse>>;
