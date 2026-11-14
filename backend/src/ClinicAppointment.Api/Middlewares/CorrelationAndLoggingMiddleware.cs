using System.Diagnostics;
using ClinicAppointment.Application.Common.Interfaces;

namespace ClinicAppointment.Api.Middlewares;

/// <summary>
/// Middleware quản lý Correlation ID và Structured Request/Response Logging
/// - Bảo đảm mọi request đều có một Correlation ID (tối đa 80 ký tự theo database schema)
/// - Trả Correlation ID về client qua header 'X-Correlation-ID'
/// - Đưa Correlation ID vào logging scope để truy vết xuyên suốt pipeline
/// - Ghi log request/response có bảo vệ che giấu PII (Redacted QueryString/Path)
/// </summary>
public class CorrelationAndLoggingMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";
    private const int MaxCorrelationIdLength = 80;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationAndLoggingMiddleware> _logger;
    private readonly ILogRedactor _redactor;

    public CorrelationAndLoggingMiddleware(
        RequestDelegate next,
        ILogger<CorrelationAndLoggingMiddleware> logger,
        ILogRedactor redactor)
    {
        _next = next;
        _logger = logger;
        _redactor = redactor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Trích xuất hoặc sinh mới Correlation ID
        var correlationId = ExtractOrGenerateCorrelationId(context);

        // 2. Gán vào HttpContext items & Response headers
        context.Items[CorrelationIdItemKey] = correlationId;
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        // 3. Mở Logging Scope chứa CorrelationId cho toàn bộ request pipeline
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = context.TraceIdentifier,
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        }))
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;

            // Redact query string nếu có chứa tham số nhạy cảm (như sđt, cccd, otp)
            var rawQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            var redactedQuery = _redactor.Redact(rawQuery);

            _logger.LogInformation("Bắt đầu xử lý HTTP Request: {Method} {Path}{Query}", method, path, redactedQuery);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation(
                    "Hoàn tất HTTP Request: {Method} {Path} -> {StatusCode} ({ElapsedMs}ms)",
                    method,
                    path,
                    statusCode,
                    elapsedMs);
            }
        }
    }

    private static string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValues) &&
            !string.IsNullOrWhiteSpace(headerValues.FirstOrDefault()))
        {
            var candidate = headerValues.FirstOrDefault()!.Trim();
            if (candidate.Length <= MaxCorrelationIdLength)
            {
                return candidate;
            }
            return candidate[..MaxCorrelationIdLength];
        }

        return Guid.NewGuid().ToString("N");
    }
}
