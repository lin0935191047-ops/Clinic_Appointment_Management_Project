using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Doctors.DTOs;
using ClinicAppointment.Application.Features.Doctors.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/doctors")]
public class DoctorsController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách bác sĩ đang hoạt động (Công khai cho Khách vãng lai và Bệnh nhân xem, lọc theo chuyên khoa)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveDoctors(
        [FromQuery] Guid? specialtyId, 
        [FromQuery] string? specialtySlug, 
        CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetActiveDoctorsQuery(specialtyId, specialtySlug), cancellationToken);
        return Ok(ApiResponse<List<DoctorDto>>.Ok(list));
    }

    /// <summary>
    /// Lấy chi tiết hồ sơ chuyên môn của Bác sĩ (Công khai)
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await Mediator.Send(new GetDoctorByIdQuery(id), cancellationToken);
        if (doctor == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin bác sĩ.", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<DoctorDto>.Ok(doctor));
    }
}
