using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.Commands;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Application.Features.Patients.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/reception/patients")]
[Authorize(Policy = "StaffOnly")]
public class ReceptionPatientsController : BaseApiController
{
    private readonly ICurrentUserService _currentUserService;

    public ReceptionPatientsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Tìm kiếm và lọc hồ sơ bệnh nhân tại bàn tiếp nhận (phân trang theo tên, số điện thoại, mã hồ sơ, ngày sinh)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PatientDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string? keyword,
        [FromQuery] DateOnly? dateOfBirth,
        [FromQuery] short? birthYear,
        [FromQuery] bool? isPossibleDuplicate,
        [FromQuery] string? recordStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchReceptionPatientsQuery(
            Keyword: keyword,
            DateOfBirth: dateOfBirth,
            BirthYear: birthYear,
            IsPossibleDuplicate: isPossibleDuplicate,
            RecordStatus: recordStatus,
            Page: page,
            PageSize: pageSize
        );

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem chi tiết hồ sơ bệnh nhân theo Id tại bàn tiếp nhận
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPatientByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>
    /// Lễ tân tạo hồ sơ bệnh nhân mới tại quầy tiếp nhận (Tự động phát hiện nghi trùng, Strictly NO Auto-merge)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreatePatientCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Lễ tân cập nhật thông tin hồ sơ bệnh nhân tại bàn tiếp nhận (Strictly NO Auto-merge nếu đổi tên/ngày sinh trùng)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdatePatientRequestBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePatientCommand(
            PatientId: id,
            FullName: body.FullName,
            PhoneNumber: body.PhoneNumber,
            DateOfBirth: body.DateOfBirth,
            BirthYear: body.BirthYear,
            Email: body.Email,
            Gender: body.Gender,
            Address: body.Address,
            NationalId: body.NationalId,
            Passport: body.Passport,
            HealthInsurance: body.HealthInsurance
        );

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy danh sách các ca rà soát hồ sơ nghi trùng (OPEN / NEEDS_MORE_INFO) cho Lễ tân
    /// </summary>
    [HttpGet("review-cases")]
    [ProducesResponseType(typeof(ApiResponse<List<PatientIdentityReviewCaseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReviewCases()
    {
        var result = await Mediator.Send(new GetOpenReviewCasesQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Lễ tân xử lý ca nghi trùng: gộp hồ sơ có kiểm toán bất biến (CONFIRMED_SAME_PERSON) hoặc xác nhận người khác (DIFFERENT_PERSON)
    /// </summary>
    [HttpPost("review-cases/{id:guid}/resolve")]
    [ProducesResponseType(typeof(ApiResponse<PatientIdentityReviewCaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveReviewCase(
        [FromRoute] Guid id,
        [FromBody] ResolveReviewCaseRequestBody body,
        CancellationToken cancellationToken)
    {
        var staffUserId = _currentUserService.UserId ?? Guid.Empty;

        var command = new ResolvePatientReviewCaseCommand(
            CaseId: id,
            Resolution: body.Resolution,
            ReviewerUserId: staffUserId,
            Note: body.Note,
            MergeReason: body.MergeReason,
            PreferredTargetPatientId: body.PreferredTargetPatientId
        );

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public record UpdatePatientRequestBody(
    string FullName,
    string PhoneNumber,
    DateOnly? DateOfBirth = null,
    short? BirthYear = null,
    string? Email = null,
    string? Gender = null,
    string? Address = null,
    string? NationalId = null,
    string? Passport = null,
    string? HealthInsurance = null
);

public record ResolveReviewCaseRequestBody(
    ClinicAppointment.Domain.Enums.IdentityReviewStatus Resolution,
    string Note,
    string? MergeReason = null,
    Guid? PreferredTargetPatientId = null
);
