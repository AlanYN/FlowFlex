using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;


namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Permission checking and management
    /// </summary>
    public partial class OnboardingService
    {
        private async Task<bool> CheckCaseOperatePermissionAsync(long caseId)
        {
            var userId = _userContext?.UserId;
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
            {
                return false;
            }

            var permissionResult = await _permissionService.CheckCaseAccessAsync(
                userIdLong,
                caseId,
                PermissionOperationType.Operate);

            return permissionResult.Success && permissionResult.CanOperate;
        }

        private async Task PopulateOnboardingOutputDtoAsync(List<OnboardingOutputDto> results, List<Onboarding> entities)
        {
            if (!results.Any() || !entities.Any())
            {
                _logger.LogDebug("PopulateOnboardingOutputDtoAsync - No results or entities to populate");
                return;
            }

            _logger.LogDebug(
                "PopulateOnboardingOutputDtoAsync - Processing {ResultCount} results and {EntityCount} entities",
                results.Count,
                entities.Count);

            // Step 1: Auto-generate CaseCode for legacy data (batch processing)
            await EnsureCaseCodesAsync(entities);

            // Step 2: Batch get related data to avoid N+1 queries
            var (workflowDict, stageDict, stageEstimatedDaysDict) = await GetRelatedDataDictionariesAsync(entities);

            // Step 3: Batch check permissions using in-memory data (no DB queries)
            var permissionContext = await BuildPermissionContextAsync(results, entities);

            // Step 4: Populate each result DTO
            foreach (var result in results)
            {
                PopulateOnboardingOutputDtoFields(result, workflowDict, stageDict, stageEstimatedDaysDict, permissionContext);
            }

            _logger.LogDebug("PopulateOnboardingOutputDtoAsync - Completed successfully");
        }

        /// <summary>
        /// Ensure CaseCode exists for all entities (batch processing)
        /// </summary>
        private async Task EnsureCaseCodesAsync(List<Onboarding> entities)
        {
            var entitiesWithoutCaseCode = entities.Where(e => string.IsNullOrWhiteSpace(e.CaseCode)).ToList();
            if (!entitiesWithoutCaseCode.Any())
            {
                return;
            }

            _logger.LogInformation(
                "Auto-generating CaseCode for {Count} legacy entities",
                entitiesWithoutCaseCode.Count);

            foreach (var entity in entitiesWithoutCaseCode)
            {
                await EnsureCaseCodeAsync(entity);
            }
        }

        /// <summary>
        /// Get related data dictionaries for fast lookup
        /// </summary>
        private async Task<(Dictionary<long, string> workflowDict, Dictionary<long, string> stageDict, Dictionary<long, decimal?> stageEstimatedDaysDict)>
            GetRelatedDataDictionariesAsync(List<Onboarding> entities)
        {
            var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(entities);

            var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
            var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);
            var stageEstimatedDaysDict = stages.ToDictionary(s => s.Id, s => s.EstimatedDuration);

            _logger.LogDebug(
                "GetRelatedDataDictionariesAsync - Loaded {WorkflowCount} workflows and {StageCount} stages",
                workflowDict.Count,
                stageDict.Count);

            return (workflowDict, stageDict, stageEstimatedDaysDict);
        }

        /// <summary>
        /// Build permission context for batch permission checking
        /// </summary>
        private async Task<PermissionBatchContext> BuildPermissionContextAsync(
            List<OnboardingOutputDto> results,
            List<Onboarding> entities)
        {
            var context = new PermissionBatchContext
            {
                UserId = _userContext?.UserId,
                IsSystemAdmin = _userContext?.IsSystemAdmin == true,
                IsTenantAdmin = _userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId)
            };

            if (string.IsNullOrEmpty(context.UserId) || !long.TryParse(context.UserId, out var userIdLong))
            {
                _logger.LogDebug("BuildPermissionContextAsync - No valid user context, skipping permission checks");
                context.Permissions = new Dictionary<long, PermissionInfoDto>();
                return context;
            }

            context.UserIdLong = userIdLong;

            // Pre-check module permissions once (batch optimization)
            context.CanViewCases = await _permissionService.CheckGroupPermissionAsync(userIdLong, PermissionConsts.Case.Read);
            context.CanOperateCases = await _permissionService.CheckGroupPermissionAsync(userIdLong, PermissionConsts.Case.Update);

            _logger.LogDebug(
                "BuildPermissionContextAsync - User {UserId} module permissions: CanView={CanView}, CanOperate={CanOperate}",
                userIdLong,
                context.CanViewCases,
                context.CanOperateCases);

            // Delegate to CasePermissionService for batch permission checking
            context.Permissions = await _casePermissionService.CheckBatchCasePermissionsAsync(
                entities,
                userIdLong,
                context.CanViewCases,
                context.CanOperateCases);

            return context;
        }

        /// <summary>
        /// Permission batch context for efficient permission checking
        /// </summary>
        private class PermissionBatchContext
        {
            public string UserId { get; set; }
            public long UserIdLong { get; set; }
            public bool IsSystemAdmin { get; set; }
            public bool IsTenantAdmin { get; set; }
            public bool CanViewCases { get; set; }
            public bool CanOperateCases { get; set; }
            public Dictionary<long, PermissionInfoDto> Permissions { get; set; }
        }

        /// <summary>
        /// Populate individual onboarding output DTO fields
        /// </summary>
        private void PopulateOnboardingOutputDtoFields(
            OnboardingOutputDto result,
            Dictionary<long, string> workflowDict,
            Dictionary<long, string> stageDict,
            Dictionary<long, decimal?> stageEstimatedDaysDict,
            PermissionBatchContext permissionContext)
        {
            // Populate workflow name
            result.WorkflowName = workflowDict.GetValueOrDefault(result.WorkflowId);

            // Populate permission info from context
            if (permissionContext.Permissions.TryGetValue(result.Id, out var permission))
            {
                result.Permission = permission;
                result.IsDisabled = !permission.CanOperate;
            }
            else
            {
                // Fallback: deny all permissions if not found in context
                result.Permission = new PermissionInfoDto { CanView = false, CanOperate = false };
                result.IsDisabled = true;
            }

            // IMPORTANT: If CurrentStageId is null but stagesProgress exists, try to get current stage from stagesProgress
            if (!result.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
            {
                var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.IsCurrent);
                if (currentStageProgress != null)
                {
                    result.CurrentStageId = currentStageProgress.StageId;
                    LoggingExtensions.WriteLine($"[DEBUG] PopulateOnboardingOutputDto - Recovered CurrentStageId from StagesProgress: {result.CurrentStageId} for Onboarding {result.Id}");
                }
            }

            // currentStageStartTime only takes startTime (null if none)
            result.CurrentStageStartTime = null;
            result.CurrentStageEndTime = null;
            double? estimatedDays = null;
            if (result.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
            {
                var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.StageId == result.CurrentStageId.Value);
                if (currentStageProgress != null)
                {
                    if (currentStageProgress.StartTime.HasValue)
                    {
                        result.CurrentStageStartTime = currentStageProgress.StartTime;
                    }
                    // currentStageEndTime priority: customEndTime > endTime > (startTime+estimatedDays) > null
                    if (currentStageProgress.CustomEndTime.HasValue)
                    {
                        result.CurrentStageEndTime = currentStageProgress.CustomEndTime.Value;
                    }
                    else if (currentStageProgress.EndTime.HasValue)
                    {
                        result.CurrentStageEndTime = currentStageProgress.EndTime.Value;
                    }
                    else
                    {
                        // Three-level priority: json.customEstimatedDays > json.estimatedDays > stageDict
                        estimatedDays = (double?)currentStageProgress.CustomEstimatedDays;
                        if (!estimatedDays.HasValue || estimatedDays.Value <= 0)
                        {
                            estimatedDays = (double?)currentStageProgress.EstimatedDays;
                            if ((!estimatedDays.HasValue || estimatedDays.Value <= 0) && result.CurrentStageId.HasValue)
                            {
                                // stageEstimatedDaysDict key: stageId -> EstimatedDuration
                                if (stageEstimatedDaysDict.TryGetValue(result.CurrentStageId.Value, out var val) && val != null && val > 0)
                                {
                                    estimatedDays = (double?)val;
                                }
                            }
                        }
                    }
                }
            }
            // Calculate currentStageEndTime separately - only when both startTime and estimatedDays exist
            if (result.CurrentStageEndTime == null && result.CurrentStageStartTime.HasValue && (estimatedDays.HasValue && estimatedDays.Value > 0))
            {
                result.CurrentStageEndTime = result.CurrentStageStartTime.Value.AddDays(estimatedDays.Value);
            }

            if (result.CurrentStageId.HasValue)
            {
                result.CurrentStageName = stageDict.GetValueOrDefault(result.CurrentStageId.Value);

                // IMPORTANT: Priority for EstimatedDays: customEstimatedDays > stage.EstimatedDuration
                var currentStageProgress = result.StagesProgress?.FirstOrDefault(sp => sp.StageId == result.CurrentStageId.Value);
                if (currentStageProgress != null && currentStageProgress.CustomEstimatedDays.HasValue && currentStageProgress.CustomEstimatedDays.Value > 0)
                {
                    result.CurrentStageEstimatedDays = currentStageProgress.CustomEstimatedDays.Value;
                }
                else
                {
                    result.CurrentStageEstimatedDays = stageEstimatedDaysDict.GetValueOrDefault(result.CurrentStageId.Value);
                }

                // End time already derived strictly from stagesProgress above
            }
            else
            {
                // Log when CurrentStageId is still null after fallback attempt
                LoggingExtensions.WriteLine($"[WARNING] PopulateOnboardingOutputDto - CurrentStageId is null for Onboarding {result.Id}, StagesProgress count: {result.StagesProgress?.Count ?? 0}");
            }
        }

        /// <summary>
        /// Validate that team IDs from JSON arrays exist in the team tree from UserService.
        /// Accepts JSON arrays (possibly double-encoded) like ["team1","team2"].
        /// Throws BusinessError if any invalid IDs are found.
        /// </summary>
        private async Task ValidateTeamSelectionsFromJsonAsync(string viewTeamsJson, string operateTeamsJson)
        {
            var viewTeams = ParseJsonArraySafe(viewTeamsJson) ?? new List<string>();
            var operateTeams = ParseJsonArraySafe(operateTeamsJson) ?? new List<string>();

            var needsValidation = (viewTeams.Any() || operateTeams.Any());
            if (!needsValidation)
            {
                return;
            }

            HashSet<string> allTeamIds;
            try
            {
                allTeamIds = await GetAllTeamIdsFromUserTreeAsync();
            }
            catch (Exception ex)
            {
                // Do not block the operation if IDM is unavailable; just log
                LoggingExtensions.WriteLine($"[TeamValidation] Skipped due to error fetching team tree: {ex.Message}");
                return;
            }

            // Add "Other" as a valid special team ID (for users without team assignment)
            allTeamIds.Add("Other");

            var invalid = new List<string>();
            invalid.AddRange(viewTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            invalid.AddRange(operateTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            invalid = invalid.Distinct(StringComparer.Ordinal).ToList();

            if (invalid.Any())
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"The following team IDs do not exist: {string.Join(", ", invalid)}");
            }
        }

        /// <summary>
        /// Get all valid team IDs from UserService team tree (excludes placeholder teams like 'Other').
        /// </summary>
        private async Task<HashSet<string>> GetAllTeamIdsFromUserTreeAsync()
        {
            var tree = await _userService.GetUserTreeAsync();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (tree == null || !tree.Any())
            {
                return ids;
            }

            void Traverse(IEnumerable<UserTreeNodeDto> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    if (string.Equals(node.Type, "team", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(node.Id) && !string.Equals(node.Id, "Other", StringComparison.Ordinal))
                        {
                            ids.Add(node.Id);
                        }
                    }
                    if (node.Children != null && node.Children.Any())
                    {
                        Traverse(node.Children);
                    }
                }
            }

            Traverse(tree);
            return ids;
        }

    }
}

