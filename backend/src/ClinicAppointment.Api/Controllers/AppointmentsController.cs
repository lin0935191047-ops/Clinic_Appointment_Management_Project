using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;
using ClinicAppointment.Application.Features.Appointments.DTOs;
using ClinicAppointment.Application.Features.Appointments.Queries;
using ClinicAppointment.Application.Features.Notifications.Queries;
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
    [ProducesResponseType(typeof(ApiResponse<BookGuestAppointmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> BookGuest([FromBody] BookGuestAppointmentCommand command, CancellationToken cancellationToken)
    {
        // 1. Nhận diện Idempotency-Key từ Request Header nếu client không truyền trong body
        string? idempotencyKey = command.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (Request.Headers.TryGetValue("Idempotency-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey))
            {
                idempotencyKey = headerKey.ToString();
            }
            else if (Request.Headers.TryGetValue("X-Idempotency-Key", out var xHeaderKey) && !string.IsNullOrWhiteSpace(xHeaderKey))
            {
                idempotencyKey = xHeaderKey.ToString();
            }
        }

        // 2. Thu thập Client IP để phòng chống lạm dụng (Abuse Protection)
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var commandWithSecurity = command with 
        { 
            IdempotencyKey = idempotencyKey, 
            ClientIp = clientIp 
        };

        var result = await Mediator.Send(commandWithSecurity, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Tra cứu tóm tắt ca khám công khai (Yêu cầu Grant token từ xác thực OTP, không cho phép tra cứu chỉ bằng mã booking)
    /// </summary>
    [HttpPost("lookup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment.PublicAppointmentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupAppointment(
        [FromBody] ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment.LookupAppointmentRequest request, 
        CancellationToken cancellationToken)
    {
        // 1. Hỗ trợ nhận Grant token từ header X-Appointment-Grant hoặc Authorization: Grant <token>
        string? grantToken = request.GrantToken;
        if (string.IsNullOrWhiteSpace(grantToken))
        {
            if (Request.Headers.TryGetValue("X-Appointment-Grant", out var headerGrant) && !string.IsNullOrWhiteSpace(headerGrant))
            {
                grantToken = headerGrant.ToString();
            }
            else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("Grant ", StringComparison.OrdinalIgnoreCase))
                {
                    grantToken = authStr.Substring(6).Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(grantToken))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "Yêu cầu xác thực OTP để tra cứu thông tin ca khám. Vui lòng cung cấp mã quyền truy cập (Grant token).", 
                StatusCodes.Status401Unauthorized));
        }

        var query = new ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment.LookupAppointmentQuery(
            request.BookingCode, 
            grantToken);

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gửi yêu cầu đổi lịch khám (Yêu cầu Grant token từ OTP với mục đích RESCHEDULE, lịch cũ giữ nguyên chờ duyệt)
    /// </summary>
    [HttpPost("{bookingCode}/reschedule-requests")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest.SubmitRescheduleRequestResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitRescheduleRequest(
        [FromRoute] string bookingCode,
        [FromBody] ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest.SubmitRescheduleRequestBody request,
        CancellationToken cancellationToken)
    {
        // Hỗ trợ nhận Grant token từ header X-Appointment-Grant hoặc Authorization: Grant <token> nếu không truyền trong body
        string? grantToken = request.GrantToken;
        if (string.IsNullOrWhiteSpace(grantToken))
        {
            if (Request.Headers.TryGetValue("X-Appointment-Grant", out var headerGrant) && !string.IsNullOrWhiteSpace(headerGrant))
            {
                grantToken = headerGrant.ToString();
            }
            else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("Grant ", StringComparison.OrdinalIgnoreCase))
                {
                    grantToken = authStr.Substring(6).Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(grantToken))
        {
            return Unauthorized(ApiResponse<object>.Fail(
                "Yêu cầu xác thực OTP để gửi yêu cầu đổi lịch. Vui lòng cung cấp mã quyền truy cập (Grant token).", 
                StatusCodes.Status401Unauthorized));
        }

        var command = new ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest.SubmitRescheduleRequestCommand(
            bookingCode,
            request.ProposedStartAtUtc,
            grantToken,
            request.RequestedDoctorId,
            request.Reason);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem trước chính sách hủy và số tiền hoàn dự kiến theo Server Clock (FEAT-031)
    /// </summary>
    [HttpPost("{bookingCode}/cancellation-preview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ClinicAppointment.Application.Features.Appointments.Queries.PreviewCancellationPolicy.CancellationPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewCancellationPolicy(
        [FromRoute] string bookingCode,
        [FromBody] ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment.LookupAppointmentRequest? body,
        CancellationToken cancellationToken)
    {
        string? grantToken = body?.GrantToken;
        if (string.IsNullOrWhiteSpace(grantToken))
        {
            if (Request.Headers.TryGetValue("X-Appointment-Grant", out var headerGrant) && !string.IsNullOrWhiteSpace(headerGrant))
            {
                grantToken = headerGrant.ToString();
            }
            else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("Grant ", StringComparison.OrdinalIgnoreCase))
                {
                    grantToken = authStr.Substring(6).Trim();
                }
            }
        }

        var query = new ClinicAppointment.Application.Features.Appointments.Queries.PreviewCancellationPolicy.PreviewCancellationPolicyQuery(
            bookingCode,
            grantToken);

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bệnh nhân / Guest hủy lịch khám (Yêu cầu Grant token từ xác thực OTP mục đích CANCEL, không cần Lễ tân duyệt)
    /// </summary>
    [HttpPost("{bookingCode}/cancel")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment.CancelAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAppointment(
        [FromRoute] string bookingCode,
        [FromBody] ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment.CancelAppointmentBody? body,
        CancellationToken cancellationToken)
    {
        string? grantToken = body?.GrantToken;
        if (string.IsNullOrWhiteSpace(grantToken))
        {
            if (Request.Headers.TryGetValue("X-Appointment-Grant", out var headerGrant) && !string.IsNullOrWhiteSpace(headerGrant))
            {
                grantToken = headerGrant.ToString();
            }
            else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("Grant ", StringComparison.OrdinalIgnoreCase))
                {
                    grantToken = authStr.Substring(6).Trim();
                }
            }
        }

        var command = new ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment.CancelAppointmentCommand(
            bookingCode,
            body?.Reason,
            grantToken);

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

    /// <summary>
    /// Nhân viên Lễ tân xác nhận ca khám từ PendingConfirmation sang Confirmed
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(ApiResponse<ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment.ConfirmAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAppointment(
        [FromRoute] Guid id,
        [FromBody] ConfirmAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new ClinicAppointment.Application.Features.Appointments.Commands.ConfirmAppointment.ConfirmAppointmentCommand(id, request?.Note);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Nhân viên Lễ tân tiếp nhận và check-in ca khám từ Confirmed sang CheckedIn
    /// </summary>
    [HttpPost("{id:guid}/check-in")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(ApiResponse<ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment.CheckInAppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckInAppointment(
        [FromRoute] Guid id,
        [FromBody] CheckInAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new ClinicAppointment.Application.Features.Appointments.Commands.CheckInAppointment.CheckInAppointmentCommand(id, request?.Note);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Tra cứu lịch sử và trạng thái gửi thông báo của ca khám (Delivery status)
    /// </summary>
    [HttpGet("{id:guid}/notifications")]
    [Authorize(Policy = "StaffOrDoctor")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointmentNotifications(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetAppointmentNotificationsQuery(id);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
