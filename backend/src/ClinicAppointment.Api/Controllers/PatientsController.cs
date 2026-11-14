using ClinicAppointment.Application.Features.Patients.Commands;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Application.Features.Patients.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

public class PatientsController : BaseApiController
{
    /// <summary>
    /// Tạo hồ sơ bệnh nhân mới (Hỗ trợ Guest độc lập hoặc liên kết User)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy thông tin hồ sơ bệnh nhân theo Id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetPatientByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>
    /// Tra cứu hồ sơ bệnh nhân theo Mã hồ sơ (PatientCode)
    /// </summary>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await Mediator.Send(new GetPatientByCodeQuery(code));
        return HandleResult(result);
    }

    /// <summary>
    /// Tra cứu hồ sơ bệnh nhân bằng Định danh bảo mật (CCCD/Hộ chiếu/BHYT qua Keyed HMAC)
    /// </summary>
    [HttpPost("lookup-by-identifier")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupByIdentifier([FromBody] LookupPatientByIdentifierQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
