using ClinicAppointment.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return StatusCode(result.StatusCode, ApiResponse<T>.Ok(result.Value!));
        }

        return StatusCode(result.StatusCode, ApiResponse<T>.Fail(result.Error, result.StatusCode));
    }
}

