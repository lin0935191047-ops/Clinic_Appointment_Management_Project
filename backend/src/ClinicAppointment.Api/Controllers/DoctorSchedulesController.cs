using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Schedules.DTOs;
using ClinicAppointment.Application.Features.Schedules.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/doctors/{doctorId:guid}/schedules")]
public class DoctorSchedulesController : BaseApiController
{
    /// <summary>
    /// Lấy lịch làm việc định kỳ hàng tuần của Bác sĩ (Công khai cho Khách và Bệnh nhân xem khi đặt lịch)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicDoctorSchedules(Guid doctorId, CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetPublicDoctorSchedulesQuery(doctorId), cancellationToken);
        return Ok(ApiResponse<List<DoctorScheduleDto>>.Ok(list));
    }
}
