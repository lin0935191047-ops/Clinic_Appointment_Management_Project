using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Specialties.Commands;
using ClinicAppointment.Application.Features.Specialties.DTOs;
using ClinicAppointment.Application.Features.Specialties.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/admin/specialties")]
[Authorize(Policy = "StaffOnly")]
public class AdminSpecialtiesController : BaseApiController
{
    /// <summary>
    /// Lấy toàn bộ danh sách chuyên khoa bao gồm cả trạng thái kích hoạt và vô hiệu hóa
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SpecialtyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetAllSpecialtiesQuery(isActive), cancellationToken);
        return Ok(ApiResponse<List<SpecialtyDto>>.Ok(list));
    }

    /// <summary>
    /// Tạo chuyên khoa mới (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSpecialtyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateSpecialtyCommand(request), cancellationToken);
        var response = ApiResponse<SpecialtyDto>.Ok(result, "Tạo chuyên khoa mới thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Cập nhật thông tin chuyên khoa (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpecialtyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateSpecialtyCommand(id, request), cancellationToken);
        return Ok(ApiResponse<SpecialtyDto>.Ok(result, "Cập nhật chuyên khoa thành công."));
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của chuyên khoa (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateSpecialtyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetSpecialtyStatusCommand(id, request.IsActive), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Cập nhật trạng thái chuyên khoa thành công."));
    }

    /// <summary>
    /// Xóa hoặc vô hiệu hóa chuyên khoa an toàn (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteSpecialtyCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Xử lý xóa chuyên khoa thành công."));
    }
}
