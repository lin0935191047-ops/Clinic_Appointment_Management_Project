using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment;
using ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

public record ConfirmAppointmentRequest(string? Note = null);
public record CheckInAppointmentRequest(string? Note = null);

[Route("api/v1/reception/appointments")]
[Authorize(Policy = "StaffOnly")]
public class ReceptionAppointmentsController : BaseApiController
{
    /// <summary>
    /// Lễ tân xác nhận ca khám từ trạng thái Chờ xác nhận (PendingConfirmation) sang Đã xác nhận (Confirmed)
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAppointment(
        [FromRoute] Guid id,
        [FromBody] ConfirmAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmAppointmentCommand(id, request?.Note);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lễ tân tiếp nhận và check-in ca khám tại quầy từ trạng thái Đã xác nhận (Confirmed) sang Đã tiếp nhận (CheckedIn)
    /// </summary>
    [HttpPost("{id:guid}/check-in")]
    [ProducesResponseType(typeof(ApiResponse<CheckInAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckInAppointment(
        [FromRoute] Guid id,
        [FromBody] CheckInAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CheckInAppointmentCommand(id, request?.Note);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
