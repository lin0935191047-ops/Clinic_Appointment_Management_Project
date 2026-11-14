using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Services.DTOs;
using ClinicAppointment.Application.Features.Services.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
public class ServicesController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách dịch vụ khám đang hoạt động (Công khai cho Khách vãng lai và Bệnh nhân xem)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveServices(
        [FromQuery] Guid? specialtyId,
        [FromQuery] string? specialtySlug,
        CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetActiveServicesQuery(specialtyId, specialtySlug), cancellationToken);
        return Ok(ApiResponse<List<ServiceDto>>.Ok(list));
    }

    /// <summary>
    /// Lấy chi tiết thông tin dịch vụ khám bệnh (Công khai)
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await Mediator.Send(new GetServiceByIdQuery(id), cancellationToken);
        if (service == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin dịch vụ khám.", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<ServiceDto>.Ok(service));
    }
}
