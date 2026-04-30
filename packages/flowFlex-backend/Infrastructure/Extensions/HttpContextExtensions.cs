using Microsoft.AspNetCore.Http;

namespace FlowFlex.Infrastructure.Extensions
{
    /// <summary>
    /// Shared extension methods for HttpContext to eliminate duplicate utility code across services
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Get client IP address from HTTP context, checking proxy headers first
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>Client IP address or "unknown" if not available</returns>
        public static string GetClientIpAddress(this HttpContext? context)
        {
            if (context == null) return string.Empty;

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                          ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
                          ?? context.Connection.RemoteIpAddress?.ToString();

            // X-Forwarded-For may contain multiple IPs (client, proxy1, proxy2...)
            // Take the first one which is the original client IP
            if (!string.IsNullOrEmpty(ipAddress) && ipAddress.Contains(','))
            {
                ipAddress = ipAddress.Split(',')[0].Trim();
            }

            return ipAddress ?? string.Empty;
        }

        /// <summary>
        /// Get client IP address from IHttpContextAccessor
        /// </summary>
        public static string GetClientIpAddress(this IHttpContextAccessor? accessor)
        {
            return accessor?.HttpContext.GetClientIpAddress() ?? string.Empty;
        }

        /// <summary>
        /// Get User-Agent string from HTTP context
        /// </summary>
        public static string GetUserAgent(this HttpContext? context)
        {
            return context?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
        }

        /// <summary>
        /// Get User-Agent string from IHttpContextAccessor
        /// </summary>
        public static string GetUserAgent(this IHttpContextAccessor? accessor)
        {
            return accessor?.HttpContext.GetUserAgent() ?? string.Empty;
        }
    }
}
