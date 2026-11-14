using ClinicAppointment.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [Route("/health")]
    [Route("/api/v1/health")]
    public IActionResult Check()
    {
        var healthData = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.2.0",
            service = "Clinic Appointment Management API",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        return Ok(ApiResponse<object>.Ok(healthData, "Hệ thống đang hoạt động bình thường."));
    }
}

