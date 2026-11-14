using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Services.Commands;
using ClinicAppointment.Application.Features.Services.DTOs;
using ClinicAppointment.Application.Features.Services.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/admin/services")]
[Authorize(Policy = "StaffOnly")]
public class AdminServicesController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách toàn bộ dịch vụ khám bệnh trong hệ thống (Quản lý nội bộ)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? specialtyId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetAllServicesQuery(specialtyId, isActive), cancellationToken);
        return Ok(ApiResponse<List<ServiceDto>>.Ok(list));
    }

    /// <summary>
    /// Tạo mới một dịch vụ khám bệnh (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateServiceCommand(request), cancellationToken);
        var response = ApiResponse<ServiceDto>.Ok(result, "Tạo dịch vụ khám bệnh thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Cập nhật thông tin dịch vụ khám bệnh (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateServiceCommand(id, request), cancellationToken);
        return Ok(ApiResponse<ServiceDto>.Ok(result, "Cập nhật dịch vụ khám bệnh thành công."));
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của dịch vụ khám (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateServiceStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetServiceStatusCommand(id, request.IsActive), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Cập nhật trạng thái dịch vụ thành công."));
    }

    /// <summary>
    /// Xóa hoặc vô hiệu hóa an toàn dịch vụ khám (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteServiceCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Xử lý xóa dịch vụ thành công."));
    }
}
