using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Audit.DTOs;
using ClinicAppointment.Application.Features.Audit.Queries.GetAppointmentCancellationAudit;
using ClinicAppointment.Application.Features.Audit.Queries.GetCancellationAuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/reception/cancellation-audit")]
[Route("api/v1/audit/cancellations")]
[Authorize(Policy = "StaffOnly")]
public class CancellationAuditController : BaseApiController
{
    /// <summary>
    /// Lấy danh sách nhật ký kiểm toán hủy lịch và xử lý hoàn tiền (có phân trang và lọc)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<CancellationAuditDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? bookingCode,
        [FromQuery] string? actorType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCancellationAuditLogsQuery(bookingCode, actorType, fromDate, toDate, page, pageSize);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem toàn bộ lịch sử kiểm toán (Audit Trail) hủy lịch và quyết định hoàn tiền của một ca khám
    /// </summary>
    [HttpGet("{bookingCode}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentCancellationAuditHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointmentAuditTrail(
        [FromRoute] string bookingCode,
        CancellationToken cancellationToken)
    {
        var query = new GetAppointmentCancellationAuditQuery(bookingCode);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
