using System.Security.Claims;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.TimeOffs.Commands;
using ClinicAppointment.Application.Features.TimeOffs.DTOs;
using ClinicAppointment.Application.Features.TimeOffs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/admin/doctor-time-offs")]
[Authorize(Policy = "StaffOrDoctor")]
public class AdminDoctorTimeOffsController : BaseApiController
{
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private bool IsStaff()
    {
        return User.IsInRole("ADMIN") || User.IsInRole("RECEPTIONIST");
    }

    /// <summary>
    /// Lấy danh sách ngày nghỉ của bác sĩ trong khoảng thời gian
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorTimeOffDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTimeOffs(
        [FromQuery] Guid doctorId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var list = await Mediator.Send(new GetDoctorTimeOffsQuery(doctorId, fromDate, toDate, onlyActive), cancellationToken);
        return Ok(ApiResponse<List<DoctorTimeOffDto>>.Ok(list));
    }

    /// <summary>
    /// Đăng ký ngày nghỉ cho Bác sĩ (Bác sĩ chỉ đăng ký cho chính mình; Nhân viên có quyền đăng ký cho mọi bác sĩ)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DoctorTimeOffDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTimeOff(
        [FromBody] CreateDoctorTimeOffRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsStaff();

        var result = await Mediator.Send(new CreateDoctorTimeOffCommand(request, userId, isStaff), cancellationToken);
        var response = ApiResponse<DoctorTimeOffDto>.Ok(result, "Đăng ký ngày nghỉ thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Hủy đăng ký ngày nghỉ
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTimeOff(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsStaff();

        var result = await Mediator.Send(new CancelDoctorTimeOffCommand(id, userId, isStaff), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Hủy ngày nghỉ thành công."));
    }

    /// <summary>
    /// Lấy danh sách khung giờ bận (Họp, nghỉ giữa giờ, bảo trì...) của bác sĩ
    /// </summary>
    [HttpGet("blocks")]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorScheduleBlockDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleBlocks(
        [FromQuery] Guid doctorId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var list = await Mediator.Send(new GetDoctorScheduleBlocksQuery(doctorId, fromDate, toDate, onlyActive), cancellationToken);
        return Ok(ApiResponse<List<DoctorScheduleBlockDto>>.Ok(list));
    }

    /// <summary>
    /// Thiết lập khung giờ bận cho bác sĩ
    /// </summary>
    [HttpPost("blocks")]
    [ProducesResponseType(typeof(ApiResponse<DoctorScheduleBlockDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateScheduleBlock(
        [FromBody] CreateDoctorScheduleBlockRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsStaff();

        var result = await Mediator.Send(new CreateDoctorScheduleBlockCommand(request, userId, isStaff), cancellationToken);
        var response = ApiResponse<DoctorScheduleBlockDto>.Ok(result, "Thiết lập khung giờ bận thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Vô hiệu hóa khung giờ bận
    /// </summary>
    [HttpDelete("blocks/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateScheduleBlock(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isStaff = IsStaff();

        var result = await Mediator.Send(new DeactivateDoctorScheduleBlockCommand(id, userId, isStaff), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Vô hiệu hóa khung giờ bận thành công."));
    }

    /// <summary>
    /// Kiểm tra xem khoảng thời gian có bị xung đột với ngày nghỉ hoặc lịch bận của bác sĩ không
    /// </summary>
    [HttpGet("check-conflict")]
    [ProducesResponseType(typeof(ApiResponse<CheckDoctorTimeOffConflictResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckConflict(
        [FromQuery] Guid doctorId,
        [FromQuery] DateTime startAt,
        [FromQuery] DateTime endAt,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CheckDoctorTimeOffConflictQuery(doctorId, startAt, endAt), cancellationToken);
        return Ok(ApiResponse<CheckDoctorTimeOffConflictResult>.Ok(result));
    }
}
