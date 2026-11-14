using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Reporting.DTOs;
using ClinicAppointment.Application.Features.Reporting.Queries.GetAdminDashboardReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/admin/reports")]
[Authorize(Policy = "AdminOnly")]
public class AdminReportsController : BaseApiController
{
    /// <summary>
    /// Báo cáo thống kê tổng quan ca khám và tài chính dành cho Quản trị viên (Admin Report)
    /// Hỗ trợ lọc theo ngày, trạng thái, bác sĩ, chuyên khoa.
    /// </summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAppointmentsReport(
        [FromQuery] GetAdminDashboardReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Dashboard số liệu thống kê quản trị (Admin Dashboard)
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardReport(
        [FromQuery] GetAdminDashboardReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
