using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for managing onboarding stage operations
    /// Handles: MoveToNextStage, MoveToStage, CompleteCurrentStage, stage validation
    /// </summary>
    public class OnboardingStageManagementService : IOnboardingStageManagementService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingPermissionService _permissionService;
        private readonly IOnboardingStageProgressService _stageProgressService;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IOnboardingLogService _onboardingLogService;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IRulesEngineService _rulesEngineService;
        private readonly IConditionActionExecutor _conditionActionExecutor;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserContext _userContext;
        private readonly ILogger<OnboardingStageManagementService> _logger;

        public OnboardingStageManagementService(
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            IOnboardingPermissionService permissionService,
            IOnboardingStageProgressService stageProgressService,
            IOperationChangeLogService operationChangeLogService,
            IOnboardingLogService onboardingLogService,
            IOperatorContextService operatorContextService,
            IBackgroundTaskQueue backgroundTaskQueue,
            IRulesEngineService rulesEngineService,
            IConditionActionExecutor conditionActionExecutor,
            IEmailService emailService,
            IUserService userService,
            IHttpContextAccessor httpContextAccessor,
            UserContext userContext,
            ILogger<OnboardingStageManagementService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _stageProgressService = stageProgressService ?? throw new ArgumentNullException(nameof(stageProgressService));
            _operationChangeLogService = operationChangeLogService ?? throw new ArgumentNullException(nameof(operationChangeLogService));
            _onboardingLogService = onboardingLogService ?? throw new ArgumentNullException(nameof(onboardingLogService));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _rulesEngineService = rulesEngineService ?? throw new ArgumentNullException(nameof(rulesEngineService));
            _conditionActionExecutor = conditionActionExecutor ?? throw new ArgumentNullException(nameof(conditionActionExecutor));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _httpContextAccessor = httpContextAccessor;
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Helper Methods

        /// <summary>
        /// Get current user name from OperatorContextService
        /// </summary>
        private string GetCurrentUserName()
        {
            return _operatorContextService.GetOperatorDisplayName();
        }

        /// <summary>
        /// Get current user ID from OperatorContextService
        /// </summary>
        private long? GetCurrentUserId()
        {
            var id = _operatorContextService.GetOperatorId();
            return id == 0 ? null : id;
        }

        /// <summary>
        /// Get current UTC time normalized to start of day
        /// Uses shared utility method
        /// </summary>
        private static DateTimeOffset GetNormalizedUtcNow()
            => OnboardingSharedUtilities.GetNormalizedUtcNowOffset();

        /// <summary>
        /// Get current user email
        /// Uses shared utility method
        /// </summary>
        private string GetCurrentUserEmail()
            => OnboardingSharedUtilities.GetCurrentUserEmail(_userContext, _operatorContextService);

        #endregion


        #region Stage Operations

        /// <inheritdoc />
        public async Task<bool> MoveToNextStageAsync(long id)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);

            if (currentStageIndex == -1 || currentStageIndex >= orderedStages.Count - 1)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "No next stage available");
            }

            var nextStage = orderedStages[currentStageIndex + 1];
            await _onboardingRepository.UpdateStageAsync(id, nextStage.Id, nextStage.Order);

            // Update status to InProgress if it was Started
            if (entity.Status == "Started")
            {
                await _onboardingRepository.UpdateStatusAsync(id, "InProgress");
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stage = await _stageRepository.GetByIdAsync(input.StageId);
            if (stage == null || !stage.IsValid || stage.WorkflowId != entity.WorkflowId)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Stage not found or not belongs to current workflow");
            }

            return await _onboardingRepository.UpdateStageAsync(id, stage.Id, stage.Order);
        }

        #endregion


        #region Complete Stage Operations

        /// <inheritdoc />
        public async Task<bool> CompleteCurrentStageInternalAsync(long id, CompleteCurrentStageInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already completed");
            }

            // Ensure stages progress is initialized
            await _stageProgressService.EnsureStagesProgressInitializedAsync(entity);

            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();

            // Get target stage ID with backward compatibility
            long stageIdToComplete;
            try
            {
                stageIdToComplete = input.GetTargetStageId();
            }
            catch (ArgumentException ex)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid stage ID parameters: {ex.Message}");
            }

            // Find the stage to complete
            var stageToComplete = orderedStages.FirstOrDefault(s => s.Id == stageIdToComplete);
            if (stageToComplete == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage with ID {stageIdToComplete} not found in workflow");
            }

            // Validate if this stage can be completed
            var (canComplete, validationError) = await _stageProgressService.ValidateStageCanBeCompletedAsync(entity, stageIdToComplete);
            if (!canComplete && !input.ForceComplete)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }

            // Update stages progress
            await _stageProgressService.UpdateStagesProgressAsync(entity, stageIdToComplete, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);

            // Check if all stages are completed (including skipped stages as "done")
            var allStagesCompleted = entity.StagesProgress.All(sp => 
                sp.IsCompleted || 
                string.Equals(sp.Status, "Skipped", StringComparison.OrdinalIgnoreCase));

            if (allStagesCompleted)
            {
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            }
            else
            {
                // Ensure completion rate never decreases
                var previousRate = entity.CompletionRate;
                var newRate = _stageProgressService.CalculateCompletionRateByCompletedStages(entity.StagesProgress);
                entity.CompletionRate = Math.Max(previousRate, newRate);
                UpdateCurrentStageAfterCompletion(entity, stageToComplete, orderedStages, input.PreventAutoMove);

                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    OnboardingSharedUtilities.SafeAppendToNotes(entity, $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}");
                }
            }

            // Update stage tracking info
            UpdateStageTrackingInfo(entity);

            // Save changes
            var result = await SaveOnboardingChangesAsync(entity);

            // Send email notification if requested
            if (result && input.SendEmailNotification)
            {
                await SendStageCompletionEmailAsync(entity, stageToComplete);
            }

            return result;
        }

        #endregion


        #region Complete Current Stage (Public API)

        /// <inheritdoc />
        public async Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Ensure stages progress is properly initialized
            await _stageProgressService.EnsureStagesProgressInitializedAsync(entity);
            await SaveOnboardingChangesAsync(entity);

            // Get target stage ID
            long targetStageId;
            try
            {
                targetStageId = input.GetTargetStageId();
            }
            catch (ArgumentException ex)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid stage ID parameters: {ex.Message}");
            }

            if (entity.Status == "Completed")
            {
                return true;
            }

            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var totalStages = orderedStages.Count;

            // Find the stage to complete
            var stageToComplete = orderedStages.FirstOrDefault(x => x.Id == targetStageId);
            if (stageToComplete == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Specified stage not found in workflow");
            }

            // Validate if this stage can be completed
            var (canComplete, validationError) = await _stageProgressService.ValidateStageCanBeCompletedAsync(entity, stageToComplete.Id);
            if (!canComplete)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }

            // Update stages progress
            await _stageProgressService.UpdateStagesProgressAsync(entity, stageToComplete.Id, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);

            // Calculate new completion rate - ensure it never decreases
            var previousRate = entity.CompletionRate;
            var newRate = _stageProgressService.CalculateCompletionRateByCompletedStages(entity.StagesProgress);
            entity.CompletionRate = Math.Max(previousRate, newRate);
            var completedCount = entity.StagesProgress.Count(s => s.IsCompleted);

            // Check if all stages are completed (including skipped stages as "done")
            var allStagesCompleted = entity.StagesProgress.All(s => 
                s.IsCompleted || 
                string.Equals(s.Status, "Skipped", StringComparison.OrdinalIgnoreCase));
            if (allStagesCompleted)
            {
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;

                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    OnboardingSharedUtilities.SafeAppendToNotes(entity, $"[Onboarding Completed] Final stage '{stageToComplete.Name}' completed: {input.CompletionNotes}");
                }
            }
            else
            {
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                }

                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    OnboardingSharedUtilities.SafeAppendToNotes(entity, $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}");
                }
            }

            UpdateStageTrackingInfo(entity);
            var result = await SaveOnboardingChangesAsync(entity);

            if (result)
            {
                // Log stage completion
                await LogStageCompletionAsync(entity, stageToComplete, totalStages, completedCount, allStagesCompleted, input.CompletionNotes);

                // Evaluate and execute stage conditions
                bool conditionActionExecuted = false;
                if (!allStagesCompleted)
                {
                    conditionActionExecuted = await EvaluateAndExecuteStageConditionAsync(entity.Id, stageToComplete.Id);
                }

                // Auto-advance to next stage if no condition action was executed
                if (!allStagesCompleted && !conditionActionExecuted)
                {
                    await AutoAdvanceToNextStageAsync(entity, stageToComplete, orderedStages);
                }

                // Send email notification
                await SendStageCompletionEmailAsync(entity, stageToComplete);
            }

            return result;
        }

        #endregion


        #region Validation and Condition Evaluation

        /// <inheritdoc />
        public async Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(long onboardingId, long stageId)
        {
            var entity = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (entity == null || !entity.IsValid)
            {
                return (false, "Onboarding not found");
            }

            return await _stageProgressService.ValidateStageCanBeCompletedAsync(entity, stageId);
        }

        /// <inheritdoc />
        public async Task<bool> EvaluateAndExecuteStageConditionAsync(long onboardingId, long stageId)
        {
            _logger.LogDebug("Evaluating stage condition for OnboardingId={OnboardingId}, StageId={StageId}",
                onboardingId, stageId);

            try
            {
                var result = await _rulesEngineService.EvaluateAndExecuteWithTransactionAsync(
                    onboardingId,
                    stageId,
                    _conditionActionExecutor,
                    _operationChangeLogService);

                if (result.IsConditionMet)
                {
                    _logger.LogInformation("Stage condition met for OnboardingId={OnboardingId}, StageId={StageId}, ActionsExecuted={ActionCount}",
                        onboardingId, stageId, result.ActionResults?.Count ?? 0);

                    if (result.ActionResults != null && result.ActionResults.Any(a => a.Success))
                    {
                        return true;
                    }
                }
                else
                {
                    // Check if fallback actions were executed
                    if (result.ActionResults != null && result.ActionResults.Any(a => a.Success))
                    {
                        _logger.LogInformation("Stage condition not met but fallback actions executed for OnboardingId={OnboardingId}, StageId={StageId}",
                            onboardingId, stageId);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating stage condition for OnboardingId={OnboardingId}, StageId={StageId}",
                    onboardingId, stageId);
                return false;
            }
        }

        #endregion


        #region Private Helper Methods

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private void UpdateStageTrackingInfo(Onboarding entity)
            => OnboardingSharedUtilities.UpdateStageTrackingInfo(entity, _operatorContextService, _userContext);

        /// <summary>
        /// Update current stage after completion
        /// </summary>
        private void UpdateCurrentStageAfterCompletion(Onboarding entity, Stage completedStage, List<Stage> orderedStages, bool preventAutoMove)
        {
            if (entity.CurrentStageId == completedStage.Id)
            {
                var nextIncompleteStage = orderedStages
                    .Where(stage => stage.Order > completedStage.Order &&
                                   !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                    .OrderBy(stage => stage.Order)
                    .FirstOrDefault();

                if (nextIncompleteStage != null)
                {
                    entity.CurrentStageId = nextIncompleteStage.Id;
                    entity.CurrentStageOrder = nextIncompleteStage.Order;
                    entity.CurrentStageStartTime = GetNormalizedUtcNow();
                }
            }
            else if (!preventAutoMove)
            {
                var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == completedStage.Id)?.Order ?? 0;
                var nextIncompleteStage = orderedStages
                    .Where(stage => stage.Order > completedStageOrder &&
                                   !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                    .OrderBy(stage => stage.Order)
                    .FirstOrDefault();

                if (nextIncompleteStage != null)
                {
                    entity.CurrentStageId = nextIncompleteStage.Id;
                    entity.CurrentStageOrder = nextIncompleteStage.Order;
                    entity.CurrentStageStartTime = GetNormalizedUtcNow();
                }
            }
        }

        /// <summary>
        /// Auto-advance to next stage after completion
        /// </summary>
        private async Task AutoAdvanceToNextStageAsync(Onboarding entity, Stage completedStage, List<Stage> orderedStages)
        {
            entity = await _onboardingRepository.GetByIdAsync(entity.Id);
            
            // Load stages progress from JSON to prevent overwriting with empty array
            _stageProgressService.LoadStagesProgressFromJson(entity);
            
            var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);
            var nextStageIndex = currentStageIndex + 1;

            bool needsUpdate = false;

            if (entity.CurrentStageId == completedStage.Id && nextStageIndex < orderedStages.Count)
            {
                var nextStage = orderedStages[nextStageIndex];
                entity.CurrentStageId = nextStage.Id;
                entity.CurrentStageOrder = nextStage.Order;
                entity.CurrentStageStartTime = GetNormalizedUtcNow();
                needsUpdate = true;
            }
            else if (entity.CurrentStageId != completedStage.Id)
            {
                var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == completedStage.Id)?.Order ?? 0;
                var nextIncompleteStage = orderedStages
                    .Where(stage => stage.Order > completedStageOrder &&
                                   !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                    .OrderBy(stage => stage.Order)
                    .FirstOrDefault();

                if (nextIncompleteStage != null)
                {
                    entity.CurrentStageId = nextIncompleteStage.Id;
                    entity.CurrentStageOrder = nextIncompleteStage.Order;
                    entity.CurrentStageStartTime = GetNormalizedUtcNow();
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                await SaveOnboardingChangesAsync(entity);
            }
        }

        #endregion


        #region Save and Log Methods

        /// <summary>
        /// Save onboarding changes to database
        /// </summary>
        private async Task<bool> SaveOnboardingChangesAsync(Onboarding entity)
        {
            try
            {
                var db = _onboardingRepository.GetSqlSugarClient();

                entity.ModifyDate = DateTimeOffset.UtcNow;
                entity.ModifyBy = _operatorContextService.GetOperatorDisplayName();
                entity.ModifyUserId = _operatorContextService.GetOperatorId();

                // Serialize stages progress
                if (entity.StagesProgress != null)
                {
                    entity.StagesProgressJson = _stageProgressService.SerializeStagesProgress(entity.StagesProgress);
                }

                var result = await _onboardingRepository.UpdateAsync(entity,
                    it => new
                    {
                        it.CurrentStageId,
                        it.CurrentStageOrder,
                        it.Status,
                        it.CompletionRate,
                        it.ActualCompletionDate,
                        it.StageUpdatedById,
                        it.StageUpdatedBy,
                        it.StageUpdatedByEmail,
                        it.StageUpdatedTime,
                        it.CurrentStageStartTime,
                        it.Notes,
                        it.ModifyDate,
                        it.ModifyBy,
                        it.ModifyUserId
                    });

                // Update stages_progress_json separately with JSONB casting
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                    await db.Ado.ExecuteCommandAsync(progressSql, new
                    {
                        StagesProgressJson = entity.StagesProgressJson,
                        Id = entity.Id
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save onboarding changes for {OnboardingId}", entity.Id);
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to save onboarding changes: {ex.Message}");
            }
        }

        #endregion


        #region Logging Methods

        /// <summary>
        /// Log stage completion to operation change log
        /// </summary>
        private async Task LogStageCompletionAsync(Onboarding entity, Stage stage, int totalStages, int completedCount, bool allStagesCompleted, string completionNotes)
        {
            try
            {
                var beforeData = new
                {
                    StageId = stage.Id,
                    StageName = stage.Name,
                    IsCompleted = false,
                    Status = "InProgress",
                    CompletionRate = entity.CompletionRate - (100.0m / totalStages)
                };

                var afterData = new
                {
                    StageId = stage.Id,
                    StageName = stage.Name,
                    IsCompleted = true,
                    Status = allStagesCompleted ? "Completed" : "InProgress",
                    CompletionRate = entity.CompletionRate,
                    CompletedTime = DateTimeOffset.UtcNow,
                    CompletedBy = GetCurrentUserName()
                };

                var extendedData = new
                {
                    WorkflowId = entity.WorkflowId,
                    TotalStages = totalStages,
                    CompletedStages = completedCount,
                    AllStagesCompleted = allStagesCompleted,
                    CompletionNotes = completionNotes,
                    Source = "manual_completion"
                };

                await _operationChangeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.StageComplete,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: stage.Id,
                    onboardingId: entity.Id,
                    stageId: stage.Id,
                    operationTitle: $"Stage Completed: {stage.Name}",
                    operationDescription: $"Stage '{stage.Name}' has been completed by {GetCurrentUserName()}" +
                        (allStagesCompleted ? " (Final stage - Onboarding completed)" : ""),
                    beforeData: JsonSerializer.Serialize(beforeData),
                    afterData: JsonSerializer.Serialize(afterData),
                    changedFields: new List<string> { "IsCompleted", "Status", "CompletionRate", "CompletedTime" },
                    extendedData: JsonSerializer.Serialize(extendedData)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log stage completion: OnboardingId={OnboardingId}, StageId={StageId}",
                    entity.Id, stage.Id);
            }
        }

        #endregion


        #region Email Notification

        /// <summary>
        /// Send stage completion email notification
        /// </summary>
        private async Task SendStageCompletionEmailAsync(Onboarding entity, Stage completedStage)
        {
            try
            {
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                var orderedStages = stages.OrderBy(x => x.Order).ToList();

                var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == completedStage.Id)?.Order ?? 0;
                var immediateNextStage = orderedStages
                    .Where(s => s.Order > completedStageOrder)
                    .OrderBy(s => s.Order)
                    .FirstOrDefault();

                if (immediateNextStage == null)
                {
                    return;
                }

                var nextStageProgress = entity.StagesProgress?.FirstOrDefault(sp => sp.StageId == immediateNextStage.Id);
                if (nextStageProgress != null && nextStageProgress.IsCompleted)
                {
                    return;
                }

                var caseUrl = BuildCaseUrl(entity.Id, entity.TenantId);
                var caseId = entity.CaseCode ?? entity.Id.ToString();
                var caseName = entity.CaseName ?? $"Case {caseId}";
                var utcTime = DateTimeOffset.UtcNow;
                var localTime = TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Local);
                var completionTime = localTime.ToString("MM/dd/yyyy hh:mm:ss tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

                await SendEmailToStageAssigneesAsync(entity, immediateNextStage, caseId, caseName, completedStage.Name, immediateNextStage.Name, GetCurrentUserName(), completionTime, caseUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stage completion email: StageId={StageId}, OnboardingId={OnboardingId}",
                    completedStage.Id, entity.Id);
            }
        }

        /// <summary>
        /// Build case URL for email notification
        /// </summary>
        private string BuildCaseUrl(long onboardingId, string? tenantId = null)
        {
            try
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context != null)
                {
                    var request = context.Request;
                    var scheme = request.Headers["X-Forwarded-Proto"].ToString();
                    if (string.IsNullOrWhiteSpace(scheme)) scheme = request.Scheme;

                    var forwardedHost = request.Headers["X-Forwarded-Host"].ToString();
                    var host = !string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost : request.Host.Value;

                    var baseUrl = $"{scheme}://{host}".TrimEnd('/');
                    var url = $"{baseUrl}/onboard/onboardDetail?onboardingId={onboardingId}";
                    if (!string.IsNullOrWhiteSpace(tenantId))
                    {
                        url += $"&tenantId={Uri.EscapeDataString(tenantId)}";
                    }
                    return url;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build case URL from request context");
            }

            var fallbackUrl = $"https://crm-staging.item.com/onboard/onboardDetail?onboardingId={onboardingId}";
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                fallbackUrl += $"&tenantId={Uri.EscapeDataString(tenantId)}";
            }
            return fallbackUrl;
        }

        #endregion


        #region Send Email to Assignees

        /// <summary>
        /// Send email notification to stage's assignees and co-assignees
        /// </summary>
        private async Task SendEmailToStageAssigneesAsync(Onboarding entity, Stage stage, string caseId, string caseName, string completedStageName, string nextStageName, string completedBy, string completionTime, string caseUrl)
        {
            try
            {
                var allAssigneeIds = new HashSet<string>();
                var stageProgress = entity.StagesProgress?.FirstOrDefault(sp => sp.StageId == stage.Id);

                // Get assignee IDs (priority: CustomStageAssignee > Assignee > Stage defaults)
                List<string> assigneeIds;
                if (stageProgress?.CustomStageAssignee?.Any() == true)
                {
                    assigneeIds = stageProgress.CustomStageAssignee;
                }
                else if (stageProgress?.Assignee?.Any() == true)
                {
                    assigneeIds = stageProgress.Assignee;
                }
                else
                {
                    assigneeIds = ParseAssigneeJson(stage.DefaultAssignee);
                }

                // Get co-assignee IDs
                List<string> coAssigneeIds;
                if (stageProgress?.CustomStageCoAssignees?.Any() == true)
                {
                    coAssigneeIds = stageProgress.CustomStageCoAssignees;
                }
                else if (stageProgress?.CoAssignees?.Any() == true)
                {
                    coAssigneeIds = stageProgress.CoAssignees;
                }
                else
                {
                    coAssigneeIds = ParseAssigneeJson(stage.CoAssignees);
                }

                foreach (var id in assigneeIds) allAssigneeIds.Add(id);
                foreach (var id in coAssigneeIds) allAssigneeIds.Add(id);

                if (allAssigneeIds.Count == 0) return;

                var userIds = allAssigneeIds
                    .Where(id => long.TryParse(id, out _))
                    .Select(id => long.Parse(id))
                    .ToList();

                if (userIds.Count == 0) return;

                var users = await _userService.GetUsersByIdsAsync(userIds);
                if (users == null || users.Count == 0) return;

                var emailTasks = users
                    .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                    .Select(async user =>
                    {
                        try
                        {
                            return await _emailService.SendStageCompletedNotificationAsync(
                                user.Email, caseId, caseName, completedStageName,
                                nextStageName, completedBy ?? "System", completionTime, caseUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send email to {Email}", user.Email);
                            return false;
                        }
                    })
                    .ToList();

                await Task.WhenAll(emailTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to stage assignees: StageId={StageId}", stage.Id);
            }
        }

        /// <summary>
        /// Parse assignee JSON string to list of user IDs
        /// </summary>
        private List<string> ParseAssigneeJson(string assigneeJson)
        {
            if (string.IsNullOrWhiteSpace(assigneeJson)) return new List<string>();

            try
            {
                var jsonDoc = JsonDocument.Parse(assigneeJson);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return jsonDoc.RootElement.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .ToList();
                }
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.String)
                {
                    var innerJson = jsonDoc.RootElement.GetString();
                    if (!string.IsNullOrWhiteSpace(innerJson))
                    {
                        var innerDoc = JsonDocument.Parse(innerJson);
                        if (innerDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            return innerDoc.RootElement.EnumerateArray()
                                .Select(e => e.GetString())
                                .Where(id => !string.IsNullOrWhiteSpace(id))
                                .ToList();
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse assignee JSON: {Json}", assigneeJson);
            }

            return new List<string>();
        }

        #endregion
    }
}
