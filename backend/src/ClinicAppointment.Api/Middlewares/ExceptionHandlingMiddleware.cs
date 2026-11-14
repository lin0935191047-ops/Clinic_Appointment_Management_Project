using System.Net;
using System.Text.Json;
using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ILogRedactor _redactor;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        ILogRedactor redactor)
    {
        _next = next;
        _logger = logger;
        _redactor = redactor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var sanitizedMessage = _redactor.Redact(ex.Message);
            _logger.LogError(ex, "Đã xảy ra ngoại lệ trong quá trình thực thi: {SanitizedMessage}", sanitizedMessage);
            await HandleExceptionAsync(context, ex, _redactor);
        }
    }

    public static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ILogRedactor? redactor = null)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => ApiResponse<object>.Fail(
                "Dữ liệu gửi lên không hợp lệ.", 
                (int)HttpStatusCode.BadRequest,
                validationEx.Errors.Select(e => new ApiError(e.PropertyName, e.ErrorMessage)).ToList()),

            AppointmentConflictException conflictEx => ApiResponse<object>.Fail(
                conflictEx.Message,
                (int)HttpStatusCode.Conflict),

            IdempotencyConflictException idempEx => ApiResponse<object>.Fail(
                idempEx.Message,
                (int)HttpStatusCode.Conflict),

            RateLimitExceededException rateLimitEx => ApiResponse<object>.Fail(
                rateLimitEx.Message,
                429),

            DbUpdateException dbUpdateEx when IsExclusionOrUniqueConstraintViolation(dbUpdateEx) => ApiResponse<object>.Fail(
                "Khung giờ khám này vừa có người khác đặt trước. Vui lòng chọn khung giờ khác.",
                (int)HttpStatusCode.Conflict),

            KeyNotFoundException notFoundEx => ApiResponse<object>.Fail(
                notFoundEx.Message,
                (int)HttpStatusCode.NotFound),

            UnauthorizedAccessException unauthEx => ApiResponse<object>.Fail(
                unauthEx.Message,
                (int)HttpStatusCode.Forbidden),

            ArgumentException argEx => ApiResponse<object>.Fail(
                argEx.Message, 
                (int)HttpStatusCode.BadRequest),

            InvalidOperationException invEx => ApiResponse<object>.Fail(
                invEx.Message, 
                (int)HttpStatusCode.BadRequest),

            _ => ApiResponse<object>.Fail(
                "Đã xảy ra lỗi nội bộ trên máy chủ. Vui lòng thử lại sau.", 
                (int)HttpStatusCode.InternalServerError)
        };

        // Đảm bảo Correlation ID có trong response header
        if (context.Items.TryGetValue(CorrelationAndLoggingMiddleware.CorrelationIdItemKey, out var corrId) &&
            corrId is string correlationIdString &&
            !context.Response.Headers.ContainsKey(CorrelationAndLoggingMiddleware.CorrelationIdHeader))
        {
            context.Response.Headers[CorrelationAndLoggingMiddleware.CorrelationIdHeader] = correlationIdString;
        }

        context.Response.StatusCode = response.StatusCode;
        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    public static bool IsExclusionOrUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (message.Contains("ex_appointments_doctor_no_overlap", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("23P01", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("exclusion_violation", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("ix_appointments_doctor_id_start_at", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
