using System.Security.Claims;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Auth.Commands;
using ClinicAppointment.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Đăng ký tài khoản Bệnh nhân tùy chọn (Public Patient Optional)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegisterPatientCommand(request), cancellationToken);
        var response = ApiResponse<AuthResponseDto>.Ok(result, "Đăng ký tài khoản thành công.");
        response.StatusCode = StatusCodes.Status201Created;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Đăng nhập tài khoản (Email + Password) cấp phát JWT Access Token & Refresh Token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request), cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng nhập thành công."));
    }

    /// <summary>
    /// Cấp mới Access Token bằng Refresh Token (Refresh Token Rotation)
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request), cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Làm mới phiên làm việc thành công."));
    }

    /// <summary>
    /// Đăng xuất - Thu hồi Refresh Token
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous] // Cho phép gửi token trong body hoặc từ header
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Đăng xuất thành công."));
    }

    /// <summary>
    /// Đổi mật khẩu cho người dùng hiện tại
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy định danh người dùng từ token.", 401));

        var result = await _mediator.Send(new ChangePasswordCommand(userId, request), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Đổi mật khẩu thành công."));
    }

    /// <summary>
    /// Yêu cầu mã đặt lại mật khẩu khi quên
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request), cancellationToken);
        return Ok(ApiResponse<string>.Ok(result, "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được xử lý."));
    }

    /// <summary>
    /// Đặt lại mật khẩu bằng mã xác thực hợp lệ
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, "Đặt lại mật khẩu thành công."));
    }

    /// <summary>
    /// Lấy thông tin tài khoản người dùng đang đăng nhập
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy định danh người dùng từ token.", 401));

        var result = await _mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return Ok(ApiResponse<UserSummaryDto>.Ok(result));
    }
}
