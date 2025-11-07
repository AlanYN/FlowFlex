using System;
using System.Collections.Generic;
using System.Linq;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.OW.Permission
{
    /// <summary>
    /// Permission helper methods shared across all permission services
    /// Provides common validation, deserialization, and check logic
    /// </summary>
    public class PermissionHelpers : IScopedService
    {
        private readonly ILogger<PermissionHelpers> _logger;
        private readonly UserContext _userContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionHelpers(
            ILogger<PermissionHelpers> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Team and User Checks

        /// <summary>
        /// Get user team IDs from UserContext
        /// If user is not in any team, returns ["Other"] to represent users without team assignment
        /// </summary>
        public List<string> GetUserTeamIds()
        {
            _logger.LogDebug(
                "GetUserTeamIds - UserContext: {HasContext}, UserTeams: {HasTeams}, UserId: {UserId}",
                _userContext != null,
                _userContext?.UserTeams != null,
                _userContext?.UserId);

            if (_userContext?.UserTeams == null)
            {
                _logger.LogWarning(
                    "UserContext.UserTeams is null for user {UserId}. " +
                    "User is not assigned to any team, treating as member of 'Other' team. " +
                    "This allows permission checks to work for users without team assignments.",
                    _userContext?.UserId);

                // User not in any team, return "Other" to represent users without team assignment
                return new List<string> { "Other" };
            }

            // Get all team IDs (including sub-teams)
            var teamIds = _userContext.UserTeams.GetAllTeamIds();
            var teamIdStrings = teamIds.Select(id => id.ToString()).ToList();

            // If user has no teams (empty list), treat as "Other" team member
            if (!teamIdStrings.Any())
            {
                _logger.LogDebug(
                    "GetUserTeamIds - User {UserId} has no team assignments, treating as member of 'Other' team",
                    _userContext?.UserId);
                return new List<string> { "Other" };
            }

            _logger.LogDebug(
                "GetUserTeamIds - Found {Count} teams: {Teams}",
                teamIdStrings.Count,
                string.Join(", ", teamIdStrings));

            return teamIdStrings;
        }

        /// <summary>
        /// Check if user belongs to any team in the whitelist
        /// </summary>
        public bool CheckTeamWhitelist(string teamsJson, List<string> userTeamIds)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
            {
                return false;
            }

            var teams = DeserializeTeamList(teamsJson);
            return userTeamIds.Any(teamId => teams.Contains(teamId));
        }

        /// <summary>
        /// Check if user does NOT belong to any team in the blacklist
        /// </summary>
        public bool CheckTeamBlacklist(string teamsJson, List<string> userTeamIds)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
            {
                return true; // No blacklist means everyone can access
            }

            var teams = DeserializeTeamList(teamsJson);
            return !userTeamIds.Any(teamId => teams.Contains(teamId));
        }

        /// <summary>
        /// Check if user is in the whitelist
        /// </summary>
        public bool CheckUserWhitelist(string usersJson, string userId)
        {
            if (string.IsNullOrWhiteSpace(usersJson))
            {
                return false;
            }

            var users = DeserializeTeamList(usersJson);
            return users.Contains(userId);
        }

        /// <summary>
        /// Check if user is NOT in the blacklist
        /// </summary>
        public bool CheckUserBlacklist(string usersJson, string userId)
        {
            if (string.IsNullOrWhiteSpace(usersJson))
            {
                return true; // No blacklist means everyone can access
            }

            var users = DeserializeTeamList(usersJson);
            return !users.Contains(userId);
        }

        /// <summary>
        /// Check if current user is the owner of the entity
        /// </summary>
        public bool IsCurrentUserOwner(long? createUserId)
        {
            if (string.IsNullOrEmpty(_userContext?.UserId) || !createUserId.HasValue)
            {
                return false;
            }

            return long.TryParse(_userContext.UserId, out var currentUserId) &&
                   createUserId.Value == currentUserId;
        }

        /// <summary>
        /// Check operate teams in Public mode
        /// NULL or empty means everyone can operate, otherwise whitelist
        /// </summary>
        public bool CheckOperateTeamsPublicMode(string operateTeamsJson, List<string> userTeamIds)
        {
            if (string.IsNullOrWhiteSpace(operateTeamsJson))
            {
                _logger.LogDebug("Public mode with NULL OperateTeams - granting operate permission to all");
                return true;
            }

            var operateTeams = DeserializeTeamList(operateTeamsJson);
            if (operateTeams.Count == 0)
            {
                _logger.LogDebug("Public mode with empty OperateTeams array - granting operate permission to all");
                return true;
            }

            // Public mode with specific teams - check membership (whitelist)
            var hasPermission = userTeamIds.Any(teamId => operateTeams.Contains(teamId));
            _logger.LogDebug(
                "Public mode with specific OperateTeams (whitelist): [{Teams}], Has permission: {HasPermission}",
                string.Join(", ", operateTeams),
                hasPermission);
            return hasPermission;
        }

        /// <summary>
        /// Check operate users in Public mode
        /// NULL or empty means everyone can operate, otherwise whitelist
        /// </summary>
        public bool CheckOperateUsersPublicMode(string operateUsersJson, string userId)
        {
            if (string.IsNullOrWhiteSpace(operateUsersJson))
            {
                _logger.LogDebug("Public mode with NULL OperateUsers - granting operate permission to all");
                return true;
            }

            var operateUsers = DeserializeTeamList(operateUsersJson);
            if (operateUsers.Count == 0)
            {
                _logger.LogDebug("Public mode with empty OperateUsers array - granting operate permission to all");
                return true;
            }

            // Public mode with specific users - check membership (whitelist)
            var hasPermission = operateUsers.Contains(userId);
            _logger.LogDebug(
                "Public mode with specific OperateUsers (whitelist): [{Users}], Has permission: {HasPermission}",
                string.Join(", ", operateUsers),
                hasPermission);
            return hasPermission;
        }

        #endregion

        #region Admin and Tenant Checks

        /// <summary>
        /// Get current tenant ID from UserContext
        /// </summary>
        public string GetCurrentTenantId()
        {
            return _userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Check if user is System Admin
        /// </summary>
        public bool IsSystemAdmin()
        {
            return _userContext?.IsSystemAdmin == true;
        }

        /// <summary>
        /// Check if user is Tenant Admin for current tenant
        /// </summary>
        public bool IsTenantAdmin()
        {
            var currentTenantId = GetCurrentTenantId();
            return _userContext != null && _userContext.HasAdminPrivileges(currentTenantId);
        }

        /// <summary>
        /// Check if user has admin privileges (System Admin or Tenant Admin)
        /// </summary>
        public bool HasAdminPrivileges()
        {
            return IsSystemAdmin() || IsTenantAdmin();
        }

        #endregion

        #region Portal Token Checks

        /// <summary>
        /// Check if current request is using a Portal token and accessing a [PortalAccess] endpoint
        /// Portal tokens should bypass module permission checks on endpoints marked with [PortalAccess]
        /// </summary>
        public bool IsPortalTokenWithPortalAccess()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: HttpContext is null");
                return false;
            }

            var user = httpContext.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: User is null or not authenticated");
                return false;
            }

            // Check for Portal token indicators in claims
            var scope = user.Claims.FirstOrDefault(c => c.Type == "scope")?.Value;
            var tokenType = user.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;

            _logger.LogDebug(
                "IsPortalTokenWithPortalAccess: Checking claims - Scope: {Scope}, TokenType: {TokenType}",
                scope ?? "NULL",
                tokenType ?? "NULL");

            // Portal token has scope="portal" and token_type="portal-access"
            bool isPortalToken = scope == "portal" && tokenType == "portal-access";

            if (!isPortalToken)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: Not a portal token");
                return false;
            }

            // Check if endpoint has [PortalAccess] attribute
            var endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
            {
                _logger.LogDebug("IsPortalTokenWithPortalAccess: Endpoint is null");
                return false;
            }

            var portalAccessAttr = endpoint.Metadata.GetMetadata<FlowFlex.Application.Filter.PortalAccessAttribute>();
            bool hasPortalAccess = portalAccessAttr != null;

            _logger.LogDebug(
                "IsPortalTokenWithPortalAccess: Endpoint has [PortalAccess]: {HasPortalAccess}",
                hasPortalAccess);

            return hasPortalAccess;
        }

        #endregion

        #region JSON Deserialization

        /// <summary>
        /// Deserialize team list from JSON, handling double-escaped JSON
        /// </summary>
        public List<string> DeserializeTeamList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                // Try direct deserialization first
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                try
                {
                    // If that fails, try double deserialization (for double-escaped JSON)
                    var jsonString = JsonConvert.DeserializeObject<string>(json);
                    return JsonConvert.DeserializeObject<List<string>>(jsonString) ?? new List<string>();
                }
                catch
                {
                    _logger.LogWarning("Failed to deserialize team list: {Json}", json);
                    return new List<string>();
                }
            }
        }

        #endregion
    }
}

