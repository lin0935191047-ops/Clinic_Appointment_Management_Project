using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Refunds.Commands.ProcessRefund;
using ClinicAppointment.Application.Features.Refunds.DTOs;
using ClinicAppointment.Application.Features.Refunds.Queries.GetRefundById;
using ClinicAppointment.Application.Features.Refunds.Queries.GetRefundsList;
using ClinicAppointment.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/reception/refunds")]
[Route("api/v1/refunds")]
[Authorize(Policy = "StaffOnly")]
public class ReceptionRefundsController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách các khoản hoàn tiền (hỗ trợ lọc theo trạng thái, mã đặt lịch, ngày và phân trang)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<RefundDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRefunds(
        [FromQuery] RefundStatus? status,
        [FromQuery] string? bookingCode,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRefundsListQuery(status, bookingCode, fromDate, toDate, page, pageSize);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem chi tiết một bản ghi hoàn tiền
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RefundDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetRefundByIdQuery(id);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xử lý cập nhật tiến độ hoặc xác nhận hoàn tất / thất bại của khoản hoàn tiền
    /// </summary>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(ApiResponse<RefundDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Process(
        [FromRoute] Guid id,
        [FromBody] ProcessRefundRequestBody body,
        CancellationToken cancellationToken)
    {
        var command = new ProcessRefundCommand(
            RefundId: id,
            TargetStatus: body.TargetStatus,
            ProviderReference: body.ProviderReference,
            FailureReason: body.FailureReason,
            Note: body.Note
        );

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public record ProcessRefundRequestBody(
    RefundStatus TargetStatus,
    string? ProviderReference = null,
    string? FailureReason = null,
    string? Note = null
);
