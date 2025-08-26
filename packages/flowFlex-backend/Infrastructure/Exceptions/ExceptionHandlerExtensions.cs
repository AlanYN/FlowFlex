using System.Net;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FlowFlex.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception handling extension methods
    /// </summary>
    public static class ExceptionHandlerExtensions
    {
        /// <summary>
        /// Convert exception to standardized API response
        /// </summary>
        /// <param name="exception">Exception to handle</param>
        /// <param name="context">HTTP context</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Standardized API response</returns>
        public static ApiResponse<object> ToApiResponse(this Exception exception, HttpContext context = null, IApplicationLogger logger = null)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            
            // Determine log level based on exception type
            var logLevel = GetLogLevel(exception);
            
            // Log the exception with appropriate level and context
            if (logger != null && context != null)
            {
                var logMessage = "Exception occurred. ErrorId: {ErrorId}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}, RemoteIP: {RemoteIP}, Exception: {ExceptionMessage}";
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                switch (logLevel)
                {
                    case LogLevel.Error:
                        logger.LogError(exception, logMessage, errorId, context.Request.Path, context.Request.Method, userAgent, remoteIp, exception.Message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(logMessage, errorId, context.Request.Path, context.Request.Method, userAgent, remoteIp, exception.Message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(logMessage, errorId, context.Request.Path, context.Request.Method, userAgent, remoteIp, exception.Message);
                        break;
                }
            }

            var response = exception switch
            {
                CRMException crmEx => crmEx.ToApiResponse(),
                PostgresException pgEx => pgEx.ToApiResponse(),
                InvalidOperationException invOpEx => invOpEx.ToApiResponse(),
                ArgumentNullException argEx => argEx.ToApiResponse(),
                ArgumentException argEx => argEx.ToApiResponse(),
                UnauthorizedAccessException => CreateUnauthorizedResponse(),
                TimeoutException => CreateTimeoutResponse(),
                NotImplementedException => CreateNotImplementedResponse(),
                _ => CreateGenericErrorResponse(exception)
            };

            // Add error ID and additional metadata to response
            var additionalData = new Dictionary<string, object>
            {
                ["ErrorId"] = errorId,
                ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            if (response.Data != null)
            {
                // Merge existing data with additional data
                if (response.Data is Dictionary<string, object> existingDict)
                {
                    foreach (var kvp in additionalData)
                    {
                        existingDict[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    additionalData["OriginalData"] = response.Data;
                    response.Data = additionalData;
                }
            }
            else
            {
                response.Data = additionalData;
            }

            return response;
        }

        /// <summary>
        /// Determine appropriate log level for exception type
        /// </summary>
        private static LogLevel GetLogLevel(Exception exception)
        {
            return exception switch
            {
                CRMException crmEx when crmEx.StatusCode == HttpStatusCode.Unauthorized => LogLevel.Warning,
                CRMException crmEx when crmEx.StatusCode == HttpStatusCode.Forbidden => LogLevel.Warning,
                CRMException crmEx when crmEx.StatusCode == HttpStatusCode.BadRequest => LogLevel.Information,
                CRMException crmEx when crmEx.StatusCode == HttpStatusCode.NotFound => LogLevel.Information,
                ArgumentNullException => LogLevel.Information, // More specific case first
                ArgumentException => LogLevel.Information,
                UnauthorizedAccessException => LogLevel.Warning,
                PostgresException pgEx when pgEx.SqlState?.StartsWith("23") == true => LogLevel.Information, // Constraint violations
                PostgresException pgEx when pgEx.SqlState?.StartsWith("22") == true => LogLevel.Information, // Data exceptions
                PostgresException => LogLevel.Error,
                TimeoutException => LogLevel.Warning,
                NotImplementedException => LogLevel.Warning,
                _ => LogLevel.Error
            };
        }

        /// <summary>
        /// Convert CRM exception to API response
        /// </summary>
        public static ApiResponse<object> ToApiResponse(this CRMException crmException)
        {
            var statusCode = crmException.StatusCode.HasValue ? (int)crmException.StatusCode.Value : 400;
            return new ApiResponse<object>
            {
                Code = statusCode,
                Message = crmException.Message,
                Msg = crmException.Message,
                Data = crmException.ErrorData ?? new { ErrorCode = crmException.Code.ToString() }
            };
        }

        /// <summary>
        /// Convert PostgreSQL exception to API response
        /// </summary>
        public static ApiResponse<object> ToApiResponse(this PostgresException pgException)
        {
            var (statusCode, message) = pgException.SqlState switch
            {
                "23505" => (HttpStatusCode.Conflict, GetUniqueConstraintMessage(pgException)),
                "23503" => (HttpStatusCode.BadRequest, "Related data does not exist"),
                "23502" => (HttpStatusCode.BadRequest, "Required field cannot be empty"),
                "23514" => (HttpStatusCode.BadRequest, "Data validation failed"),
                "22001" => (HttpStatusCode.BadRequest, GetStringLengthMessage(pgException)),
                "22003" => (HttpStatusCode.BadRequest, "Numeric value out of range"),
                "22007" => (HttpStatusCode.BadRequest, "Invalid date/time format"),
                "22008" => (HttpStatusCode.BadRequest, "Invalid datetime value"),
                "22012" => (HttpStatusCode.BadRequest, "Division by zero"),
                "42P01" => (HttpStatusCode.InternalServerError, "Database configuration error"),
                "42703" => (HttpStatusCode.InternalServerError, "Database structure error"),
                "08003" => (HttpStatusCode.ServiceUnavailable, "Database connection lost"),
                "08006" => (HttpStatusCode.ServiceUnavailable, "Database service unavailable"),
                "40001" => (HttpStatusCode.Conflict, "Database transaction conflict - Please retry"),
                "40P01" => (HttpStatusCode.Conflict, "Database deadlock - Please retry"),
                "53300" => (HttpStatusCode.ServiceUnavailable, "Database resources exhausted"),
                _ => (HttpStatusCode.InternalServerError, "Database operation failed")
            };

            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            
            return new ApiResponse<object>
            {
                Code = (int)statusCode,
                Message = message,
                Msg = message,
                Data = isDevelopment ? new 
                { 
                    SqlState = pgException.SqlState,
                    ConstraintName = pgException.ConstraintName,
                    TableName = pgException.TableName,
                    Detail = pgException.Detail,
                    ErrorType = "DatabaseError"
                } : new 
                { 
                    ErrorType = "DatabaseError",
                    SqlState = pgException.SqlState
                }
            };
        }

        /// <summary>
        /// Convert InvalidOperationException to API response
        /// </summary>
        public static ApiResponse<object> ToApiResponse(this InvalidOperationException invalidOpException)
        {
            var errorType = invalidOpException.Message switch
            {
                var msg when msg.Contains("Unsupported file type") => "UnsupportedFileType",
                var msg when msg.Contains("file size") || msg.Contains("too large") => "FileTooLarge",
                var msg when msg.Contains("validation") || msg.Contains("invalid") => "ValidationError",
                _ => "InvalidOperation"
            };

            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.BadRequest,
                Message = invalidOpException.Message,
                Msg = invalidOpException.Message,
                Data = new { ErrorType = errorType }
            };
        }

        /// <summary>
        /// Convert ArgumentException to API response
        /// </summary>
        public static ApiResponse<object> ToApiResponse(this ArgumentException argException)
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.BadRequest,
                Message = "Invalid request parameters",
                Msg = "Invalid request parameters",
                Data = new { Details = argException.Message, ErrorType = "ArgumentError" }
            };
        }

        /// <summary>
        /// Create a simplified error response for client consumption
        /// </summary>
        /// <param name="exception">Source exception</param>
        /// <param name="userFriendlyMessage">User-friendly message</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Simplified API response</returns>
        public static ApiResponse<object> ToSimpleResponse(this Exception exception, 
            string userFriendlyMessage, 
            HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new ApiResponse<object>
            {
                Code = (int)statusCode,
                Message = userFriendlyMessage,
                Msg = userFriendlyMessage,
                Data = new { ErrorType = exception.GetType().Name }
            };
        }

        #region Private Helper Methods

        private static string GetUniqueConstraintMessage(PostgresException pgException)
        {
            return pgException.ConstraintName switch
            {
                "idx_users_email_tenant" => "User already exists in this tenant",
                "idx_users_email" => "User with this email already exists",
                _ => "A record with these values already exists"
            };
        }

        private static string GetStringLengthMessage(PostgresException pgException)
        {
            var errorDetail = pgException.Detail ?? pgException.MessageText ?? "";
            
            // Extract numeric limit using regex
            var match = System.Text.RegularExpressions.Regex.Match(errorDetail, @"varying\((\d+)\)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int limit))
            {
                return $"Input text is too long. Maximum {limit} characters allowed";
            }

            return "Input text exceeds maximum allowed length";
        }

        private static ApiResponse<object> CreateUnauthorizedResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.Unauthorized,
                Message = "Unauthorized access",
                Msg = "Unauthorized access",
                Data = new { ErrorType = "UnauthorizedAccess" }
            };
        }

        private static ApiResponse<object> CreateTimeoutResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.RequestTimeout,
                Message = "Request timeout",
                Msg = "Request timeout", 
                Data = new { ErrorType = "Timeout" }
            };
        }

        private static ApiResponse<object> CreateNotImplementedResponse()
        {
            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.NotImplemented,
                Message = "Feature not implemented",
                Msg = "Feature not implemented",
                Data = new { ErrorType = "NotImplemented" }
            };
        }

        private static ApiResponse<object> CreateGenericErrorResponse(Exception exception)
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            
            var message = isDevelopment && !string.IsNullOrEmpty(exception.Message) 
                ? $"Server error: {exception.Message}"
                : "An internal server error occurred";

            return new ApiResponse<object>
            {
                Code = (int)HttpStatusCode.InternalServerError,
                Message = message,
                Msg = message,
                Data = isDevelopment ? new 
                { 
                    ExceptionType = exception.GetType().Name,
                    StackTrace = exception.StackTrace?.Split('\n').Take(5).ToArray(),
                    ErrorType = "InternalError"
                } : new 
                { 
                    ErrorType = "InternalError" 
                }
            };
        }

        #endregion
    }
}