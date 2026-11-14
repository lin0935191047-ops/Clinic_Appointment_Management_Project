using ClinicAppointment.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClinicAppointment.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Kiểm tra trạng thái toàn diện của hệ thống và kết nối PostgreSQL
    /// </summary>
    [HttpGet]
    [Route("/health")]
    [Route("/api/v1/health")]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var dbEntry = report.Entries.Values.FirstOrDefault();
        var latencyMs = dbEntry.Data.TryGetValue("latencyMs", out var lat) ? lat : 0;
        var dbStatus = dbEntry.Status.ToString();

        var healthData = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            version = "1.2.0",
            service = "Clinic Appointment Management API",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            database = new
            {
                status = dbStatus,
                latencyMs,
                description = dbEntry.Description
            }
        };

        if (report.Status == HealthStatus.Unhealthy)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail(
                    "Cơ sở dữ liệu đang gặp sự cố kết nối.",
                    StatusCodes.Status503ServiceUnavailable,
                    new List<ApiError> { new ApiError("database", dbEntry.Description ?? "Database connection failed") }));
        }

        return Ok(ApiResponse<object>.Ok(healthData, "Hệ thống và cơ sở dữ liệu đang hoạt động bình thường."));
    }

    /// <summary>
    /// Liveness probe: Kiểm tra tiến trình Web API đang chạy
    /// </summary>
    [HttpGet]
    [Route("/health/live")]
    [Route("/api/v1/health/live")]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness probe: Kiểm tra DB sẵn sàng nhận request
    /// </summary>
    [HttpGet]
    [Route("/health/ready")]
    [Route("/api/v1/health/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        if (report.Status == HealthStatus.Unhealthy)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
