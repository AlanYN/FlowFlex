using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Infrastructure.Services.Logging;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Infrastructure.Exceptions
{
    /// <summary>
    /// Global exception handling middleware
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetRequiredService<IApplicationLogger>();
                await HandleExceptionAsync(context, ex, logger);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, IApplicationLogger logger)
        {
            var errorResponse = exception switch
            {
                CRMException crmEx => CreateCRMErrorResponse(crmEx),
                ArgumentNullException argEx => CreateArgumentErrorResponse(argEx),
                ArgumentException argEx => CreateArgumentErrorResponse(argEx),
                UnauthorizedAccessException => CreateUnauthorizedErrorResponse(),
                TimeoutException => CreateTimeoutErrorResponse(),
                _ => CreateGenericErrorResponse(exception)
            };

            // Log exception
            var errorId = Guid.NewGuid().ToString("N")[..8];
            logger.LogError(exception, "Unhandled exception occurred. ErrorId: {ErrorId}, Path: {Path}, Method: {Method}",
                errorId, context.Request.Path, context.Request.Method);

            // Add error ID to response
            errorResponse.Data = new { ErrorId = errorId };

            context.Response.StatusCode = errorResponse.Code;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }

        private ApiResponse<object> CreateCRMErrorResponse(CRMException crmException)
        {
            var statusCode = crmException.StatusCode?.ToString() ?? "400";
            return new ApiResponse<object>
            {
                Code = int.Parse(statusCode),
                Msg = crmException.Message, // 改为Msg字段
                Data = crmException.ErrorData
            };
        }

        private ApiResponse<object> CreateArgumentErrorResponse(Exception exception)
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.BadRequest,
                Msg = "Invalid request parameters", // 改为Msg字段
                Data = new { Details = exception.Message }
            };
        }

        private ApiResponse<object> CreateUnauthorizedErrorResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.Unauthorized,
                Msg = "Unauthorized access", // 改为Msg字段
                Data = null
            };
        }

        private ApiResponse<object> CreateTimeoutErrorResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.RequestTimeout,
                Msg = "Request timeout", // 改为Msg字段
                Data = null
            };
        }

        private ApiResponse<object> CreateGenericErrorResponse(Exception exception)
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.InternalServerError,
                Msg = "An internal server error occurred", // 改为Msg字段
                Data = null
            };
        }
    }
}