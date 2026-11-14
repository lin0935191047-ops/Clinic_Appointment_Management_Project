using System.Net;
using System.Text.Json;
using ClinicAppointment.Application.Common.Models;
using FluentValidation;

namespace ClinicAppointment.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã xảy ra lỗi không xử lý được trong quá trình thực thi: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => ApiResponse<object>.Fail(
                "Dữ liệu gửi lên không hợp lệ.", 
                (int)HttpStatusCode.BadRequest,
                validationEx.Errors.Select(e => new ApiError(e.PropertyName, e.ErrorMessage)).ToList()),

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

        context.Response.StatusCode = response.StatusCode;
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

