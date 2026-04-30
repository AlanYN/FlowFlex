using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Resolve operator information (name/id) from UserContext, headers, or claims
    /// </summary>
    public class OperatorContextService : IOperatorContextService, IScopedService
    {
        private readonly UserContext _userContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OperatorContextService> _logger;

        public OperatorContextService(
            UserContext userContext, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<OperatorContextService> logger)
        {
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetOperatorDisplayName()
        {
            _logger.LogDebug("GetOperatorDisplayName called - FirstName: {FirstName}, LastName: {LastName}, UserName: {UserName}, Email: {Email}, UserId: {UserId}",
                _userContext?.FirstName ?? "null",
                _userContext?.LastName ?? "null",
                _userContext?.UserName ?? "null",
                _userContext?.Email ?? "null",
                _userContext?.UserId ?? "null");

            // Priority 1: FirstName + LastName
            if (!string.IsNullOrWhiteSpace(_userContext?.FirstName) || !string.IsNullOrWhiteSpace(_userContext?.LastName))
            {
                var firstName = _userContext?.FirstName?.Trim() ?? "";
                var lastName = _userContext?.LastName?.Trim() ?? "";
                var fullName = $"{firstName} {lastName}".Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    _logger.LogDebug("Returning FirstName + LastName: {FullName}", fullName);
                    return fullName;
                }
            }

            // Priority 2: UserName
            if (!string.IsNullOrWhiteSpace(_userContext?.UserName))
            {
                _logger.LogDebug("Returning UserName: {UserName}", _userContext.UserName);
                return _userContext.UserName;
            }

            // Priority 3: Email
            if (!string.IsNullOrWhiteSpace(_userContext?.Email))
            {
                _logger.LogDebug("Returning Email: {Email}", _userContext.Email);
                return _userContext.Email;
            }

            var httpContext = _httpContextAccessor?.HttpContext;
            // Custom headers from gateway/frontend
            var headerEmail = httpContext?.Request?.Headers["X-User-Email"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerEmail))
            {
                return headerEmail;
            }
            var headerName = httpContext?.Request?.Headers["X-User-Name"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerName))
            {
                return headerName;
            }

            // Claims
            var user = httpContext?.User;
            if (user != null)
            {
                string[] emailFirstClaims = new[]
                {
                    ClaimTypes.Email,
                    "email",
                    "preferred_username",
                    ClaimTypes.Name,
                    "name",
                    ClaimTypes.GivenName,
                    "upn"
                };
                foreach (var ct in emailFirstClaims)
                {
                    var value = user.Claims.FirstOrDefault(c => c.Type == ct)?.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            // Fallback: use UserId if available
            if (!string.IsNullOrWhiteSpace(_userContext?.UserId))
            {
                _logger.LogDebug("Using UserId as fallback: User_{UserId}", _userContext.UserId);
                return $"User_{_userContext.UserId}";
            }

            _logger.LogDebug("No user info found, returning 'System'");
            return "System";
        }

        public long GetOperatorId()
        {
            if (!string.IsNullOrWhiteSpace(_userContext?.UserId) && long.TryParse(_userContext.UserId, out var idFromContext))
            {
                return idFromContext;
            }

            var httpContext = _httpContextAccessor?.HttpContext;
            var headerUserId = httpContext?.Request?.Headers["X-User-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerUserId) && long.TryParse(headerUserId, out var idFromHeader))
            {
                return idFromHeader;
            }

            var user = httpContext?.User;
            if (user != null)
            {
                string[] idClaims = new[]
                {
                    ClaimTypes.NameIdentifier,
                    "sub",
                    "user_id",
                    "uid"
                };
                foreach (var ct in idClaims)
                {
                    var v = user.Claims.FirstOrDefault(c => c.Type == ct)?.Value;
                    if (!string.IsNullOrWhiteSpace(v) && long.TryParse(v, out var idFromClaims))
                    {
                        return idFromClaims;
                    }
                }
            }

            return 0;
        }
    }
}

