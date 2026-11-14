using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Queries.GetDoctorAppointments;
using ClinicAppointment.Application.Features.Consultations.Commands.CompleteConsultation;
using ClinicAppointment.Application.Features.Consultations.Commands.RecordConsultation;
using ClinicAppointment.Application.Features.Consultations.Commands.StartConsultation;
using ClinicAppointment.Application.Features.Consultations.DTOs;
using ClinicAppointment.Application.Features.Consultations.Queries.GetConsultationByAppointmentId;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[Route("api/v1/doctor/appointments")]
[Authorize(Policy = "StaffOrDoctor")]
public class DoctorAppointmentsController : BaseApiController
{
    /// <summary>
    /// Bác sĩ xem danh sách các ca khám được phân công cho mình (Assigned doctor appointments)
    /// Hỗ trợ lọc theo ngày khám, trạng thái (CheckedIn, InProgress, Confirmed...), và phân trang.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<DoctorAssignedAppointmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAssignedAppointments(
        [FromQuery] GetDoctorAppointmentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bác sĩ bắt đầu ca khám (chuyển CheckedIn -> InProgress)
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(ApiResponse<StartConsultationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartConsultation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new StartConsultationCommand(id);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bác sĩ ghi chép thông tin chẩn đoán và thăm khám (Consultation record)
    /// </summary>
    [HttpPost("{id:guid}/consultation")]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordConsultation(
        [FromRoute] Guid id,
        [FromBody] RecordConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordConsultationCommand(
            AppointmentId: id,
            Symptoms: request.Symptoms,
            Diagnosis: request.Diagnosis,
            ExaminationNotes: request.ExaminationNotes,
            Conclusion: request.Conclusion,
            FollowUpInstructions: request.FollowUpInstructions
        );
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bác sĩ xem nội dung thăm khám của ca khám được phân công
    /// </summary>
    [HttpGet("{id:guid}/consultation")]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsultation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetConsultationByAppointmentIdQuery(id);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bác sĩ hoàn tất ca khám (chuyển InProgress -> Completed, khóa ca khám và cập nhật kết luận/hướng dẫn)
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<CompleteConsultationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteConsultation(
        [FromRoute] Guid id,
        [FromBody] CompleteConsultationRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteConsultationCommand(
            AppointmentId: id,
            Conclusion: request?.Conclusion,
            FollowUpInstructions: request?.FollowUpInstructions
        );
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
