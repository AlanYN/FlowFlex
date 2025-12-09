using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using System.Linq;

namespace FlowFlex.Application.Services.Action.Executors
{
    /// <summary>
    /// System action executor - handles predefined system actions
    /// </summary>
    public class SystemActionExecutor : IActionExecutor
    {
        private readonly ILogger<SystemActionExecutor> _logger;
        private readonly IOnboardingService _onboardingService;
        private readonly IStageService _stageService;
        private readonly UserContext _userContext;

        public SystemActionExecutor(
            ILogger<SystemActionExecutor> logger,
            IOnboardingService onboardingService,
            IStageService stageService,
            UserContext userContext)
        {
            _logger = logger;
            _onboardingService = onboardingService;
            _stageService = stageService;
            _userContext = userContext;
        }

        public ActionTypeEnum ActionType => ActionTypeEnum.System;

        public async Task<object> ExecuteAsync(string config, object triggerContext)
        {
            _logger.LogInformation("Executing System action with config: {Config}", config);

            try
            {
                var configJson = JObject.Parse(config);
                var actionName = configJson["actionName"]?.ToString();

                if (string.IsNullOrEmpty(actionName))
                {
                    throw new ArgumentException("System action must specify 'actionName' in configuration");
                }

                switch (actionName.ToLower())
                {
                    case "completestage":
                        return await ExecuteCompleteStageAsync(configJson, triggerContext);

                    case "movetostage":
                        return await ExecuteMoveToStageAsync(configJson, triggerContext);

                    case "assignonboarding":
                        return await ExecuteAssignOnboardingAsync(configJson, triggerContext);

                    default:
                        throw new NotSupportedException($"System action '{actionName}' is not supported");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON configuration for System action");
                throw new ArgumentException("Invalid JSON configuration for System action", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing System action");
                throw;
            }
        }

        /// <summary>
        /// Execute CompleteStage system action
        /// </summary>
        private async Task<object> ExecuteCompleteStageAsync(JObject config, object triggerContext)
        {
            _logger.LogInformation("Executing CompleteStage system action");

            // Extract parameters from config
            var stageId = config["stageId"]?.ToObject<long>();
            var onboardingId = config["onboardingId"]?.ToObject<long>();
            var completionNotes = config["completionNotes"]?.ToString() ?? "Completed by system action";
            var autoMoveToNext = config["autoMoveToNext"]?.ToObject<bool>() ?? true;
            var useValidationApi = config["useValidationApi"]?.ToObject<bool>() ?? false;

            // Add operator information to completion notes
            var operatorInfo = _userContext?.UserName ?? "System";
            var enhancedNotes = $"{completionNotes} (Operator: {operatorInfo})";

            // Try to extract from trigger context if not in config
            if (triggerContext != null)
            {
                var contextJson = JToken.FromObject(triggerContext);

                if (!stageId.HasValue)
                {
                    // Try different case variations and prioritize user-specified StageId over CurrentStageId
                    stageId = contextJson["StageId"]?.ToObject<long>() ??
                              contextJson["stageId"]?.ToObject<long>() ??
                              contextJson["CompletedStageId"]?.ToObject<long>() ??
                              contextJson["CurrentStageId"]?.ToObject<long>();
                }

                if (!onboardingId.HasValue)
                    onboardingId = contextJson["onboardingId"]?.ToObject<long>() ?? contextJson["Id"]?.ToObject<long>() ?? contextJson["OnboardingId"]?.ToObject<long>();
            }

            if (!stageId.HasValue || !onboardingId.HasValue)
            {
                throw new ArgumentException("CompleteStage action requires 'stageId' and 'onboardingId' parameters");
            }

            // Check if Onboarding is already completed before attempting to complete stage
            var onboarding = await _onboardingService.GetByIdAsync(onboardingId.Value);
            if (onboarding != null && string.Equals(onboarding.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Onboarding is already completed, skipping CompleteStage action: OnboardingId={OnboardingId}, Status={Status}",
                    onboardingId.Value, onboarding.Status);

                // Check and fix any data inconsistency for the specific stage
                await CheckAndFixStageCompletionInconsistencyAsync(onboardingId.Value, stageId.Value);

                return new { Success = false, Message = "Onboarding is already completed", AlreadyCompleted = true };
            }

            bool result;

            // Use internal completion method to avoid event publishing and prevent circular dependencies
            _logger.LogInformation("Using internal completion API (no events) for stage {StageId} in onboarding {OnboardingId} with operator: {Operator}",
                stageId.Value, onboardingId.Value, operatorInfo);

            // 使用专门的方法完成指定阶段，避免自动移动到下一阶段
            result = await _onboardingService.CompleteCurrentStageInternalAsync(onboardingId.Value, new FlowFlex.Application.Contracts.Dtos.OW.Onboarding.CompleteCurrentStageInputDto
            {
                StageId = stageId.Value,
                CompletionNotes = enhancedNotes,
                ForceComplete = false,
                // 重要：阻止自动移动到下一阶段，严格按照用户指定的阶段执行
                PreventAutoMove = true,
                // System action 执行时发送邮件通知
                SendEmailNotification = true
            });

            // If autoMoveToNext is true and stage completion was successful, move to next stage
            if (result && autoMoveToNext)
            {
                try
                {
                    await _onboardingService.MoveToNextStageAsync(onboardingId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-move to next stage after completing stage {StageId} for onboarding {OnboardingId}", stageId.Value, onboardingId.Value);
                    // Don't fail the entire operation if auto-move fails
                }
            }

            return new
            {
                success = result,
                message = result ? "Stage completed successfully (internal - no events)" : "Failed to complete stage",
                timestamp = DateTimeOffset.UtcNow,
                stageId = stageId.Value,
                onboardingId = onboardingId.Value,
                completionNotes = enhancedNotes,
                autoMoveToNext = autoMoveToNext,
                operatorInfo = operatorInfo,
                internalCompletion = true // Indicates this was completed without event publishing
            };
        }

        /// <summary>
        /// 检查和修复阶段完成状态不一致的问题
        /// </summary>
        private async Task CheckAndFixStageCompletionInconsistencyAsync(long onboardingId, long stageId)
        {
            try
            {
                if (stageId <= 0 || onboardingId <= 0) return;

                var updatedOnboardingDto = await _onboardingService.GetByIdAsync(onboardingId);

                if (updatedOnboardingDto?.StagesProgress != null)
                {
                    var stageProgress = updatedOnboardingDto.StagesProgress.FirstOrDefault(sp => sp.StageId == stageId);
                    if (stageProgress != null && !stageProgress.IsCompleted &&
                        string.Equals(updatedOnboardingDto.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("发现数据不一致: Onboarding 已完成但 Stage 标记为未完成, 正在修复: OnboardingId={OnboardingId}, StageId={StageId}, StageIsCompleted={StageIsCompleted}",
                            onboardingId, stageId, stageProgress.IsCompleted);

                        // Fix the inconsistency by marking the stage as completed
                        stageProgress.IsCompleted = true;
                        stageProgress.Status = "Completed";
                        stageProgress.CompletionTime = stageProgress.CompletionTime ?? DateTimeOffset.UtcNow;
                        stageProgress.CompletedBy = stageProgress.CompletedBy ?? _userContext?.UserName ?? "System";

                        // Handle UserId type conversion - UserId is string, CompletedById expects long?
                        if (!stageProgress.CompletedById.HasValue)
                        {
                            if (long.TryParse(_userContext?.UserId, out long userId))
                            {
                                stageProgress.CompletedById = userId;
                            }
                            else
                            {
                                stageProgress.CompletedById = null; // Default to null if conversion fails
                            }
                        }

                        // Note: We can't directly update the entity here without repository access
                        // This is logged for awareness - the ChecklistTaskCompletionService will handle the actual fix
                        _logger.LogInformation("Stage consistency issue detected in SystemActionExecutor: OnboardingId={OnboardingId}, StageId={StageId}",
                            onboardingId, stageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查和修复阶段完成状态不一致时发生错误: OnboardingId={OnboardingId}, StageId={StageId}",
                    onboardingId, stageId);
            }
        }

        /// <summary>
        /// Execute MoveToStage system action
        /// </summary>
        private async Task<object> ExecuteMoveToStageAsync(JObject config, object triggerContext)
        {
            _logger.LogInformation("Executing MoveToStage system action");

            // Extract parameters from config
            var targetStageId = config["targetStageId"]?.ToObject<long>();
            var onboardingId = config["onboardingId"]?.ToObject<long>();
            var notes = config["notes"]?.ToString() ?? "Moved by system action";

            // Try to extract from trigger context if not in config
            if (triggerContext != null)
            {
                var contextJson = JToken.FromObject(triggerContext);

                if (!targetStageId.HasValue)
                    targetStageId = contextJson["targetStageId"]?.ToObject<long>();

                if (!onboardingId.HasValue)
                    onboardingId = contextJson["onboardingId"]?.ToObject<long>() ?? contextJson["Id"]?.ToObject<long>();
            }

            if (!targetStageId.HasValue || !onboardingId.HasValue)
            {
                throw new ArgumentException("MoveToStage action requires 'targetStageId' and 'onboardingId' parameters");
            }

            // Execute stage move
            var result = await _onboardingService.MoveToStageAsync(onboardingId.Value, new FlowFlex.Application.Contracts.Dtos.OW.Onboarding.MoveToStageInputDto
            {
                StageId = targetStageId.Value,
                Reason = notes
            });

            return new
            {
                success = result,
                message = result ? "Moved to stage successfully" : "Failed to move to stage",
                timestamp = DateTimeOffset.UtcNow,
                targetStageId = targetStageId.Value,
                onboardingId = onboardingId.Value,
                notes = notes
            };
        }

        /// <summary>
        /// Execute AssignOnboarding system action
        /// </summary>
        private async Task<object> ExecuteAssignOnboardingAsync(JObject config, object triggerContext)
        {
            _logger.LogInformation("Executing AssignOnboarding system action");

            // Extract parameters from config
            var onboardingId = config["onboardingId"]?.ToObject<long>();
            var assigneeId = config["assigneeId"]?.ToObject<long>();
            var assigneeName = config["assigneeName"]?.ToString();
            var team = config["team"]?.ToString();
            var notes = config["notes"]?.ToString() ?? "Assigned by system action";

            // Try to extract from trigger context if not in config
            if (triggerContext != null)
            {
                var contextJson = JToken.FromObject(triggerContext);

                if (!onboardingId.HasValue)
                    onboardingId = contextJson["onboardingId"]?.ToObject<long>() ?? contextJson["Id"]?.ToObject<long>();

                if (!assigneeId.HasValue)
                    assigneeId = contextJson["assigneeId"]?.ToObject<long>();

                if (string.IsNullOrEmpty(assigneeName))
                    assigneeName = contextJson["assigneeName"]?.ToString();

                if (string.IsNullOrEmpty(team))
                    team = contextJson["team"]?.ToString();
            }

            if (!onboardingId.HasValue)
            {
                throw new ArgumentException("AssignOnboarding action requires 'onboardingId' parameter");
            }

            if (!assigneeId.HasValue)
            {
                throw new ArgumentException("AssignOnboarding action requires 'assigneeId' parameter");
            }

            // Execute onboarding assignment
            var result = await _onboardingService.AssignAsync(onboardingId.Value, new FlowFlex.Application.Contracts.Dtos.OW.Onboarding.AssignOnboardingInputDto
            {
                AssigneeId = assigneeId.Value,
                AssigneeName = assigneeName ?? "System User",
                Team = team,
                Reason = notes
            });

            return new
            {
                success = result,
                message = result ? "Onboarding assigned successfully" : "Failed to assign onboarding",
                timestamp = DateTimeOffset.UtcNow,
                onboardingId = onboardingId.Value,
                assigneeId = assigneeId.Value,
                assigneeName = assigneeName,
                team = team,
                notes = notes
            };
        }
    }
}