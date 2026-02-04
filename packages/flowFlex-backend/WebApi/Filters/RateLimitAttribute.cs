using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace FlowFlex.WebApi.Filters
{
    /// <summary>
    /// Rate limiting attribute for protecting anonymous endpoints from abuse
    /// Uses sliding window algorithm with in-memory cache
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _maxRequests;
        private readonly int _windowSeconds;
        private readonly string _keyPrefix;

        /// <summary>
        /// Initialize rate limit attribute
        /// </summary>
        /// <param name="maxRequests">Maximum requests allowed in the time window</param>
        /// <param name="windowSeconds">Time window in seconds</param>
        /// <param name="keyPrefix">Optional prefix for cache key (default: endpoint path)</param>
        public RateLimitAttribute(int maxRequests = 10, int windowSeconds = 60, string keyPrefix = "")
        {
            _maxRequests = maxRequests;
            _windowSeconds = windowSeconds;
            _keyPrefix = keyPrefix;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache == null)
            {
                // If cache is not available, allow the request
                await next();
                return;
            }

            var clientKey = GetClientKey(context);
            var cacheKey = $"RateLimit:{_keyPrefix}:{clientKey}";

            var requestCount = cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_windowSeconds);
                return 0;
            });

            if (requestCount >= _maxRequests)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    error = "Too many requests. Please try again later.",
                    retryAfterSeconds = _windowSeconds
                })
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests
                };

                context.HttpContext.Response.Headers.Append("Retry-After", _windowSeconds.ToString());
                return;
            }

            // Increment request count
            cache.Set(cacheKey, requestCount + 1, TimeSpan.FromSeconds(_windowSeconds));

            await next();
        }

        private string GetClientKey(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            // Try to get client IP from various headers (for proxied requests)
            var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            // Use first IP if multiple are present (X-Forwarded-For can contain multiple IPs)
            if (clientIp.Contains(','))
            {
                clientIp = clientIp.Split(',')[0].Trim();
            }

            // Include endpoint path for more granular rate limiting
            var path = string.IsNullOrEmpty(_keyPrefix) 
                ? httpContext.Request.Path.ToString() 
                : _keyPrefix;

            return $"{clientIp}:{path}";
        }
    }
}
