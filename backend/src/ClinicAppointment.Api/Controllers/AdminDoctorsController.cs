using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Doctors.Commands;
using ClinicAppointment.Application.Features.Doctors.DTOs;
using ClinicAppointment.Application.Features.Doctors.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/admin/doctors")]
[Authorize(Policy = "StaffOnly")]
public class AdminDoctorsController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách toàn bộ bác sĩ (Quản lý nội bộ)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? specialtyId, 
        [FromQuery] bool? isActive, 
        CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetAllDoctorsQuery(specialtyId, isActive), cancellationToken);
        return Ok(ApiResponse<List<DoctorDto>>.Ok(list));
    }

    /// <summary>
    /// Tạo mới hồ sơ bác sĩ và tài khoản đăng nhập staff DOCTOR (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateDoctorRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateDoctorCommand(request), cancellationToken);
        var response = ApiResponse<DoctorDto>.Ok(result, "Tạo hồ sơ bác sĩ thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Cập nhật thông tin bác sĩ và chuyên khoa phụ trách (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateDoctorCommand(id, request), cancellationToken);
        return Ok(ApiResponse<DoctorDto>.Ok(result, "Cập nhật hồ sơ bác sĩ thành công."));
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của bác sĩ (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateDoctorStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetDoctorStatusCommand(id, request.IsActive), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Cập nhật trạng thái bác sĩ thành công."));
    }

    /// <summary>
    /// Xóa hoặc vô hiệu hóa bác sĩ an toàn (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteDoctorCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Xử lý xóa bác sĩ thành công."));
    }
}
