using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts.Options;
using System.Security.Claims;

namespace FlowFlex.WebApi.Middlewares
{
    /// <summary>
    /// File access security middleware
    /// </summary>
    public class FileAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FileAccessMiddleware> _logger;
        private readonly FileStorageOptions _options;

        public FileAccessMiddleware(
            RequestDelegate next,
            ILogger<FileAccessMiddleware> logger,
            IOptions<FileStorageOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a file access request
            if (context.Request.Path.StartsWithSegments("/uploads"))
            {
                // If access control is enabled
                if (_options.EnableAccessControl)
                {
                    var accessResult = await ValidateFileAccessAsync(context);
                    if (!accessResult.IsAllowed)
                    {
                        context.Response.StatusCode = accessResult.StatusCode;
                        await context.Response.WriteAsync(accessResult.Message);
                        return;
                    }
                }

                // Add security headers
                AddSecurityHeaders(context);

                // Log file access
                LogFileAccess(context);
            }

            await _next(context);
        }

        private async Task<FileAccessResult> ValidateFileAccessAsync(HttpContext context)
        {
            try
            {
                // Check user authentication
                if (!context.User.Identity?.IsAuthenticated == true)
                {
                    // Some public files may not require authentication
                    if (IsPublicFile(context.Request.Path))
                    {
                        return FileAccessResult.Allow();
                    }

                    return FileAccessResult.Deny(401, "Unauthorized access");
                }

                // Get user information
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "default";

                // Check if file path belongs to user's tenant
                var filePath = context.Request.Path.Value;
                if (!string.IsNullOrEmpty(filePath) && !IsUserAllowedToAccessFile(filePath, tenantId, userId))
                {
                    return FileAccessResult.Deny(403, "No permission to access this file");
                }

                // Check if file type is safe
                if (!IsSafeFileType(filePath))
                {
                    return FileAccessResult.Deny(403, "Unsafe file type");
                }

                return FileAccessResult.Allow();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file access");
                return FileAccessResult.Deny(500, "Internal server error");
            }
        }

        private bool IsPublicFile(PathString path)
        {
            // Define public file paths (such as avatars, public images, etc.)
            var publicPaths = new[]
            {
                "/uploads/public/",
                "/uploads/avatars/",
                "/uploads/common/"
            };

            return publicPaths.Any(p => path.StartsWithSegments(p));
        }

        private bool IsUserAllowedToAccessFile(string filePath, string tenantId, string userId)
        {
            // Check if file path contains user's tenant ID
            // File path format: /uploads/{tenantId}/{category}/{year}/{month}/{day}/{filename}
            var pathParts = filePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathParts.Length >= 2 && pathParts[0] == "uploads")
            {
                var fileTenantId = pathParts[1];
                
                // Check if tenant ID matches
                if (fileTenantId != tenantId)
                {
                    return false;
                }
            }

            // Can add more fine-grained permission checks
            // For example: check if user has permission to access specific category files
            
            return true;
        }

        private bool IsSafeFileType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Dangerous file types
            var dangerousExtensions = new[]
            {
                ".exe", ".bat", ".cmd", ".com", ".scr", ".pif",
                ".js", ".vbs", ".ps1", ".sh", ".php", ".asp", ".aspx"
            };

            return !dangerousExtensions.Contains(extension);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;
            
            // Prevent content sniffing
            response.Headers.Add("X-Content-Type-Options", "nosniff");
            
            // Prevent display in iframe
            response.Headers.Add("X-Frame-Options", "DENY");
            
            // XSS protection
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            // Content security policy
            response.Headers.Add("Content-Security-Policy", "default-src 'none'; img-src 'self'; media-src 'self'; object-src 'none';");
            
            // Disable caching for sensitive files
            var filePath = context.Request.Path.Value?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(filePath) && IsSensitiveFile(filePath))
            {
                response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                response.Headers.Add("Pragma", "no-cache");
                response.Headers.Add("Expires", "0");
            }
        }

        private bool IsSensitiveFile(string filePath)
        {
            // Define sensitive file types (such as personal documents, contracts, etc.)
            var sensitiveCategories = new[]
            {
                "/uploads/default/documents/",
                "/uploads/default/contracts/",
                "/uploads/default/personal/"
            };

            return sensitiveCategories.Any(category => filePath.Contains(category));
        }

        private void LogFileAccess(HttpContext context)
        {
            var filePath = context.Request.Path.Value;
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation(
                "File access: {FilePath} by User: {UserId}, IP: {IpAddress}, UserAgent: {UserAgent}",
                filePath, userId ?? "Anonymous", ipAddress, userAgent);
        }
    }

    /// <summary>
    /// File access result
    /// </summary>
    public class FileAccessResult
    {
        public bool IsAllowed { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }

        public static FileAccessResult Allow()
        {
            return new FileAccessResult { IsAllowed = true };
        }

        public static FileAccessResult Deny(int statusCode, string message)
        {
            return new FileAccessResult 
            { 
                IsAllowed = false, 
                StatusCode = statusCode, 
                Message = message 
            };
        }
    }
} 
