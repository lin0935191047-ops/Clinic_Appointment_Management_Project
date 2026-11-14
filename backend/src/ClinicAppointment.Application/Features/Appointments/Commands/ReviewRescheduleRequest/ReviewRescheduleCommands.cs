using ClinicAppointment.Application.Common.Models;
using MediatR;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;

public record ApproveRescheduleRequestBody(string? ReviewNotes = null);

public record ApproveRescheduleRequestCommand(
    Guid RequestId,
    string? ReviewNotes = null
) : IRequest<Result<ReviewRescheduleResponse>>;

public record RejectRescheduleRequestBody(string ReviewNotes);

public record RejectRescheduleRequestCommand(
    Guid RequestId,
    string ReviewNotes
) : IRequest<Result<ReviewRescheduleResponse>>;
