using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Refunds.DTOs;
using ClinicAppointment.Domain.Enums;
using MediatR;

namespace ClinicAppointment.Application.Features.Refunds.Commands.ProcessRefund;

public record ProcessRefundCommand(
    Guid RefundId,
    RefundStatus TargetStatus,
    string? ProviderReference = null,
    string? FailureReason = null,
    string? Note = null
) : IRequest<Result<RefundDetailDto>>;
