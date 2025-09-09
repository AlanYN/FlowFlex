using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;

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
                    stageId = contextJson["stageId"]?.ToObject<long>() ?? contextJson["CurrentStageId"]?.ToObject<long>() ?? contextJson["CompletedStageId"]?.ToObject<long>();

                if (!onboardingId.HasValue)
                    onboardingId = contextJson["onboardingId"]?.ToObject<long>() ?? contextJson["Id"]?.ToObject<long>() ?? contextJson["OnboardingId"]?.ToObject<long>();
            }

            if (!stageId.HasValue || !onboardingId.HasValue)
            {
                throw new ArgumentException("CompleteStage action requires 'stageId' and 'onboardingId' parameters");
            }

            bool result;

            // Choose API based on useValidationApi flag or action code pattern
            if (useValidationApi)
            {
                _logger.LogInformation("Using validation API (COMP-STG) for stage {StageId} in onboarding {OnboardingId} with operator: {Operator}",
                    stageId.Value, onboardingId.Value, operatorInfo);

                // Use the validation API that calls complete-stage-with-validation endpoint
                result = await _onboardingService.CompleteCurrentStageAsync(onboardingId.Value, new FlowFlex.Application.Contracts.Dtos.OW.Onboarding.CompleteCurrentStageInputDto
                {
                    StageId = stageId.Value,
                    CompletionNotes = enhancedNotes,
                    ForceComplete = false
                });
            }
            else
            {
                _logger.LogInformation("Using standard completion API for stage {StageId} in onboarding {OnboardingId} with operator: {Operator}",
                    stageId.Value, onboardingId.Value, operatorInfo);

                // Use standard completion API
                result = await _onboardingService.CompleteCurrentStageAsync(onboardingId.Value, new FlowFlex.Application.Contracts.Dtos.OW.Onboarding.CompleteCurrentStageInputDto
                {
                    StageId = stageId.Value,
                    CompletionNotes = enhancedNotes,
                    ForceComplete = false
                });
            }

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
                message = result ? "Stage completed successfully" : "Failed to complete stage",
                timestamp = DateTimeOffset.UtcNow,
                stageId = stageId.Value,
                onboardingId = onboardingId.Value,
                completionNotes = enhancedNotes,
                autoMoveToNext = autoMoveToNext,
                useValidationApi = useValidationApi,
                operatorInfo = operatorInfo
            };
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