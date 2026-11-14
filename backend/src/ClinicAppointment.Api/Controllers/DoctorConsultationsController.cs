using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Consultations.Commands.CompleteConsultation;
using ClinicAppointment.Application.Features.Consultations.Commands.RecordConsultation;
using ClinicAppointment.Application.Features.Consultations.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

public record CreateConsultationRequest(
    Guid AppointmentId,
    string Symptoms,
    string Diagnosis,
    string? ExaminationNotes = null,
    string? Conclusion = null,
    string? FollowUpInstructions = null
);

[Route("api/v1/doctor/consultations")]
[Authorize(Policy = "StaffOrDoctor")]
public class DoctorConsultationsController : BaseApiController
{
    /// <summary>
    /// Bác sĩ ghi chép kết quả thăm khám y khoa (Consultation record)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateConsultation(
        [FromBody] CreateConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordConsultationCommand(
            AppointmentId: request.AppointmentId,
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
    /// Bác sĩ hoàn tất ca khám y khoa
    /// </summary>
    [HttpPost("{appointmentId:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<CompleteConsultationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteConsultation(
        [FromRoute] Guid appointmentId,
        [FromBody] CompleteConsultationRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteConsultationCommand(
            AppointmentId: appointmentId,
            Conclusion: request?.Conclusion,
            FollowUpInstructions: request?.FollowUpInstructions
        );
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
