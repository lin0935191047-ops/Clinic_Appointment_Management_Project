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

    public AppointmentAccessController(IOtpChallengeService otpService)
    {
        _otpService = otpService;
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
    /// Xác thực mã OTP gửi về điện thoại
    /// </summary>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _otpService.VerifyOtpAsync(
            request.ChallengeId, 
            request.Otp, 
            request.Purpose, 
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorMessage ?? "Mã OTP không hợp lệ.", 
                StatusCodes.Status400BadRequest));
        }

        var response = new VerifyOtpResponse(
            true, 
            "Xác thực mã OTP thành công.", 
            result.ChallengeId, 
            result.AppointmentId, 
            result.Purpose);

        return Ok(ApiResponse<VerifyOtpResponse>.Ok(response, "Xác thực mã OTP thành công."));
    }
}
