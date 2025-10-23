using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.Auth.Authorization;
using Item.Internal.Auth.Authorization.BnpToken.Services;
using Item.ThirdParty.IdentityHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace WebApi.Authorization
{
    /// <summary>
    /// FlowFlex authorization handler
    /// Implements permission verification mechanism similar to UnisAuthorize in Unis CRM
    /// </summary>
    public class WfeAuthorizationHandler : AbstractAuthorizationHandler
    {
        private readonly IdentityHubConfigOptions _options;
        private readonly UserContext _userContext;
        private readonly IdentityHubClient _client;

        public WfeAuthorizationHandler(
            IBnpTokenService bnpTokenService,
            IOptions<IdentityHubConfigOptions> options,
            UserContext userContext,
            IdentityHubClient client) : base(bnpTokenService)
        {
            _options = options.Value;
            _userContext = userContext;
            _client = client;
        }

        /// <summary>
        /// Check user permissions against requested permissions
        /// This method is called when UnisAuthorize attribute is used on controllers/actions
        /// </summary>
        /// <param name="requestPermissions">Permissions requested by the controller/action</param>
        /// <returns>
        /// True if user has ANY of the requested permissions (OR logic), false otherwise
        /// When multiple permissions are provided, user only needs ONE to pass
        /// </returns>
        protected override async Task<bool> CheckUserPermissionsAsync(IEnumerable<string> requestPermissions)
        {
            // For special authentication schemes, bypass permission check
            // Similar to Unis CRM's approach with PassIdentification, IdentityClient, etc.
            if (IsSpecialAuthenticationScheme())
            {
                return true;
            }

            // System Admin bypass - check if user is system administrator
            if (_userContext?.IsSystemAdmin == true)
            {
                return true;
            }

            // Tenant Admin bypass - check if user is tenant administrator for current tenant
            var currentTenantId = _userContext?.TenantId ?? "DEFAULT";
            if (_userContext != null && _userContext.HasAdminPrivileges(currentTenantId))
            {
                return true;
            }

            // For regular users, check permissions via IdentityHub
            if (_userContext?.IamToken == null)
            {
                // If no IAM token, deny access
                return false;
            }

            var permissionList = requestPermissions.ToList();
            if (!permissionList.Any())
            {
                // No permissions requested, deny by default
                return false;
            }

            try
            {
                // OR Logic: Check each permission individually
                // User passes if they have ANY of the requested permissions
                foreach (var permission in permissionList)
                {
                    var hasPermission = await _client.UserRolePermissionCheck(
                        _userContext.IamToken,
                        new List<string> { permission });

                    if (hasPermission)
                    {
                        // User has this permission, grant access
                        return true;
                    }
                }

                // User doesn't have any of the requested permissions
                return false;
            }
            catch (Exception)
            {
                // On error, deny access for security
                return false;
            }
        }

        /// <summary>
        /// Handle user authentication requirement
        /// This method verifies that the user is authenticated and has valid claims
        /// </summary>
        /// <param name="context">Authorization handler context</param>
        /// <returns>True if user is authenticated and valid, false otherwise</returns>
        protected override async Task<bool> UserRequirementHandler(AuthorizationHandlerContext context)
        {
            await Task.CompletedTask;

            // Check if user is authenticated
            if (context.User.Identity is null || !context.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var claims = context.User.Claims.ToList();
            var scheme = _userContext.Schema;

            // For Identification scheme (IdentityHub), perform additional validation
            if (scheme is AuthSchemes.Identification)
            {
                var tokenCategory = claims.FirstOrDefault(x => x.Type == "token_category")?.Value;
                var userId = claims.FirstOrDefault(x => x.Type is "userId" or JwtRegisteredClaimNames.NameId)?.Value;

                // User ID is required
                if (userId == null)
                {
                    return false;
                }

                // If token has category, validate application ID
                if (tokenCategory != null)
                {
                    var appId = claims.FirstOrDefault(x => x.Type == "app_id")?.Value;
                    if (appId != _options.ApplicationId)
                    {
                        // Token is not for this application
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if current authentication scheme is a special scheme that bypasses permission checks
        /// </summary>
        /// <returns>True if special authentication scheme, false otherwise</returns>
        private bool IsSpecialAuthenticationScheme()
        {
            var scheme = _userContext?.Schema;
            
            // Add special authentication schemes that bypass permission checks
            // Similar to Unis CRM's PassIdentification, IdentityClient, ItemIamClientIdentification
            var specialSchemes = new[]
            {
                AuthSchemes.PassIdentification,
                AuthSchemes.IdentityClient,
                AuthSchemes.ItemIamClientIdentification
            };

            return specialSchemes.Contains(scheme);
        }
    }
}
