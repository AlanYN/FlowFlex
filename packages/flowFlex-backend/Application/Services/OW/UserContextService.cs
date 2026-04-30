using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain;
using FlowFlex.Domain.Shared;
using FlowFlex.Infrastructure.Services;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// User context service
    /// </summary>
    public class UserContextService : IUserContextService, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserContextService> _logger;

        public UserContextService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserContextService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get current user ID
        /// </summary>
        /// <returns>User ID</returns>
        public long GetCurrentUserId()
        {
            // First try to get user ID from "sub" claim (JWT standard)
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return 0;
            }

            return long.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get current user email
        /// </summary>
        /// <returns>User email</returns>
        public string GetCurrentUserEmail()
        {
            var emailClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email);
            return emailClaim?.Value ?? string.Empty;
        }

        /// <summary>
        /// Get current username
        /// </summary>
        /// <returns>Username</returns>
        public string GetCurrentUsername()
        {
            var usernameClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("username");
            return usernameClaim?.Value ?? string.Empty;
        }

        /// <summary>
        /// Get current user's tenant ID
        /// </summary>
        /// <returns>Tenant ID</returns>
        public string GetCurrentTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenantId");
            if (!string.IsNullOrEmpty(tenantIdClaim?.Value))
            {
                return tenantIdClaim.Value;
            }

            // If no tenant ID in token, try to get from email
            var email = GetCurrentUserEmail();
            if (!string.IsNullOrEmpty(email))
            {
                return TenantHelper.GetTenantIdByEmail(email);
            }

            return "default";
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        /// <returns>Whether user is authenticated</returns>
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}