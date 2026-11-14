using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Otp.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/appointment-access")]
public class AppointmentAccessController : BaseApiController
{
    private readonly IOtpChallengeService _otpService;
    private readonly IOtpGrantService _grantService;

    public AppointmentAccessController(
        IOtpChallengeService otpService,
        IOtpGrantService grantService)
    {
        _otpService = otpService;
        _grantService = grantService;
    }

    /// <summary>
    /// Yêu cầu gửi mã xác thực OTP phục vụ tra cứu, đổi lịch hoặc hủy lịch (Guest Booking)
    /// </summary>
    [HttpPost("otp/request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RequestOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] RequestOtpRequest request, 
        CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _otpService.RequestOtpAsync(
            request.BookingCode, 
            request.PhoneNumber, 
            request.Purpose, 
            clientIp, 
            cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, 
                ApiResponse<object>.Fail(result.Message, StatusCodes.Status429TooManyRequests));
        }

        var response = new RequestOtpResponse(
            true, 
            result.Message, 
            result.ChallengeId, 
            result.CooldownSeconds);

        return Ok(ApiResponse<RequestOtpResponse>.Ok(response, result.Message));
    }

    /// <summary>
    /// Xác thực mã OTP và cấp phát Scoped Access Grant tạm thời (Not a User JWT)
    /// </summary>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request, 
        CancellationToken cancellationToken)
    {
        var verifyResult = await _otpService.VerifyOtpAsync(
            request.ChallengeId, 
            request.Otp, 
            request.Purpose, 
            cancellationToken);

        if (!verifyResult.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(
                verifyResult.ErrorMessage ?? "Mã OTP không hợp lệ.", 
                StatusCodes.Status400BadRequest));
        }

        // Cấp phát Scoped Access Grant có thời hạn 15 phút gắn chặt cuộc hẹn và mục đích (FEAT-026)
        var grantResult = await _grantService.IssueGrantAsync(
            request.ChallengeId, 
            TimeSpan.FromMinutes(15), 
            cancellationToken);

        if (!grantResult.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(
                grantResult.ErrorMessage ?? "Không thể cấp quyền truy cập tạm thời.", 
                StatusCodes.Status400BadRequest));
        }

        var response = new VerifyOtpResponse(
            true, 
            "Xác thực mã OTP thành công. Quyền truy cập tạm thời đã được cấp phát.", 
            verifyResult.ChallengeId, 
            verifyResult.AppointmentId, 
            verifyResult.Purpose,
            grantResult.GrantToken,
            grantResult.ExpiresAt,
            "ScopedAppointmentGrant");

        return Ok(ApiResponse<VerifyOtpResponse>.Ok(response, "Xác thực mã OTP thành công."));
    }
}
