using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

public class AppointmentsController : BaseApiController
{
    /// <summary>
    /// Đặt lịch khám dành cho Khách vãng lai (Guest Booking - Không yêu cầu đăng nhập)
    /// </summary>
    [HttpPost("guest")]
    [ProducesResponseType(typeof(ApiResponse<BookGuestAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookGuest([FromBody] BookGuestAppointmentCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

