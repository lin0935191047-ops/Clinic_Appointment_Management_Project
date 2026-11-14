using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;
using ClinicAppointment.Application.Features.Appointments.DTOs;
using ClinicAppointment.Application.Features.Appointments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

public class AppointmentsController : BaseApiController
{
    private readonly IResourceAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public AppointmentsController(
        IResourceAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Đặt lịch khám dành cho Khách vãng lai (Guest Booking - Không yêu cầu đăng nhập)
    /// </summary>
    [HttpPost("guest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BookGuestAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookGuest([FromBody] BookGuestAppointmentCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem chi tiết lịch khám theo Id (Kiểm tra quyền sở hữu ca khám, chống IDOR)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        // Chống IDOR: Bệnh nhân chỉ xem lịch của mình, Bác sĩ chỉ xem lịch được phân công, Staff có quyền quản lý
        var canAccess = await _authorizationService.CanAccessAppointmentAsync(id, cancellationToken);
        if (!canAccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                ApiResponse<object>.Fail("Bạn không có quyền truy cập ca khám này.", StatusCodes.Status403Forbidden));
        }

        var appointment = await Mediator.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        if (appointment == null)
        {
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông tin ca khám.", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<AppointmentDetailDto>.Ok(appointment));
    }

    /// <summary>
    /// Lấy danh sách ca khám được phân công cho Bác sĩ đang đăng nhập (Doctor Assigned Only)
    /// </summary>
    [HttpGet("doctor/assigned")]
    [Authorize(Roles = "DOCTOR")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDoctorAssignedAppointments(CancellationToken cancellationToken)
    {
        var doctorProfileId = _currentUserService.DoctorProfileId;
        if (!doctorProfileId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                ApiResponse<object>.Fail("Không tìm thấy hồ sơ bác sĩ gắn với tài khoản này.", StatusCodes.Status403Forbidden));
        }

        var list = await Mediator.Send(new GetDoctorAssignedAppointmentsQuery(doctorProfileId.Value), cancellationToken);
        return Ok(ApiResponse<List<AppointmentDetailDto>>.Ok(list));
    }

    /// <summary>
    /// Lấy danh sách lịch khám của Bệnh nhân đang đăng nhập (Patient Own Data Only)
    /// </summary>
    [HttpGet("patient/my-appointments")]
    [Authorize(Roles = "PATIENT")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientMyAppointments(CancellationToken cancellationToken)
    {
        var patientProfileId = _currentUserService.PatientProfileId;
        if (!patientProfileId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                ApiResponse<object>.Fail("Không tìm thấy hồ sơ bệnh nhân gắn với tài khoản này.", StatusCodes.Status403Forbidden));
        }

        var list = await Mediator.Send(new GetPatientAppointmentsQuery(patientProfileId.Value), cancellationToken);
        return Ok(ApiResponse<List<AppointmentDetailDto>>.Ok(list));
    }
}
