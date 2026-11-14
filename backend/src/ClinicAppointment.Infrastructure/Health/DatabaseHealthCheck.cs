using System.Diagnostics;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClinicAppointment.Infrastructure.Health;

/// <summary>
/// Health check kiểm tra kết nối tới cơ sở dữ liệu PostgreSQL thực tế
/// - Xác nhận DB online và sẵn sàng phục vụ
/// - Đo lường độ trễ (latency ms)
/// - Trả về Unhealthy khi DB mất kết nối hoặc timeout
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogRedactor _redactor;

    public DatabaseHealthCheck(ApplicationDbContext dbContext, ILogRedactor redactor)
    {
        _dbContext = dbContext;
        _redactor = redactor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Kiểm tra kết nối với timeout giới hạn
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);
            stopwatch.Stop();

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Không thể thiết lập kết nối tới cơ sở dữ liệu PostgreSQL.",
                    data: new Dictionary<string, object>
                    {
                        ["database"] = "PostgreSQL",
                        ["provider"] = _dbContext.Database.ProviderName ?? "Npgsql",
                        ["latencyMs"] = stopwatch.ElapsedMilliseconds,
                        ["status"] = "Disconnected"
                    });
            }

            return HealthCheckResult.Healthy(
                $"Kết nối cơ sở dữ liệu PostgreSQL hoạt động tốt ({stopwatch.ElapsedMilliseconds}ms).",
                data: new Dictionary<string, object>
                {
                    ["database"] = "PostgreSQL",
                    ["provider"] = _dbContext.Database.ProviderName ?? "Npgsql",
                    ["latencyMs"] = stopwatch.ElapsedMilliseconds,
                    ["status"] = "Connected"
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var sanitizedMessage = _redactor.Redact(ex.Message);
            return HealthCheckResult.Unhealthy(
                $"Lỗi kết nối cơ sở dữ liệu PostgreSQL: {sanitizedMessage}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["database"] = "PostgreSQL",
                    ["provider"] = _dbContext.Database.ProviderName ?? "Npgsql",
                    ["latencyMs"] = stopwatch.ElapsedMilliseconds,
                    ["status"] = "Error"
                });
        }
    }
}
