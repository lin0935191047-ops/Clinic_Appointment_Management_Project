using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Specialties.DTOs;
using ClinicAppointment.Application.Features.Specialties.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/specialties")]
public class SpecialtiesController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách chuyên khoa đang hoạt động (Công khai cho Khách vãng lai và Bệnh nhân)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<SpecialtyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSpecialties(CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetActiveSpecialtiesQuery(), cancellationToken);
        return Ok(ApiResponse<List<SpecialtyDto>>.Ok(list));
    }

    /// <summary>
    /// Lấy thông tin chi tiết một chuyên khoa theo Slug hoặc Id (Công khai)
    /// </summary>
    [HttpGet("{slugOrId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlugOrId(string slugOrId, CancellationToken cancellationToken)
    {
        var specialty = await Mediator.Send(new GetSpecialtyBySlugOrIdQuery(slugOrId), cancellationToken);
        if (specialty == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy chuyên khoa yêu cầu.", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<SpecialtyDto>.Ok(specialty));
    }
}
