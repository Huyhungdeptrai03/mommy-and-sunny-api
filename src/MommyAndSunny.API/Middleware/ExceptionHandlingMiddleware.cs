using MommyAndSunny.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace MommyAndSunny.API.Middleware
{
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, exception.Message),
                ConflictException => (HttpStatusCode.Conflict, exception.Message),
                UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message),
                InsufficientStockException => (HttpStatusCode.Conflict, exception.Message),
                DomainException => (HttpStatusCode.BadRequest, exception.Message),
                _ => (HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra, vui lòng thử lại sau")
            };

            // Chỉ log chi tiết (kể cả stack trace) với lỗi hệ thống ngoài dự kiến —
            // lỗi nghiệp vụ (DomainException) là hành vi bình thường, không cần log như lỗi
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Lỗi hệ thống ngoài dự kiến");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                data = (object?)null,
                message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
