using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Schedules.Commands;
using ClinicAppointment.Application.Features.Schedules.DTOs;
using ClinicAppointment.Application.Features.Schedules.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/admin/doctor-schedules")]
[Authorize(Policy = "StaffOnly")]
public class AdminDoctorSchedulesController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách lịch làm việc định kỳ của bác sĩ (Quản lý nội bộ)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorScheduleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly? effectiveDate,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var list = await Mediator.Send(new GetDoctorSchedulesQuery(doctorId, effectiveDate, isActive), cancellationToken);
        return Ok(ApiResponse<List<DoctorScheduleDto>>.Ok(list));
    }

    /// <summary>
    /// Lấy chi tiết một ca làm việc định kỳ
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await Mediator.Send(new GetDoctorScheduleByIdQuery(id), cancellationToken);
        if (schedule == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin ca làm việc.", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<DoctorScheduleDto>.Ok(schedule));
    }

    /// <summary>
    /// Thiết lập ca làm việc định kỳ mới cho Bác sĩ (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateDoctorScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateDoctorScheduleCommand(request), cancellationToken);
        var response = ApiResponse<DoctorScheduleDto>.Ok(result, "Tạo ca làm việc định kỳ thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Cập nhật thông tin ca làm việc định kỳ của Bác sĩ (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateDoctorScheduleCommand(id, request), cancellationToken);
        return Ok(ApiResponse<DoctorScheduleDto>.Ok(result, "Cập nhật ca làm việc định kỳ thành công."));
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của ca làm việc (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateDoctorScheduleStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetDoctorScheduleStatusCommand(id, request.IsActive), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Cập nhật trạng thái ca làm việc thành công."));
    }

    /// <summary>
    /// Xóa ca làm việc định kỳ (Dành riêng cho Quản trị viên)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteDoctorScheduleCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Xóa ca làm việc định kỳ thành công."));
    }
}
