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
            // Use extension method for consistent error handling
            var errorResponse = exception.ToApiResponse(context, logger);

            // Set response properties
            context.Response.StatusCode = errorResponse.Code;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }

        // Note: All error handling logic has been moved to ExceptionHandlerExtensions.cs
        // This keeps the middleware clean and promotes reusability
    }
}