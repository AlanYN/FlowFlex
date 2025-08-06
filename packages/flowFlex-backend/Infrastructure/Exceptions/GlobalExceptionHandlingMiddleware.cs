using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Infrastructure.Services.Logging;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Npgsql;

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
                PostgresException pgEx => CreateDatabaseErrorResponse(pgEx),
                InvalidOperationException invOpEx => CreateInvalidOperationErrorResponse(invOpEx),
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
            var statusCode = crmException.StatusCode.HasValue ? (int)crmException.StatusCode.Value : 400;
            return new ApiResponse<object>
            {
                Code = statusCode,
                Message = crmException.Message, // 设置Message字段
                Msg = crmException.Message, // 同时设置Msg字段以保持兼容性
                Data = crmException.ErrorData
            };
        }

        private ApiResponse<object> CreateArgumentErrorResponse(Exception exception)
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.BadRequest,
                Message = "Invalid request parameters", // 设置Message字段
                Msg = "Invalid request parameters", // 同时设置Msg字段以保持兼容性
                Data = new { Details = exception.Message }
            };
        }

        private ApiResponse<object> CreateUnauthorizedErrorResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.Unauthorized,
                Message = "Unauthorized access", // 设置Message字段
                Msg = "Unauthorized access", // 同时设置Msg字段以保持兼容性
                Data = null
            };
        }

        private ApiResponse<object> CreateTimeoutErrorResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.RequestTimeout,
                Message = "Request timeout", // 设置Message字段
                Msg = "Request timeout", // 同时设置Msg字段以保持兼容性
                Data = null
            };
        }

        private ApiResponse<object> CreateDatabaseErrorResponse(PostgresException pgException)
        {
            var (statusCode, message) = pgException.SqlState switch
            {
                "23505" => (HttpStatusCode.Conflict, GetUniqueConstraintErrorMessage(pgException)),
                "23503" => (HttpStatusCode.BadRequest, "Foreign key constraint violation"),
                "23502" => (HttpStatusCode.BadRequest, "Not null constraint violation"),
                "23514" => (HttpStatusCode.BadRequest, "Check constraint violation"),
                "42P01" => (HttpStatusCode.InternalServerError, "Table does not exist"),
                "42703" => (HttpStatusCode.InternalServerError, "Column does not exist"),
                "08003" => (HttpStatusCode.ServiceUnavailable, "Database connection error"),
                "08006" => (HttpStatusCode.ServiceUnavailable, "Database connection failure"),
                _ => (HttpStatusCode.InternalServerError, "Database operation failed")
            };

            return new ApiResponse<object>
            {
                Code = (int)statusCode,
                Message = message,
                Msg = message,
                Data = new { 
                    SqlState = pgException.SqlState,
                    ConstraintName = pgException.ConstraintName,
                    TableName = pgException.TableName
                }
            };
        }

        private string GetUniqueConstraintErrorMessage(PostgresException pgException)
        {
            return pgException.ConstraintName switch
            {
                "idx_users_email_tenant" => "User already exists in this tenant. Please use a different email address or switch to the correct tenant.",
                "idx_users_email" => "User with this email already exists.",
                _ => "A record with these values already exists. Please check for duplicates."
            };
        }

        private ApiResponse<object> CreateInvalidOperationErrorResponse(InvalidOperationException invalidOpException)
        {
            // Check if it's a file-related error
            if (invalidOpException.Message.Contains("Unsupported file type"))
            {
                return new ApiResponse<object>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Message = invalidOpException.Message,
                    Msg = invalidOpException.Message,
                    Data = new { ErrorType = "UnsupportedFileType" }
                };
            }

            // Check if it's a file size error
            if (invalidOpException.Message.Contains("file size") || invalidOpException.Message.Contains("too large"))
            {
                return new ApiResponse<object>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Message = invalidOpException.Message,
                    Msg = invalidOpException.Message,
                    Data = new { ErrorType = "FileTooLarge" }
                };
            }

            // Check if it's a validation error
            if (invalidOpException.Message.Contains("validation") || invalidOpException.Message.Contains("invalid"))
            {
                return new ApiResponse<object>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Message = invalidOpException.Message,
                    Msg = invalidOpException.Message,
                    Data = new { ErrorType = "ValidationError" }
                };
            }

            // Generic invalid operation error
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.BadRequest,
                Message = invalidOpException.Message,
                Msg = invalidOpException.Message,
                Data = new { ErrorType = "InvalidOperation" }
            };
        }

        private ApiResponse<object> CreateGenericErrorResponse(Exception exception)
        {
            // For development, show more details; for production, show generic message
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            
            var message = isDevelopment && !string.IsNullOrEmpty(exception.Message) 
                ? $"Server error: {exception.Message}"
                : "An internal server error occurred";

            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.InternalServerError,
                Message = message,
                Msg = message,
                Data = isDevelopment ? new { 
                    ExceptionType = exception.GetType().Name,
                    StackTrace = exception.StackTrace?.Split('\n').Take(5).ToArray() // Only first 5 lines
                } : null
            };
        }
    }
}