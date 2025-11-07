using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using FlowFlex.Application.Contracts.Dtos.OW.User;


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
            if (!results.Any() || !entities.Any()) return;

            // Auto-generate CaseCode for legacy data (batch processing)
            var entitiesWithoutCaseCode = entities.Where(e => string.IsNullOrWhiteSpace(e.CaseCode)).ToList();
            if (entitiesWithoutCaseCode.Any())
            {
                foreach (var entity in entitiesWithoutCaseCode)
                {
                    await EnsureCaseCodeAsync(entity);
                }
            }

            // Batch get related data to avoid N+1 queries
            var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(entities);
            var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
            var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);
            var stageEstimatedDaysDict = stages.ToDictionary(s => s.Id, s => s.EstimatedDuration);

            // 馃殌 PERFORMANCE OPTIMIZATION: Batch check permissions using in-memory data (no DB queries)
            var userId = _userContext?.UserId;
            bool isSystemAdmin = _userContext?.IsSystemAdmin == true;
            bool isTenantAdmin = _userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId);
            Dictionary<long, PermissionInfoDto> permissions = null;

            if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var userIdLong))
            {
                // Pre-check module permissions once (batch optimization)
                bool canViewCases = await _permissionService.CheckGroupPermissionAsync(userIdLong, PermissionConsts.Case.Read);
                bool canOperateCases = await _permissionService.CheckGroupPermissionAsync(userIdLong, PermissionConsts.Case.Update);

                // Get user teams once (avoid repeated calls)
                var userTeams = _permissionService.GetUserTeamIds();
                var userTeamLongs = userTeams?.Select(t => long.TryParse(t, out var tid) ? tid : 0).Where(t => t > 0).ToList() ?? new List<long>();

                // 馃攧 Use unified CasePermissionService for permission checks
                permissions = new Dictionary<long, PermissionInfoDto>();

                // Create entity lookup for fast access
                var entityDict = entities.ToDictionary(e => e.Id);

                foreach (var result in results)
                {
                    if (!entityDict.TryGetValue(result.Id, out var entity))
                    {
                        // Fallback: entity not found, deny access
                        permissions[result.Id] = new PermissionInfoDto { CanView = false, CanOperate = false };
                        continue;
                    }

                    // 鉁?Use unified CasePermissionService - includes admin bypass
                    var viewResult = await _casePermissionService.CheckCasePermissionAsync(
                        entity, userIdLong, PermissionOperationType.View);

                    bool canOperateCase = false;
                    if (viewResult.Success && viewResult.CanView)
                    {
                        var operateResult = await _casePermissionService.CheckCasePermissionAsync(
                            entity, userIdLong, PermissionOperationType.Operate);
                        canOperateCase = operateResult.Success && operateResult.CanOperate;
                    }

                    permissions[result.Id] = new PermissionInfoDto
                    {
                        CanView = canViewCases && viewResult.Success && viewResult.CanView,
                        CanOperate = canOperateCases && canOperateCase
                    };
                }
            }

            // Populate workflow and stage names, calculate current stage end time, and check permissions
            foreach (var result in results)
            {
                result.WorkflowName = workflowDict.GetValueOrDefault(result.WorkflowId);

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

                // currentStageStartTime 鍙彇 startTime锛堟棤鍒欎负null锛?
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
                        // currentStageEndTime 浼樺厛绾? customEndTime > endTime > (startTime+estimatedDays) > null
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
                            // 涓夌骇浼樺厛锛歫son.customEstimatedDays > json.estimatedDays > stageDict
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
                // 鍗曠嫭鎺ㄧ畻 currentStageEndTime鈥斺€斾粎褰搒tartTime鍜宔stimatedDays閮藉瓨鍦?
                if (result.CurrentStageEndTime == null && result.CurrentStageStartTime.HasValue && (estimatedDays.HasValue && estimatedDays.Value > 0))
                {
                    result.CurrentStageEndTime = result.CurrentStageStartTime.Value.AddDays(estimatedDays.Value);
                }

                if (result.CurrentStageId.HasValue)
                {
                    result.CurrentStageName = stageDict.GetValueOrDefault(result.CurrentStageId.Value);

                    // Fallback: if name missing from dictionary, fetch directly
                    if (string.IsNullOrEmpty(result.CurrentStageName))
                    {
                        try
                        {
                            var stageNameFallback = await _stageRepository.GetByIdAsync(result.CurrentStageId.Value);
                            if (!string.IsNullOrEmpty(stageNameFallback?.Name))
                            {
                                result.CurrentStageName = stageNameFallback.Name;
                                LoggingExtensions.WriteLine($"[DEBUG] PopulateOnboardingOutputDto - Fallback CurrentStageName='{result.CurrentStageName}' for Onboarding {result.Id}");
                            }
                        }
                        catch { }
                    }

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

                    // Fallback: if still missing, fetch stage directly
                    if ((!result.CurrentStageEstimatedDays.HasValue || result.CurrentStageEstimatedDays.Value <= 0) && result.CurrentStageId.HasValue)
                    {
                        try
                        {
                            var stageFallback = await _stageRepository.GetByIdAsync(result.CurrentStageId.Value);
                            if (stageFallback?.EstimatedDuration != null && stageFallback.EstimatedDuration > 0)
                            {
                                result.CurrentStageEstimatedDays = stageFallback.EstimatedDuration;
                                LoggingExtensions.WriteLine($"[DEBUG] PopulateOnboardingOutputDto - Fallback EstimatedDays from Stage fetch: {result.CurrentStageEstimatedDays} for Onboarding {result.Id}");
                            }
                        }
                        catch { }
                    }

                    // End time already derived strictly from stagesProgress above
                }
                else
                {
                    // Log when CurrentStageId is still null after fallback attempt
                    LoggingExtensions.WriteLine($"[WARNING] PopulateOnboardingOutputDto - CurrentStageId is null for Onboarding {result.Id}, StagesProgress count: {result.StagesProgress?.Count ?? 0}");
                }

                // Set IsDisabled and Permission fields
                if (isSystemAdmin)
                {
                    result.IsDisabled = false; // System Admin can operate on all cases
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                else if (isTenantAdmin)
                {
                    result.IsDisabled = false; // Tenant Admin can operate on all cases in their tenant
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = true,
                        CanOperate = true,
                        ErrorMessage = null
                    };
                }
                else if (permissions != null && permissions.ContainsKey(result.Id))
                {
                    result.Permission = permissions[result.Id];
                    result.IsDisabled = !result.Permission.CanOperate;
                }
                else
                {
                    result.IsDisabled = true; // No user context, disable by default
                    result.Permission = new Application.Contracts.Dtos.OW.Permission.PermissionInfoDto
                    {
                        CanView = false,
                        CanOperate = false,
                        ErrorMessage = "User not authenticated"
                    };
                }
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

