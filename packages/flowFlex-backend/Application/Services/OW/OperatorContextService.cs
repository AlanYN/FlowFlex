using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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

        public OperatorContextService(UserContext userContext, IHttpContextAccessor httpContextAccessor)
        {
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetOperatorDisplayName()
        {
            Console.WriteLine($"[OperatorContextService] GetOperatorDisplayName called");
            Console.WriteLine($"[OperatorContextService] UserContext.FirstName: '{_userContext?.FirstName ?? "null"}'");
            Console.WriteLine($"[OperatorContextService] UserContext.LastName: '{_userContext?.LastName ?? "null"}'");
            Console.WriteLine($"[OperatorContextService] UserContext.UserName: '{_userContext?.UserName ?? "null"}'");
            Console.WriteLine($"[OperatorContextService] UserContext.Email: '{_userContext?.Email ?? "null"}'");
            Console.WriteLine($"[OperatorContextService] UserContext.UserId: '{_userContext?.UserId ?? "null"}'");

            // Priority 1: FirstName + LastName
            if (!string.IsNullOrWhiteSpace(_userContext?.FirstName) || !string.IsNullOrWhiteSpace(_userContext?.LastName))
            {
                var firstName = _userContext?.FirstName?.Trim() ?? "";
                var lastName = _userContext?.LastName?.Trim() ?? "";
                var fullName = $"{firstName} {lastName}".Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    Console.WriteLine($"[OperatorContextService] Returning FirstName + LastName: '{fullName}'");
                    return fullName;
                }
            }

            // Priority 2: UserName
            if (!string.IsNullOrWhiteSpace(_userContext?.UserName))
            {
                Console.WriteLine($"[OperatorContextService] Returning UserName: '{_userContext.UserName}'");
                return _userContext.UserName;
            }

            // Priority 3: Email
            if (!string.IsNullOrWhiteSpace(_userContext?.Email))
            {
                Console.WriteLine($"[OperatorContextService] Returning Email: '{_userContext.Email}'");
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
                Console.WriteLine($"[OperatorContextService] Using UserId as fallback: 'User_{_userContext.UserId}'");
                return $"User_{_userContext.UserId}";
            }

            Console.WriteLine($"[OperatorContextService] No user info found, returning 'System'");
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

