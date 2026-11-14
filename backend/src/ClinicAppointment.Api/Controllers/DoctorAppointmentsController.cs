using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Queries.GetDoctorAppointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/doctor/appointments")]
[Authorize(Policy = "StaffOrDoctor")]
public class DoctorAppointmentsController : BaseApiController
{
    /// <summary>
    /// Bác sĩ xem danh sách các ca khám được phân công cho mình (Assigned doctor appointments)
    /// Hỗ trợ lọc theo ngày khám, trạng thái (CheckedIn, InProgress, Confirmed...), và phân trang.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<DoctorAssignedAppointmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAssignedAppointments(
        [FromQuery] GetDoctorAppointmentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
