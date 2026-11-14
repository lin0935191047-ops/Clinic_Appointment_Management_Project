using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Reporting.DTOs;
using ClinicAppointment.Application.Features.Reporting.Queries.GetDoctorDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

/// <summary>
/// API điều khiển Dashboard và thống kê ca khám dành cho Bác sĩ (Doctor Dashboard)
/// </summary>
[Route("api/v1/doctor/dashboard")]
[Authorize(Policy = "StaffOrDoctor")]
public class DoctorDashboardController : BaseApiController
{
    /// <summary>
    /// Lấy dữ liệu tổng quan Dashboard và lịch khám hôm nay của Bác sĩ (Today Schedule & Assigned Scope)
    /// </summary>
    /// <param name="date">Ngày khám cần xem (mặc định là ngày hôm nay theo múi giờ Việt Nam)</param>
    /// <param name="doctorId">Mã Bác sĩ (chỉ áp dụng đối với Quản trị viên muốn xem dashboard của bác sĩ cụ thể)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DoctorDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? doctorId,
        CancellationToken cancellationToken)
    {
        var query = new GetDoctorDashboardQuery(Date: date, DoctorId: doctorId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
