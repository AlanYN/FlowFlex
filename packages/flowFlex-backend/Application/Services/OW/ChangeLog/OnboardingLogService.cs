using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.OperationChangeLog;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Repository.OW;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.Application.Services.OW.ChangeLog
{
    /// <summary>
    /// Onboarding-related operation log service
    /// </summary>
    public class OnboardingLogService : BaseOperationLogService, IOnboardingLogService
    {
        public OnboardingLogService(
            IOperationChangeLogRepository operationChangeLogRepository,
            ILogger<OnboardingLogService> logger,
            UserContext userContext,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogCacheService logCacheService,
            IUserService userService,
            IOperatorContextService operatorContextService)
            : base(operationChangeLogRepository, logger, userContext, httpContextAccessor, mapper, logCacheService, userService, operatorContextService)
        {
        }

        protected override string GetBusinessModuleName() => BusinessModuleEnum.Onboarding.ToString();

        #region Onboarding CRUD Operations

        /// <summary>
        /// Log onboarding create operation
        /// </summary>
        public async Task<bool> LogOnboardingCreateAsync(long onboardingId, string onboardingName, string afterData = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Created: {onboardingName}";

                string operationDescription;
                if (!string.IsNullOrEmpty(afterData))
                {
                    // Extract all fields from afterData and display them
                    var fieldDetails = await GetCreateFieldDetailsAsync(afterData);
                    if (!string.IsNullOrEmpty(fieldDetails))
                    {
                        operationDescription = $"Case '{onboardingName}' has been created by {GetOperatorDisplayName()}. {fieldDetails}";
                    }
                    else
                    {
                        operationDescription = $"Case '{onboardingName}' has been created by {GetOperatorDisplayName()}";
                    }
                }
                else
                {
                    operationDescription = $"Case '{onboardingName}' has been created by {GetOperatorDisplayName()}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        CreatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseCreate,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    beforeData: null,
                    afterData: afterData,
                    changedFields: null,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding create operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Get field details for create operation - display all fields
        /// </summary>
        private async Task<string> GetCreateFieldDetailsAsync(string afterData)
        {
            try
            {
                var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);
                if (afterJson == null || !afterJson.Any())
                {
                    return string.Empty;
                }

                var fieldList = new List<string>();
                foreach (var kvp in afterJson)
                {
                    // Skip Ownership field if OwnershipName exists (we'll show OwnershipName instead)
                    if (kvp.Key.Equals("Ownership", StringComparison.OrdinalIgnoreCase) &&
                        afterJson.ContainsKey("OwnershipName"))
                    {
                        continue;
                    }

                    // Skip WorkflowId field if WorkflowName exists (we'll show WorkflowName instead)
                    if (kvp.Key.Equals("WorkflowId", StringComparison.OrdinalIgnoreCase) &&
                        afterJson.ContainsKey("WorkflowName"))
                    {
                        continue;
                    }

                    // Skip CurrentStageId and LifeCycleStageId fields
                    if (kvp.Key.Equals("CurrentStageId", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Equals("LifeCycleStageId", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fieldName = GetFriendlyFieldName(kvp.Key);
                    var fieldValue = await GetDisplayValueForCreateAsync(kvp.Value, kvp.Key);
                    // Skip empty values (including empty arrays)
                    if (!string.IsNullOrEmpty(fieldValue))
                    {
                        fieldList.Add($"{fieldName}: {fieldValue}");
                    }
                }

                if (fieldList.Any())
                {
                    return $"Created with: {string.Join(", ", fieldList)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse create field details from JSON data");
            }

            return string.Empty;
        }

        /// <summary>
        /// Get display value for create operation with special handling for arrays and enums
        /// </summary>
        private async Task<string> GetDisplayValueForCreateAsync(object value, string fieldName)
        {
            if (value == null) return "null";

            // Handle JSON array fields (ViewTeams, ViewUsers, OperateTeams, OperateUsers)
            if (fieldName.Equals("ViewTeams", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("ViewUsers", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("OperateTeams", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("OperateUsers", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var jsonStr = value.ToString();
                    if (string.IsNullOrWhiteSpace(jsonStr))
                    {
                        return string.Empty;
                    }

                    List<string> items;

                    // Use ParseTeamList for Teams, ParseUserList for Users
                    if (fieldName.Equals("ViewTeams", StringComparison.OrdinalIgnoreCase) ||
                        fieldName.Equals("OperateTeams", StringComparison.OrdinalIgnoreCase))
                    {
                        items = ParseTeamList(jsonStr);
                    }
                    else
                    {
                        items = ParseUserList(jsonStr);
                    }

                    if (items.Any())
                    {
                        // For Teams, try to get team names if possible
                        if ((fieldName.Equals("ViewTeams", StringComparison.OrdinalIgnoreCase) ||
                             fieldName.Equals("OperateTeams", StringComparison.OrdinalIgnoreCase)) &&
                            _userService != null)
                        {
                            try
                            {
                                var tenantId = _userContext?.TenantId ?? "999";
                                var teamNameMap = await _userService.GetTeamNamesByIdsAsync(items, tenantId);
                                if (teamNameMap != null && teamNameMap.Any())
                                {
                                    var teamNames = items.Select(id =>
                                        teamNameMap.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) ? name : id
                                    ).ToList();
                                    return string.Join(", ", teamNames);
                                }
                            }
                            catch
                            {
                                // Fallback to IDs
                            }
                        }

                        // For Users, try to get user names if possible
                        if ((fieldName.Equals("ViewUsers", StringComparison.OrdinalIgnoreCase) ||
                             fieldName.Equals("OperateUsers", StringComparison.OrdinalIgnoreCase)) &&
                            _userService != null)
                        {
                            try
                            {
                                var userNames = await GetUserNamesByIdsAsync(items);
                                if (userNames.Any())
                                {
                                    return string.Join(", ", userNames);
                                }
                            }
                            catch
                            {
                                // Fallback to IDs
                            }
                        }

                        return string.Join(", ", items);
                    }

                    // Return empty string for empty arrays (don't display [])
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse {FieldName} array for display", fieldName);
                    // Fallback to default display
                }
            }

            // Handle ViewPermissionMode enum
            if (fieldName.Equals("ViewPermissionMode", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var enumValue = value.ToString();
                    if (int.TryParse(enumValue, out var intValue))
                    {
                        return GetViewPermissionModeDisplayName(intValue);
                    }
                }
                catch
                {
                    // Fallback to default display
                }
            }

            // Handle ViewPermissionSubjectType and OperatePermissionSubjectType
            if (fieldName.Equals("ViewPermissionSubjectType", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("OperatePermissionSubjectType", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var typeValue = value.ToString();
                    if (int.TryParse(typeValue, out var intValue))
                    {
                        return intValue == 1 ? "Team" : intValue == 2 ? "User" : typeValue;
                    }
                }
                catch
                {
                    // Fallback to default display
                }
            }

            // Default display
            var displayValue = GetDisplayValue(value, fieldName);
            return $"'{displayValue}'";
        }

        /// <summary>
        /// Get friendly field names for display
        /// </summary>
        private string GetFriendlyFieldName(string fieldName)
        {
            return fieldName switch
            {
                "LeadName" => "Customer Name",
                "CaseCode" => "Case Code",
                "WorkflowId" => "Workflow",
                "WorkflowName" => "Workflow",
                "Status" => "Status",
                "Priority" => "Priority",
                "LifeCycleStageName" => "Life Cycle Stage",
                "ContactPerson" => "Contact Name",
                "ContactEmail" => "Contact Email",
                "CurrentStageId" => "Current Stage",
                "Ownership" => "Ownership",
                "OwnershipName" => "Ownership",
                "ViewPermissionMode" => "View Permission Mode",
                "ViewTeams" => "View Teams",
                "ViewUsers" => "View Users",
                "ViewPermissionSubjectType" => "View Permission Subject Type",
                "OperateTeams" => "Operate Teams",
                "OperateUsers" => "Operate Users",
                "OperatePermissionSubjectType" => "Operate Permission Subject Type",
                _ => fieldName
            };
        }

        /// <summary>
        /// Log onboarding update operation
        /// </summary>
        public async Task<bool> LogOnboardingUpdateAsync(long onboardingId, string onboardingName, string beforeData, string afterData, List<string> changedFields, string extendedData = null)
        {
            if (!HasMeaningfulValueChangeEnhanced(beforeData, afterData))
            {
                _logger.LogDebug("Skipping operation log for onboarding {OnboardingId} as there's no meaningful value change", onboardingId);
                return true;
            }

            try
            {
                var operationTitle = $"Case Updated: {onboardingName}";
                var operationDescription = await BuildOnboardingUpdateDescriptionAsync(
                    onboardingName,
                    beforeData: beforeData,
                    afterData: afterData,
                    changedFields: changedFields
                );

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        ChangedFieldsCount = changedFields?.Count ?? 0,
                        UpdatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseUpdate,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    beforeData: beforeData,
                    afterData: afterData,
                    changedFields: changedFields != null ? JsonSerializer.Serialize(changedFields) : null,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding update operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Build enhanced operation description for onboarding update with all field changes
        /// </summary>
        private async Task<string> BuildOnboardingUpdateDescriptionAsync(
            string onboardingName,
            string beforeData,
            string afterData,
            List<string> changedFields)
        {
            var description = $"Case '{onboardingName}' has been updated by {GetOperatorDisplayName()}";

            if (changedFields == null || !changedFields.Any())
            {
                return description;
            }

            try
            {
                var beforeJson = JsonSerializer.Deserialize<Dictionary<string, object>>(beforeData);
                var afterJson = JsonSerializer.Deserialize<Dictionary<string, object>>(afterData);

                if (beforeJson == null || afterJson == null)
                {
                    return description;
                }

                var changeList = new List<string>();

                foreach (var field in changedFields)
                {
                    // Skip CurrentStageId and LifeCycleStageId fields
                    if (field.Equals("CurrentStageId", StringComparison.OrdinalIgnoreCase) ||
                        field.Equals("LifeCycleStageId", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fieldName = GetFriendlyFieldName(field);
                    var beforeValue = beforeJson.TryGetValue(field, out var bv) ? bv : null;
                    var afterValue = afterJson.TryGetValue(field, out var av) ? av : null;

                    // Handle ViewTeams and OperateTeams
                    if (field.Equals("ViewTeams", StringComparison.OrdinalIgnoreCase) ||
                        field.Equals("OperateTeams", StringComparison.OrdinalIgnoreCase))
                    {
                        var beforeTeams = ParseTeamList(beforeValue?.ToString());
                        var afterTeams = ParseTeamList(afterValue?.ToString());
                        var teamChanges = await GetTeamChangesAsync(beforeTeams, afterTeams, field.ToLower().Contains("view") ? "view" : "operate");
                        if (!string.IsNullOrEmpty(teamChanges))
                        {
                            changeList.Add(teamChanges);
                        }
                        continue;
                    }

                    // Handle ViewUsers and OperateUsers
                    if (field.Equals("ViewUsers", StringComparison.OrdinalIgnoreCase) ||
                        field.Equals("OperateUsers", StringComparison.OrdinalIgnoreCase))
                    {
                        var beforeUsers = ParseUserList(beforeValue?.ToString());
                        var afterUsers = ParseUserList(afterValue?.ToString());
                        var userChanges = await GetUserChangesAsync(beforeUsers, afterUsers, field.ToLower().Contains("view") ? "view" : "operate");
                        if (!string.IsNullOrEmpty(userChanges))
                        {
                            changeList.Add(userChanges);
                        }
                        continue;
                    }

                    // Handle ViewPermissionMode
                    if (field.Equals("ViewPermissionMode", StringComparison.OrdinalIgnoreCase))
                    {
                        var beforeStr = GetViewPermissionModeDisplayName(beforeValue);
                        var afterStr = GetViewPermissionModeDisplayName(afterValue);
                        changeList.Add($"{fieldName} from '{beforeStr}' to '{afterStr}'");
                        continue;
                    }

                    // Handle ViewPermissionSubjectType and OperatePermissionSubjectType
                    if (field.Equals("ViewPermissionSubjectType", StringComparison.OrdinalIgnoreCase) ||
                        field.Equals("OperatePermissionSubjectType", StringComparison.OrdinalIgnoreCase))
                    {
                        var beforeStr = GetPermissionSubjectTypeDisplayName(beforeValue);
                        var afterStr = GetPermissionSubjectTypeDisplayName(afterValue);
                        changeList.Add($"{fieldName} from '{beforeStr}' to '{afterStr}'");
                        continue;
                    }

                    // Handle Ownership - use OwnershipName instead of ID
                    if (field.Equals("Ownership", StringComparison.OrdinalIgnoreCase))
                    {
                        var beforeOwnershipName = beforeJson.TryGetValue("OwnershipName", out var bon) ? bon?.ToString() : null;
                        var afterOwnershipName = afterJson.TryGetValue("OwnershipName", out var aon) ? aon?.ToString() : null;

                        // Use OwnershipName if available, otherwise fallback to ID
                        var beforeStr = !string.IsNullOrEmpty(beforeOwnershipName) ? beforeOwnershipName : (beforeValue?.ToString() ?? "null");
                        var afterStr = !string.IsNullOrEmpty(afterOwnershipName) ? afterOwnershipName : (afterValue?.ToString() ?? "null");

                        changeList.Add($"{fieldName} from '{beforeStr}' to '{afterStr}'");
                        continue;
                    }

                    // Handle WorkflowId - use WorkflowName instead of ID
                    if (field.Equals("WorkflowId", StringComparison.OrdinalIgnoreCase))
                    {
                        var beforeWorkflowName = beforeJson.TryGetValue("WorkflowName", out var bwn) ? bwn?.ToString() : null;
                        var afterWorkflowName = afterJson.TryGetValue("WorkflowName", out var awn) ? awn?.ToString() : null;

                        // Use WorkflowName if available, otherwise fallback to ID
                        var beforeStr = !string.IsNullOrEmpty(beforeWorkflowName) ? beforeWorkflowName : (beforeValue?.ToString() ?? "null");
                        var afterStr = !string.IsNullOrEmpty(afterWorkflowName) ? afterWorkflowName : (afterValue?.ToString() ?? "null");

                        changeList.Add($"{fieldName} from '{beforeStr}' to '{afterStr}'");
                        continue;
                    }

                    // Default handling for other fields
                    var beforeDisplay = GetDisplayValueForUpdate(beforeValue, field);
                    var afterDisplay = GetDisplayValueForUpdate(afterValue, field);
                    changeList.Add($"{fieldName} from {beforeDisplay} to {afterDisplay}");
                }

                if (changeList.Any())
                {
                    description += $". Changes: {string.Join(", ", changeList)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build onboarding update description");
            }

            return description;
        }

        /// <summary>
        /// Get display value for update operation
        /// </summary>
        private string GetDisplayValueForUpdate(object value, string fieldName)
        {
            if (value == null) return "'null'";
            return $"'{GetDisplayValue(value, fieldName)}'";
        }

        /// <summary>
        /// Get permission subject type display name
        /// </summary>
        private string GetPermissionSubjectTypeDisplayName(object value)
        {
            if (value == null) return "null";
            if (int.TryParse(value.ToString(), out var intValue))
            {
                return intValue == 1 ? "Team" : intValue == 2 ? "User" : value.ToString();
            }
            return value.ToString();
        }

        /// <summary>
        /// Parse user list from JSON string (handles double-encoded JSON)
        /// </summary>
        private List<string> ParseUserList(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return new List<string>();
            try
            {
                var trimmedData = jsonString.Trim();

                // Handle double-encoded JSON string (e.g., "\"[\\\"123\\\",\\\"456\\\"]\"")
                // First, try to deserialize as a JSON string to get the actual JSON array string
                if (trimmedData.StartsWith("\"") && trimmedData.EndsWith("\""))
                {
                    try
                    {
                        var unescapedJson = JsonSerializer.Deserialize<string>(trimmedData);
                        if (!string.IsNullOrWhiteSpace(unescapedJson))
                        {
                            trimmedData = unescapedJson;
                            _logger.LogDebug("Unescaped double-encoded user JSON: {UnescapedJson}", trimmedData);
                        }
                    }
                    catch
                    {
                        // If deserialization fails, continue with original data
                        _logger.LogDebug("Failed to unescape as double-encoded JSON, using original data");
                    }
                }

                // Try to parse as JSON array
                if (trimmedData.StartsWith("["))
                {
                    var users = JsonSerializer.Deserialize<List<string>>(trimmedData);
                    if (users != null)
                    {
                        _logger.LogDebug("Successfully parsed {Count} users from JSON array", users.Count);
                        return users.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }
                }

                // Fallback: treat as comma-separated string
                var userList = trimmedData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .Where(u => !string.IsNullOrEmpty(u))
                    .ToList();

                _logger.LogDebug("Parsed {Count} users from comma-separated string", userList.Count);
                return userList;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse user list: {UserJson}", jsonString);
                return new List<string>();
            }
        }

        /// <summary>
        /// Get user changes description with user names
        /// </summary>
        private async Task<string> GetUserChangesAsync(List<string> beforeUsers, List<string> afterUsers, string permissionType)
        {
            var added = afterUsers.Except(beforeUsers).ToList();
            var removed = beforeUsers.Except(afterUsers).ToList();

            // Get friendly permission type name for display
            var permissionTypeDisplay = permissionType.ToLower() == "view" ? "View Users" : "Operate Users";

            var changes = new List<string>();

            // Get user names for added users
            if (added.Any())
            {
                var addedUserNames = await GetUserNamesByIdsAsync(added);
                if (addedUserNames.Any())
                {
                    changes.Add($"added {string.Join(", ", addedUserNames)} to {permissionTypeDisplay}");
                }
                else
                {
                    changes.Add($"added {added.Count} user(s) to {permissionTypeDisplay}");
                }
            }

            // Get user names for removed users
            if (removed.Any())
            {
                var removedUserNames = await GetUserNamesByIdsAsync(removed);
                if (removedUserNames.Any())
                {
                    changes.Add($"removed {string.Join(", ", removedUserNames)} from {permissionTypeDisplay}");
                }
                else
                {
                    changes.Add($"removed {removed.Count} user(s) from {permissionTypeDisplay}");
                }
            }

            if (changes.Any())
            {
                return string.Join(", ", changes);
            }

            return string.Empty;
        }

        /// <summary>
        /// Get user names by IDs
        /// </summary>
        private async Task<List<string>> GetUserNamesByIdsAsync(List<string> userIds)
        {
            if (userIds == null || !userIds.Any() || _userService == null)
            {
                return new List<string>();
            }

            try
            {
                var tenantId = _userContext?.TenantId ?? "999";
                var userIdsLong = userIds.Where(id => long.TryParse(id, out _))
                    .Select(id => long.Parse(id))
                    .ToList();

                if (userIdsLong.Any())
                {
                    var users = await _userService.GetUsersByIdsAsync(userIdsLong, tenantId);
                    if (users != null && users.Any())
                    {
                        var userMap = users.ToDictionary(u => u.Id.ToString(), u =>
                            !string.IsNullOrEmpty(u.Username) ? u.Username :
                            !string.IsNullOrEmpty(u.Email) ? u.Email :
                            u.Id.ToString());

                        return userIds
                            .Select(id => userMap.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name)
                                ? name
                                : id)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user names for IDs: {UserIds}", string.Join(", ", userIds));
            }

            return new List<string>();
        }

        /// <summary>
        /// Log onboarding delete operation
        /// </summary>
        public async Task<bool> LogOnboardingDeleteAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Deleted: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been deleted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        DeletedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseDelete,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding delete operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        #endregion

        #region Onboarding Status Operations

        /// <summary>
        /// Log onboarding start operation
        /// </summary>
        public async Task<bool> LogOnboardingStartAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Started: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been started by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        StartedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseStart,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding start operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Log onboarding pause operation
        /// </summary>
        public async Task<bool> LogOnboardingPauseAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Paused: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been paused by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        PausedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CasePause,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding pause operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Log onboarding resume operation
        /// </summary>
        public async Task<bool> LogOnboardingResumeAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Resumed: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been resumed by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        ResumedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseResume,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding resume operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Log onboarding abort operation
        /// </summary>
        public async Task<bool> LogOnboardingAbortAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Aborted: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been aborted by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        AbortedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseAbort,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding abort operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Log onboarding reactivate operation
        /// </summary>
        public async Task<bool> LogOnboardingReactivateAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Reactivated: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been reactivated by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        ReactivatedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseReactivate,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding reactivate operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Log onboarding force complete operation
        /// </summary>
        public async Task<bool> LogOnboardingForceCompleteAsync(long onboardingId, string onboardingName, string reason = null, string extendedData = null)
        {
            try
            {
                var operationTitle = $"Case Force Completed: {onboardingName}";
                var operationDescription = $"Case '{onboardingName}' has been force completed by {GetOperatorDisplayName()}";

                if (!string.IsNullOrEmpty(reason))
                {
                    operationDescription += $" with reason: {reason}";
                }

                if (string.IsNullOrEmpty(extendedData))
                {
                    extendedData = JsonSerializer.Serialize(new
                    {
                        OnboardingId = onboardingId,
                        OnboardingName = onboardingName,
                        Reason = reason,
                        ForceCompletedAt = FormatUsDateTime(DateTimeOffset.UtcNow)
                    });
                }

                return await LogOperationAsync(
                    OperationTypeEnum.CaseForceComplete,
                    BusinessModuleEnum.Onboarding,
                    onboardingId,
                    onboardingId,
                    null,
                    operationTitle,
                    operationDescription,
                    extendedData: extendedData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log onboarding force complete operation for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Get operation logs from database (abstract method implementation)
        /// </summary>
        protected override async Task<PagedResult<OperationChangeLogOutputDto>> GetOperationLogsFromDatabaseAsync(
            long? onboardingId,
            long? stageId,
            OperationTypeEnum? operationType,
            int pageIndex,
            int pageSize)
        {
            try
            {
                var logs = new List<Domain.Entities.OW.OperationChangeLog>();

                // Get logs based on filters
                if (onboardingId.HasValue)
                {
                    logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId.Value);
                    logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Onboarding.ToString()).ToList();
                }
                else
                {
                    // Get all onboarding-related logs
                    logs = await _operationChangeLogRepository.GetByBusinessAsync(BusinessModuleEnum.Onboarding.ToString(), 0);
                }

                // Apply operation type filter
                if (operationType.HasValue)
                {
                    logs = logs.Where(x => x.OperationType == operationType.ToString()).ToList();
                }

                // Apply pagination
                var totalCount = logs.Count;
                var pagedLogs = logs
                    .OrderByDescending(x => x.OperationTime)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var outputDtos = pagedLogs.Select(MapToOutputDto).ToList();

                return new PagedResult<OperationChangeLogOutputDto>
                {
                    Items = outputDtos,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get onboarding operation logs from database");
                return new PagedResult<OperationChangeLogOutputDto>();
            }
        }

        /// <summary>
        /// Get onboarding logs
        /// </summary>
        public async Task<PagedResult<OperationChangeLogOutputDto>> GetOnboardingLogsAsync(long onboardingId, int pageIndex = 1, int pageSize = 20)
        {
            return await GetOperationLogsAsync(onboardingId, null, null, pageIndex, pageSize);
        }

        /// <summary>
        /// Get onboarding operation statistics
        /// </summary>
        public async Task<Dictionary<string, int>> GetOnboardingOperationStatisticsAsync(long onboardingId)
        {
            try
            {
                var logs = await _operationChangeLogRepository.GetByOnboardingIdAsync(onboardingId);
                logs = logs.Where(x => x.BusinessModule == BusinessModuleEnum.Onboarding.ToString()).ToList();

                return logs.GroupBy(x => x.OperationType)
                          .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get onboarding operation statistics for onboarding {OnboardingId}", onboardingId);
                return new Dictionary<string, int>();
            }
        }

        #endregion

        /// <summary>
        /// Format date time in US format (MM/dd/yyyy hh:mm tt)
        /// </summary>
        private string FormatUsDateTime(DateTimeOffset dateTime)
        {
            return dateTime.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        }
    }
}

