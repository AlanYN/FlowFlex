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
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for managing onboarding status transitions
    /// Handles: Start, Pause, Resume, Cancel, Abort, Reactivate, ForceComplete
    /// </summary>
    public class OnboardingStatusService : IOnboardingStatusService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingPermissionService _permissionService;
        private readonly IOnboardingStageProgressService _stageProgressService;
        private readonly IOnboardingLogService _onboardingLogService;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly UserContext _userContext;
        private readonly ILogger<OnboardingStatusService> _logger;

        public OnboardingStatusService(
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            IOnboardingPermissionService permissionService,
            IOnboardingStageProgressService stageProgressService,
            IOnboardingLogService onboardingLogService,
            IOperatorContextService operatorContextService,
            IBackgroundTaskQueue backgroundTaskQueue,
            UserContext userContext,
            ILogger<OnboardingStatusService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _stageProgressService = stageProgressService ?? throw new ArgumentNullException(nameof(stageProgressService));
            _onboardingLogService = onboardingLogService ?? throw new ArgumentNullException(nameof(onboardingLogService));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Helper Methods

        /// <summary>
        /// Get current user email from OperatorContextService
        /// </summary>
        private string GetCurrentUserEmail()
            => OnboardingSharedUtilities.GetCurrentUserEmail(_userContext, _operatorContextService);

        /// <summary>
        /// Normalize DateTimeOffset to start of day (00:00:00)
        /// </summary>
        private static DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dateTime)
            => OnboardingSharedUtilities.NormalizeToStartOfDay(dateTime);

        /// <summary>
        /// Get current UTC time normalized to start of day
        /// </summary>
        private static DateTimeOffset GetNormalizedUtcNow()
            => OnboardingSharedUtilities.GetNormalizedUtcNowOffset();

        #endregion


        #region Update Methods

        /// <summary>
        /// Safely update onboarding without modifying stages_progress_json
        /// </summary>
        private async Task<bool> SafeUpdateOnboardingWithoutStagesProgressAsync(Onboarding entity, string preserveStagesProgressJson)
        {
            try
            {
                var db = _onboardingRepository.GetSqlSugarClient();

                var result = await _onboardingRepository.UpdateAsync(entity,
                    it => new
                    {
                        it.WorkflowId,
                        it.CurrentStageId,
                        it.CurrentStageOrder,
                        it.LeadId,
                        it.CaseName,
                        it.LeadEmail,
                        it.LeadPhone,
                        it.ContactPerson,
                        it.ContactEmail,
                        it.LifeCycleStageId,
                        it.LifeCycleStageName,
                        it.Status,
                        it.CompletionRate,
                        it.StartDate,
                        it.EstimatedCompletionDate,
                        it.ActualCompletionDate,
                        it.CurrentAssigneeId,
                        it.CurrentAssigneeName,
                        it.CurrentTeam,
                        it.StageUpdatedById,
                        it.StageUpdatedBy,
                        it.StageUpdatedByEmail,
                        it.StageUpdatedTime,
                        it.CurrentStageStartTime,
                        it.Priority,
                        it.IsPrioritySet,
                        it.Ownership,
                        it.OwnershipName,
                        it.OwnershipEmail,
                        it.Notes,
                        it.IsActive,
                        it.ModifyDate,
                        it.ModifyBy,
                        it.ModifyUserId,
                        it.IsValid
                    });

                // Restore original stages_progress_json
                if (!string.IsNullOrEmpty(preserveStagesProgressJson))
                {
                    try
                    {
                        var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                        await db.Ado.ExecuteCommandAsync(progressSql, new
                        {
                            StagesProgressJson = preserveStagesProgressJson,
                            Id = entity.Id
                        });
                    }
                    catch (Exception progressEx)
                    {
                        _logger.LogWarning(progressEx, "Failed to preserve stages_progress_json for onboarding {OnboardingId}", entity.Id);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to safely update onboarding: {ex.Message}");
            }
        }

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private void UpdateStageTrackingInfo(Onboarding entity)
            => OnboardingSharedUtilities.UpdateStageTrackingInfo(entity, _operatorContextService, _userContext);

        #endregion


        #region Status Operations

        /// <inheritdoc />
        public async Task<bool> StartOnboardingAsync(long id, StartOnboardingInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status != "Inactive")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot start onboarding. Current status is '{entity.Status}'. Only 'Inactive' onboardings can be started.");
            }

            var originalStagesProgressJson = entity.StagesProgressJson;

            entity.Status = OnboardingStatusEnum.Active.ToDbString();
            entity.StartDate = NormalizeToStartOfDay(DateTimeOffset.UtcNow);
            entity.CurrentStageStartTime = GetNormalizedUtcNow();

            if (input.ResetProgress)
            {
                entity.CurrentStageId = null;
                entity.CurrentStageOrder = 0;
                entity.CompletionRate = 0;
            }

            var startText = $"[Start Onboarding] Onboarding activated";
            if (!string.IsNullOrEmpty(input.Reason)) startText += $" - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.Notes)) startText += $" - Notes: {input.Notes}";
            OnboardingSharedUtilities.SafeAppendToNotes(entity, startText);

            UpdateStageTrackingInfo(entity);

            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingStartAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: input.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding start operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> PauseAsync(long id)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot pause completed onboarding");
            }

            var result = await _onboardingRepository.UpdateStatusAsync(id, "Paused");

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingPauseAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding pause operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> ResumeAsync(long id)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status != "Paused")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Only paused onboarding can be resumed");
            }

            var result = await _onboardingRepository.UpdateStatusAsync(id, "InProgress");

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingResumeAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding resume operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        #endregion


        #region Additional Status Operations

        /// <inheritdoc />
        public async Task<bool> ResumeWithConfirmationAsync(long id, ResumeOnboardingInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status != "Paused")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot resume onboarding. Current status is '{entity.Status}'. Only 'Paused' onboardings can be resumed.");
            }

            var originalStagesProgressJson = entity.StagesProgressJson;

            entity.Status = OnboardingStatusEnum.Active.ToDbString();

            var resumeText = $"[Resume] Onboarding resumed from Paused status";
            if (!string.IsNullOrEmpty(input.Reason)) resumeText += $" - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.Notes)) resumeText += $" - Notes: {input.Notes}";
            OnboardingSharedUtilities.SafeAppendToNotes(entity, resumeText);

            UpdateStageTrackingInfo(entity);

            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingResumeAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: input.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding resume operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> CancelAsync(long id, string reason)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot cancel completed onboarding");
            }

            if (!string.IsNullOrEmpty(reason))
            {
                var cancellationNote = $"Cancelled: {reason}";
                var currentNotes = entity.Notes ?? string.Empty;
                var newContent = string.IsNullOrEmpty(currentNotes) ? cancellationNote : $"{cancellationNote}. {currentNotes}";
                if (newContent.Length > 1000) newContent = newContent.Substring(0, 1000);
                entity.Notes = newContent;

                var originalStagesProgressJson = entity.StagesProgressJson;
                await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
            }

            return await _onboardingRepository.UpdateStatusAsync(id, "Cancelled");
        }

        /// <inheritdoc />
        public async Task<bool> AbortAsync(long id, AbortOnboardingInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed" || entity.Status == "Aborted")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot abort onboarding with status '{entity.Status}'");
            }

            var originalStagesProgressJson = entity.StagesProgressJson;

            entity.Status = OnboardingStatusEnum.Aborted.ToDbString();
            entity.EstimatedCompletionDate = null;

            var abortText = $"[Abort] Onboarding terminated - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.Notes)) abortText += $" - Notes: {input.Notes}";
            OnboardingSharedUtilities.SafeAppendToNotes(entity, abortText);

            UpdateStageTrackingInfo(entity);

            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingAbortAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: input.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding abort operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        #endregion


        #region Reactivate and ForceComplete

        /// <inheritdoc />
        public async Task<bool> ReactivateAsync(long id, ReactivateOnboardingInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status != "Aborted")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot reactivate onboarding. Current status is '{entity.Status}'. Only 'Aborted' onboardings can be reactivated.");
            }

            var originalStagesProgressJson = entity.StagesProgressJson;

            entity.Status = OnboardingStatusEnum.Active.ToDbString();
            entity.ActualCompletionDate = null;

            var reactivateText = $"[Reactivate] Onboarding reactivated from Aborted status - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.Notes)) reactivateText += $" - Notes: {input.Notes}";
            if (input.PreserveAnswers) reactivateText += " - Questionnaire answers preserved";
            OnboardingSharedUtilities.SafeAppendToNotes(entity, reactivateText);

            UpdateStageTrackingInfo(entity);

            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingReactivateAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: input.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding reactivate operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot reject completed onboarding");
            }

            if (entity.Status == "Rejected" || entity.Status == "Terminated")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already rejected or terminated");
            }

            var currentTime = DateTimeOffset.UtcNow;
            var originalStagesProgressJson = entity.StagesProgressJson;

            entity.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
            entity.ModifyDate = currentTime;

            var rejectionNote = $"[{(input.TerminateWorkflow ? "TERMINATED" : "REJECTED")}] {input.RejectionReason}";
            if (!string.IsNullOrEmpty(input.AdditionalNotes)) rejectionNote += $" - Additional Notes: {input.AdditionalNotes}";
            rejectionNote += $" - {(input.TerminateWorkflow ? "Terminated" : "Rejected")} by: {input.RejectedBy} at {currentTime:yyyy-MM-dd HH:mm:ss}";
            OnboardingSharedUtilities.SafeAppendToNotes(entity, rejectionNote);

            // Update stages progress to reflect rejection
            _stageProgressService.LoadStagesProgressFromJson(entity);
            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    if (stage.Status == "InProgress" || stage.Status == "Pending")
                    {
                        stage.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
                        stage.RejectionReason = input.RejectionReason;
                        stage.RejectionTime = currentTime;
                        stage.RejectedBy = input.RejectedBy;
                        stage.LastUpdatedTime = currentTime;
                        stage.LastUpdatedBy = input.RejectedBy;

                        if (input.TerminateWorkflow)
                        {
                            stage.TerminationTime = currentTime;
                            stage.TerminatedBy = input.RejectedBy;
                        }
                    }
                }
                entity.StagesProgressJson = _stageProgressService.SerializeStagesProgress(entity.StagesProgress);
            }

            UpdateStageTrackingInfo(entity);

            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, entity.StagesProgressJson);
        }

        #endregion


        #region ForceComplete, Assign, UpdateCompletionRate

        /// <inheritdoc />
        public async Task<bool> ForceCompleteAsync(long id, ForceCompleteOnboardingInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            if (entity.Status == "Completed" || entity.Status == "Force Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot force complete onboarding with status '{entity.Status}'");
            }

            var originalStagesProgressJson = entity.StagesProgressJson;

            entity.Status = OnboardingStatusEnum.ForceCompleted.ToDbString();
            entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            entity.CompletionRate = 100;

            var completionText = $"[Force Complete] Onboarding force completed - Reason: {input.Reason}";
            if (!string.IsNullOrEmpty(input.CompletionNotes)) completionText += $" - Notes: {input.CompletionNotes}";
            if (input.Rating.HasValue) completionText += $" - Rating: {input.Rating}/5";
            if (!string.IsNullOrEmpty(input.Feedback)) completionText += $" - Feedback: {input.Feedback}";
            OnboardingSharedUtilities.SafeAppendToNotes(entity, completionText);

            UpdateStageTrackingInfo(entity);

            var result = await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);

            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingForceCompleteAsync(id, entity.CaseName ?? entity.CaseCode ?? "Unknown", reason: input.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding force complete operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> AssignAsync(long id, AssignOnboardingInputDto input)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            entity.CurrentAssigneeId = input.AssigneeId;
            entity.CurrentAssigneeName = input.AssigneeName;
            entity.CurrentTeam = input.Team;

            var originalStagesProgressJson = entity.StagesProgressJson;
            return await SafeUpdateOnboardingWithoutStagesProgressAsync(entity, originalStagesProgressJson);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateCompletionRateAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;

            var completedStages = Math.Max(0, entity.CurrentStageOrder - 1);
            var stageBasedCompletionRate = totalStages > 0 ? (decimal)completedStages / totalStages * 100 : 0;

            return await _onboardingRepository.UpdateCompletionRateAsync(id, stageBasedCompletionRate);
        }

        #endregion
    }
}
