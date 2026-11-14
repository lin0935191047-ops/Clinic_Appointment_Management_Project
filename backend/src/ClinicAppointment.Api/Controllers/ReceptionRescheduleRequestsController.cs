using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/reception/reschedule-requests")]
[Authorize(Policy = "StaffOnly")]
public class ReceptionRescheduleRequestsController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách hàng đợi các yêu cầu đổi lịch đang chờ duyệt (PendingReview)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RescheduleRequestDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPendingRescheduleRequestsQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lễ tân phê duyệt yêu cầu đổi lịch (Kiểm tra lại slot trong transaction và cập nhật ca khám sang khung giờ mới)
    /// </summary>
    [HttpPost("{requestId:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<ReviewRescheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveRequest(
        [FromRoute] Guid requestId,
        [FromBody] ApproveRescheduleRequestBody? body,
        CancellationToken cancellationToken)
    {
        var command = new ApproveRescheduleRequestCommand(requestId, body?.ReviewNotes);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lễ tân từ chối yêu cầu đổi lịch kèm lý do (Lịch hẹn ban đầu của bệnh nhân tiếp tục giữ nguyên)
    /// </summary>
    [HttpPost("{requestId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<ReviewRescheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRequest(
        [FromRoute] Guid requestId,
        [FromBody] RejectRescheduleRequestBody body,
        CancellationToken cancellationToken)
    {
        var command = new RejectRescheduleRequestCommand(requestId, body.ReviewNotes);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
