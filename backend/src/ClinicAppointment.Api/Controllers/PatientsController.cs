using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.Commands;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Application.Features.Patients.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

public class PatientsController : BaseApiController
{
    private readonly IResourceAuthorizationService _authorizationService;

    public PatientsController(IResourceAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Tạo hồ sơ bệnh nhân mới (Hỗ trợ Guest độc lập hoặc liên kết User)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy thông tin hồ sơ bệnh nhân theo Id (Kiểm tra quyền sở hữu, chống IDOR)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Chống IDOR: Bệnh nhân chỉ được xem hồ sơ của chính mình; Nhân viên y tế có quyền xem
        var canAccess = await _authorizationService.CanAccessPatientAsync(id);
        if (!canAccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                ApiResponse<object>.Fail("Bạn không có quyền truy cập thông tin hồ sơ bệnh nhân này.", StatusCodes.Status403Forbidden));
        }

        var result = await Mediator.Send(new GetPatientByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>
    /// Tra cứu hồ sơ bệnh nhân theo Mã hồ sơ (PatientCode) - Dành riêng cho nhân viên y tế
    /// </summary>
    [HttpGet("code/{code}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await Mediator.Send(new GetPatientByCodeQuery(code));
        return HandleResult(result);
    }

    /// <summary>
    /// Tra cứu hồ sơ bệnh nhân bằng Định danh bảo mật (CCCD/Hộ chiếu/BHYT) - Dành cho nhân viên y tế
    /// </summary>
    [HttpPost("lookup-by-identifier")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupByIdentifier([FromBody] LookupPatientByIdentifierQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy danh sách các ca rà soát hồ sơ nghi trùng (Dành riêng cho Lễ tân / Quản trị viên)
    /// </summary>
    [HttpGet("review-cases")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(List<PatientIdentityReviewCaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReviewCases()
    {
        var result = await Mediator.Send(new GetOpenReviewCasesQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Xử lý ca rà soát nghi trùng: gộp hồ sơ có kiểm toán hoặc xác nhận người khác biệt (Dành riêng cho Staff)
    /// </summary>
    [HttpPost("review-cases/{id:guid}/resolve")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(PatientIdentityReviewCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveReviewCase(Guid id, [FromBody] ResolvePatientReviewCaseCommand command)
    {
        if (id != command.CaseId)
        {
            return BadRequest("Id trên đường dẫn không khớp với dữ liệu yêu cầu.");
        }

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
