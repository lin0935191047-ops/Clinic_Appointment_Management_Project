using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Scheduling.DTOs;
using ClinicAppointment.Application.Features.Scheduling.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/availability")]
public class AvailabilityController : BaseApiController
{
    /// <summary>
    /// Tra cứu các khung giờ/slot khám khả dụng của Bác sĩ trong ngày cụ thể (Công khai cho Khách và Bệnh nhân)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DoctorAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? serviceId,
        [FromQuery] bool onlyAvailable = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            new GetDoctorAvailabilityQuery(doctorId, date, serviceId, onlyAvailable), cancellationToken);

        return Ok(ApiResponse<DoctorAvailabilityDto>.Ok(result));
    }
}
