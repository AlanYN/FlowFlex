using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
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
using Microsoft.Extensions.Logging;
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
    /// Onboarding service - Stage management operations
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<bool> MoveToNextStageAsync(long id)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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

        /// <summary>
        /// Move to specific stage
        /// </summary>
        public async Task<bool> MoveToStageAsync(long id, MoveToStageInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

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
            await EnsureStagesProgressInitializedAsync(entity);

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
            (bool canComplete, string validationError) = await ValidateStageCanBeCompletedAsync(entity, stageIdToComplete);
            if (!canComplete && !input.ForceComplete)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }

            // Update stages progress
            await UpdateStagesProgressAsync(entity, stageIdToComplete, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);

            // Check if all stages are completed
            var allStagesCompleted = entity.StagesProgress.All(sp => sp.IsCompleted);

            if (allStagesCompleted)
            {
                // Complete the entire onboarding
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;
            }
            else
            {
                // Update completion rate
                entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                // Update current stage if needed
                if (entity.CurrentStageId == stageToComplete.Id)
                {
                    // If we completed the current stage, advance to the next incomplete stage
                    var nextIncompleteStage = orderedStages
                        .Where(stage => stage.Order > stageToComplete.Order &&
                                       !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                        .OrderBy(stage => stage.Order)
                        .FirstOrDefault();

                    if (nextIncompleteStage != null)
                    {
                        entity.CurrentStageId = nextIncompleteStage.Id;
                        entity.CurrentStageOrder = nextIncompleteStage.Order;
                        entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    }
                }
                else if (entity.CurrentStageId != stageToComplete.Id && !input.PreventAutoMove)
                {
                    // If we completed a different stage (not the current one), 
                    // advance to the next incomplete stage AFTER the completed stage (only look forward)
                    // 浣嗗鏋?PreventAutoMove 涓?true锛屽垯涓嶈嚜鍔ㄧЩ鍔紙鐢ㄤ簬绯荤粺鍔ㄤ綔鐨勭簿纭帶鍒讹級
                    var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == stageToComplete.Id)?.Order ?? 0;
                    var nextIncompleteStage = orderedStages
                        .Where(stage => stage.Order > completedStageOrder &&
                                       !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                        .OrderBy(stage => stage.Order)
                        .FirstOrDefault();

                    if (nextIncompleteStage != null)
                    {
                        entity.CurrentStageId = nextIncompleteStage.Id;
                        entity.CurrentStageOrder = nextIncompleteStage.Order;
                        entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    }
                }

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}";
                    SafeAppendToNotes(entity, noteText);
                }
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Update the entity using safe method - NO EVENT PUBLISHING
            var result = await SafeUpdateOnboardingAsync(entity);

            // Send email notification to next stage's default assignees (only if explicitly requested)
            // Default is false to avoid duplicate emails from multiple callers
            if (result && input.SendEmailNotification)
            {
                try
                {
                    var completedBy = GetCurrentUserName() ?? "System";
                    await SendStageCompletionEmailNotificationAsync(entity, stageToComplete, completedBy);
                    _logger.LogInformation("Sent stage completion email notification from internal completion: OnboardingId={OnboardingId}, StageId={StageId}",
                        entity.Id, stageToComplete.Id);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send stage completion email notification from internal completion: OnboardingId={OnboardingId}, StageId={StageId}",
                        entity.Id, stageToComplete.Id);
                    // Don't re-throw to avoid breaking the main flow
                }
            }

            return result;
        }
        /// <summary>
        /// Complete specified stage with validation (supports non-sequential completion)
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
        {
            // Check permission
            await EnsureCaseOperatePermissionAsync(id);

            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Ensure stages progress is properly initialized and synced
            await EnsureStagesProgressInitializedAsync(entity);

            // Save the initialized stages progress
            await SafeUpdateOnboardingAsync(entity);

            // Debug logging handled by structured logging ===");
            // Debug logging handled by structured logging
            // Get target stage ID with backward compatibility
            long targetStageId;
            try
            {
                targetStageId = input.GetTargetStageId();
            }
            catch (ArgumentException ex)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid stage ID parameters: {ex.Message}");
            }
            // Debug logging handled by structured logging
            // Check if onboarding is already completed
            if (entity.Status == "Completed")
            {
                // Debug logging handled by structured logging
                return true; // Return success since the desired outcome (completion) is already achieved
            }

            // Optional: Check if frontend stage matches backend stage (only if CurrentStageId is provided)
            if (input.CurrentStageId.HasValue && entity.CurrentStageId != input.CurrentStageId)
            {
                // Debug logging handled by structured logging
                // Auto-correct: Update completion rate and sync stage information
                // Debug logging handled by structured logging
                try
                {
                    await UpdateCompletionRateAsync(id);
                    // Debug logging handled by structured logging
                    // Reload the entity to get the latest data after completion rate update
                    entity = await _onboardingRepository.GetByIdAsync(id);
                    // Debug logging handled by structured logging
                }
                catch (Exception ex)
                {
                    // Debug logging handled by structured logging
                }

                // Check if the mismatch still exists after correction
                if (entity.CurrentStageId != input.CurrentStageId)
                {
                    // Debug logging handled by structured logging
                }
                else
                {
                    // Debug logging handled by structured logging
                }
            }
            // Debug logging handled by structured logging
            // Get all stages for this workflow
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var orderedStages = stages.OrderBy(x => x.Order).ToList();
            var totalStages = orderedStages.Count;
            // Debug logging handled by structured logging
            // Debug logging handled by structured logging)}");

            // Find the stage to complete
            var stageToComplete = orderedStages.FirstOrDefault(x => x.Id == targetStageId);
            if (stageToComplete == null)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Specified stage not found in workflow");
            }

            // Debug logging handled by structured logging");

            // Validate if this stage can be completed
            var (canComplete, validationError) = await ValidateStageCanBeCompletedAsync(entity, stageToComplete.Id);
            if (!canComplete)
            {
                // Debug logging handled by structured logging
                throw new CRMException(ErrorCodeEnum.BusinessError, validationError);
            }
            // Debug logging handled by structured logging
            // Check stage completion logs to see if this stage has already been completed
            // Debug logging handled by structured logging
            // Stage completion log checking functionality removed

            // Update stages progress for the completed stage (non-sequential completion)
            // Debug logging handled by structured logging
            await UpdateStagesProgressAsync(entity, stageToComplete.Id, GetCurrentUserName(), GetCurrentUserId(), input.CompletionNotes);

            // NOTE: AI summary generation has been moved to a separate background service
            // to avoid blocking stage completion. The frontend should poll for AI summary updates.

            // Calculate new completion rate based on completed stages
            entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);
            var completedCount = entity.StagesProgress.Count(s => s.IsCompleted);
            // Debug logging handled by structured logging
            // Check if all stages are completed
            var allStagesCompleted = entity.StagesProgress.All(s => s.IsCompleted);
            if (allStagesCompleted)
            {
                // Complete the entire onboarding
                // Debug logging handled by structured logging
                entity.Status = "Completed";
                entity.CompletionRate = 100;
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Onboarding Completed] Final stage '{stageToComplete.Name}' completed: {input.CompletionNotes}";
                    SafeAppendToNotes(entity, noteText);
                    // Debug logging handled by structured logging
                }
            }
            else
            {
                // Update status to InProgress if it was Started
                if (entity.Status == "Started")
                {
                    entity.Status = "InProgress";
                    // Debug logging handled by structured logging
                }

                // Auto-advance to next stage logic (similar to CompleteCurrentStageAsync without input)
                // Find the next incomplete stage to advance to
                var currentStageIndex = orderedStages.FindIndex(x => x.Id == entity.CurrentStageId);
                var nextStageIndex = currentStageIndex + 1;

                _logger.LogDebug("CompleteCurrentStageAsync - Before auto-advance: CurrentStageId={CurrentStageId}, CompletedStageId={CompletedStageId}, NextIndex={NextIndex}",
                    entity.CurrentStageId, stageToComplete.Id, nextStageIndex);

                // If current stage is the completed stage and there's a next stage, advance to it
                if (entity.CurrentStageId == stageToComplete.Id && nextStageIndex < orderedStages.Count)
                {
                    var nextStage = orderedStages[nextStageIndex];
                    var oldStageId = entity.CurrentStageId;
                    entity.CurrentStageId = nextStage.Id;
                    entity.CurrentStageOrder = nextStage.Order;
                    entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    _logger.LogDebug("CompleteCurrentStageAsync - Advanced to next stage: OldStageId={OldStageId}, NewStageId={NewStageId}, StageName={StageName}",
                        oldStageId, entity.CurrentStageId, nextStage.Name);
                }
                else if (entity.CurrentStageId != stageToComplete.Id)
                {
                    // If we completed a different stage (not the current one), 
                    // advance to the next incomplete stage AFTER the completed stage (only look forward)
                    var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == stageToComplete.Id)?.Order ?? 0;
                    var nextIncompleteStage = orderedStages
                        .Where(stage => stage.Order > completedStageOrder &&
                                       !entity.StagesProgress.Any(sp => sp.StageId == stage.Id && sp.IsCompleted))
                        .OrderBy(stage => stage.Order)
                        .FirstOrDefault();

                    if (nextIncompleteStage != null)
                    {
                        entity.CurrentStageId = nextIncompleteStage.Id;
                        entity.CurrentStageOrder = nextIncompleteStage.Order;
                        entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                        // Debug logging handled by structured logging
                    }
                }

                // Add completion notes if provided
                if (!string.IsNullOrEmpty(input.CompletionNotes))
                {
                    var noteText = $"[Stage Completed] {stageToComplete.Name}: {input.CompletionNotes}";
                    SafeAppendToNotes(entity, noteText);
                    // Debug logging handled by structured logging
                }
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Update the entity using safe method
            var result = await SafeUpdateOnboardingAsync(entity);
            // Debug logging handled by structured logging
            // Log stage completion to operation_change_log
            if (result)
            {
                try
                {
                    var beforeData = new
                    {
                        StageId = stageToComplete.Id,
                        StageName = stageToComplete.Name,
                        IsCompleted = false,
                        Status = "InProgress",
                        CompletionRate = entity.CompletionRate - (100.0m / totalStages)
                    };

                    var afterData = new
                    {
                        StageId = stageToComplete.Id,
                        StageName = stageToComplete.Name,
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
                        CompletionNotes = input.CompletionNotes,
                        NextStageId = entity.CurrentStageId != stageToComplete.Id ? entity.CurrentStageId : (long?)null,
                        Source = "manual_completion"
                    };

                    await _operationChangeLogService.LogOperationAsync(
                        operationType: OperationTypeEnum.StageComplete,
                        businessModule: BusinessModuleEnum.Stage,
                        businessId: stageToComplete.Id,
                        onboardingId: entity.Id,
                        stageId: stageToComplete.Id,
                        operationTitle: $"Stage Completed: {stageToComplete.Name}",
                        operationDescription: $"Stage '{stageToComplete.Name}' has been completed by {GetCurrentUserName()}" +
                            (allStagesCompleted ? " (Final stage - Onboarding completed)" : ""),
                        beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                        afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                        changedFields: new List<string> { "IsCompleted", "Status", "CompletionRate", "CompletedTime" },
                        extendedData: System.Text.Json.JsonSerializer.Serialize(extendedData)
                    );

                    _logger.LogInformation("Stage completion logged: OnboardingId={OnboardingId}, StageId={StageId}, StageName={StageName}",
                        entity.Id, stageToComplete.Id, stageToComplete.Name);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log stage completion: OnboardingId={OnboardingId}, StageId={StageId}",
                        entity.Id, stageToComplete.Id);
                    // Don't re-throw to avoid breaking the main flow
                }

                // Debug logging handled by structured logging
                await PublishStageCompletionEventForCurrentStageAsync(entity, stageToComplete, allStagesCompleted);

                // Send email notification to stage default assignees
                try
                {
                    await SendStageCompletionEmailNotificationAsync(entity, stageToComplete, GetCurrentUserName());
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send stage completion email notification: OnboardingId={OnboardingId}, StageId={StageId}",
                        entity.Id, stageToComplete.Id);
                    // Don't re-throw to avoid breaking the main flow
                }
            }

            // Debug logging handled by structured logging End ===");
            return result;
        }

        /// <summary>
        /// Send stage completion email notification to next stage's default assignees
        /// Only sends email if the IMMEDIATE next stage (by order) is not completed
        /// If the immediate next stage is already completed, no email is sent
        /// </summary>
        private async Task SendStageCompletionEmailNotificationAsync(Onboarding entity, Stage stage, string completedBy)
        {
            try
            {
                // Get all stages for this workflow to find next stage
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                var orderedStages = stages.OrderBy(x => x.Order).ToList();

                // Find the IMMEDIATE next stage (by order), not skipping any stages
                var completedStageOrder = orderedStages.FirstOrDefault(s => s.Id == stage.Id)?.Order ?? 0;
                var immediateNextStage = orderedStages
                    .Where(s => s.Order > completedStageOrder)
                    .OrderBy(s => s.Order)
                    .FirstOrDefault();

                // No next stage exists
                if (immediateNextStage == null)
                {
                    _logger.LogDebug("No next stage found after stage {StageId}, skipping email notification", stage.Id);
                    return;
                }

                // Check if the immediate next stage is already completed
                // If it's completed, don't send email (the workflow has already moved past this stage)
                var nextStageProgress = entity.StagesProgress?.FirstOrDefault(sp => sp.StageId == immediateNextStage.Id);
                if (nextStageProgress != null && nextStageProgress.IsCompleted)
                {
                    _logger.LogDebug("Immediate next stage {NextStageId} ({NextStageName}) is already completed, skipping email notification",
                        immediateNextStage.Id, immediateNextStage.Name);
                    return;
                }

                string nextStageName = immediateNextStage.Name;

                // Build case URL using GetRequestOrigin method (similar to portal invitation)
                var caseUrl = BuildCaseUrl(entity.Id, entity.TenantId);

                // Prepare email data
                var caseId = entity.CaseCode ?? entity.Id.ToString();
                var caseName = entity.CaseName ?? $"Case {caseId}";
                var stageName = stage.Name ?? "Unknown Stage";
                // Convert UTC time to server local time and format as US time format: MM/dd/yyyy hh:mm:ss tt
                var utcTime = DateTimeOffset.UtcNow;
                var localTime = TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Local);
                var completionTime = localTime.ToString("MM/dd/yyyy hh:mm:ss tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

                // Send email to immediate next stage's default assignees only
                await SendEmailToStageAssigneesAsync(entity, immediateNextStage, caseId, caseName, stageName, nextStageName, completedBy, completionTime, caseUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stage completion email notification: StageId={StageId}, OnboardingId={OnboardingId}",
                    stage.Id, entity.Id);
                throw;
            }
        }

        /// <summary>
        /// Send email notification to stage's assignees and co-assignees
        /// Priority: CustomStageAssignee/CustomStageCoAssignees (user customization) > Assignee/CoAssignees (synced from Stage)
        /// </summary>
        private async Task SendEmailToStageAssigneesAsync(Onboarding entity, Stage stage, string caseId, string caseName, string completedStageName, string nextStageName, string completedBy, string completionTime, string caseUrl)
        {
            try
            {
                // Collect all assignee IDs from both Assignee and CoAssignees
                var allAssigneeIds = new HashSet<string>();

                // Get StagesProgress for this stage
                var stageProgress = entity.StagesProgress?.FirstOrDefault(sp => sp.StageId == stage.Id);
                
                List<string> assigneeIds;
                List<string> coAssigneeIds;

                // Priority: CustomStageAssignee > Assignee (synced from Stage)
                if (stageProgress?.CustomStageAssignee?.Any() == true)
                {
                    // Use user-customized assignee
                    assigneeIds = stageProgress.CustomStageAssignee;
                    _logger.LogDebug("Using CustomStageAssignee for stage {StageId}: {CustomStageAssignee}",
                        stage.Id, string.Join(",", assigneeIds));
                }
                else if (stageProgress?.Assignee?.Any() == true)
                {
                    // Use synced assignee from Stage template
                    assigneeIds = stageProgress.Assignee;
                    _logger.LogDebug("Using synced Assignee for stage {StageId}: {Assignee}",
                        stage.Id, string.Join(",", assigneeIds));
                }
                else
                {
                    // Fallback to Stage entity defaults (for backward compatibility)
                    assigneeIds = ParseAssigneeJson(stage.DefaultAssignee, stage.Id, "DefaultAssignee");
                    _logger.LogDebug("Using Stage template DefaultAssignee for stage {StageId}: {DefaultAssignee}",
                        stage.Id, string.Join(",", assigneeIds));
                }

                // Priority: CustomStageCoAssignees > CoAssignees (synced from Stage)
                if (stageProgress?.CustomStageCoAssignees?.Any() == true)
                {
                    // Use user-customized co-assignees
                    coAssigneeIds = stageProgress.CustomStageCoAssignees;
                    _logger.LogDebug("Using CustomStageCoAssignees for stage {StageId}: {CustomStageCoAssignees}",
                        stage.Id, string.Join(",", coAssigneeIds));
                }
                else if (stageProgress?.CoAssignees?.Any() == true)
                {
                    // Use synced co-assignees from Stage template
                    coAssigneeIds = stageProgress.CoAssignees;
                    _logger.LogDebug("Using synced CoAssignees for stage {StageId}: {CoAssignees}",
                        stage.Id, string.Join(",", coAssigneeIds));
                }
                else
                {
                    // Fallback to Stage entity defaults (for backward compatibility)
                    coAssigneeIds = ParseAssigneeJson(stage.CoAssignees, stage.Id, "CoAssignees");
                    _logger.LogDebug("Using Stage template CoAssignees for stage {StageId}: {CoAssignees}",
                        stage.Id, string.Join(",", coAssigneeIds));
                }

                // Add all assignees
                foreach (var id in assigneeIds)
                {
                    allAssigneeIds.Add(id);
                }

                // Add all co-assignees
                foreach (var id in coAssigneeIds)
                {
                    allAssigneeIds.Add(id);
                }

                if (allAssigneeIds.Count == 0)
                {
                    _logger.LogDebug("Next stage {StageId} has no assignees (assignee or co-assignees), skipping email notification", stage.Id);
                    return;
                }

                // Convert string IDs to long IDs
                var userIds = allAssigneeIds
                    .Where(id => long.TryParse(id, out _))
                    .Select(id => long.Parse(id))
                    .ToList();

                if (userIds.Count == 0)
                {
                    _logger.LogWarning("No valid user IDs found in assignees for next stage {StageId}", stage.Id);
                    return;
                }

                // Get user information
                var users = await _userService.GetUsersByIdsAsync(userIds);
                if (users == null || users.Count == 0)
                {
                    _logger.LogWarning("No users found for IDs: {UserIds} (Next Stage {StageId})", string.Join(", ", userIds), stage.Id);
                    return;
                }

                // Send email to each user
                var emailTasks = users
                    .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                    .Select(async user =>
                    {
                        try
                        {
                            return await _emailService.SendStageCompletedNotificationAsync(
                                user.Email,
                                caseId,
                                caseName,
                                completedStageName,
                                nextStageName,
                                completedBy ?? "System",
                                completionTime,
                                caseUrl
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send email to {Email} for next stage notification (Next Stage {StageId})", user.Email, stage.Id);
                            return false;
                        }
                    })
                    .ToList();

                var results = await Task.WhenAll(emailTasks);
                var successCount = results.Count(r => r);

                _logger.LogInformation("Sent email notifications to next stage assignees: {SuccessCount}/{TotalCount} successful, NextStageId={StageId}, OnboardingId={OnboardingId}",
                    successCount, emailTasks.Count, stage.Id, entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to next stage assignees: NextStageId={StageId}, OnboardingId={OnboardingId}",
                    stage.Id, entity.Id);
                // Don't re-throw to avoid breaking the main flow
            }
        }

        /// <summary>
        /// Build case URL for email notification (similar to portal invitation URL building)
        /// </summary>
        private string BuildCaseUrl(long onboardingId, string? tenantId = null)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    var request = context.Request;
                    var scheme = request.Headers["X-Forwarded-Proto"].ToString();
                    if (string.IsNullOrWhiteSpace(scheme))
                    {
                        scheme = request.Scheme;
                    }

                    var forwardedHost = request.Headers["X-Forwarded-Host"].ToString();
                    var host = !string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost : request.Host.Value;

                    var baseUrl = $"{scheme}://{host}".TrimEnd('/');
                    // Build frontend route: /onboard/onboardDetail?onboardingId=xxx&tenantId=xxx
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

            // Fallback to default URL
            var fallbackUrl = $"https://crm-staging.item.com/onboard/onboardDetail?onboardingId={onboardingId}";
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                fallbackUrl += $"&tenantId={Uri.EscapeDataString(tenantId)}";
            }
            return fallbackUrl;
        }

        /// <summary>
        /// Pause onboarding
        /// </summary>

        /// <summary>
        /// Parse assignee JSON string to list of user IDs
        /// </summary>
        private List<string> ParseAssigneeJson(string assigneeJson, long stageId, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(assigneeJson))
            {
                return new List<string>();
            }

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
                    // Nested JSON string
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
                _logger.LogWarning(ex, "Failed to parse {FieldName} JSON for stage {StageId}: {Json}",
                    fieldName, stageId, assigneeJson);
            }

            return new List<string>();
        }
    }
}

