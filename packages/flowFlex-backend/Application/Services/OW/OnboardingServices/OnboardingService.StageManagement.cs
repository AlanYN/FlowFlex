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
    /// Onboarding service - Stage management operations
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<bool> MoveToNextStageAsync(long id)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

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
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

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

            return result;
        }
        /// <summary>
        /// Complete specified stage with validation (supports non-sequential completion)
        /// </summary>
        public async Task<bool> CompleteCurrentStageAsync(long id, CompleteCurrentStageInputDto input)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(id))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {id}");
            }

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

            // After updating progress, synchronously generate AI summary so client can fetch it right after completion
            //try
            //{
            //    var lang = string.IsNullOrWhiteSpace(input.Language) ? "auto" : input.Language;
            //    var opts = new StageSummaryOptions { Language = lang, SummaryLength = "short", IncludeTaskAnalysis = true, IncludeQuestionnaireInsights = true };
            //    // Try multiple attempts synchronously before falling back to placeholder
            //    var ai = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //    if (ai == null || !ai.Success)
            //    {
            //        try { await Task.Delay(1500); } catch {}
            //        ai = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //    }
            //    LoadStagesProgressFromJson(entity);
            //    var sp = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageToComplete.Id);
            //    if (sp != null)
            //    {
            //        if (ai != null && ai.Success)
            //        {
            //            sp.AiSummary = ai.Summary;
            //            sp.AiSummaryGeneratedAt = DateTime.UtcNow;
            //            sp.AiSummaryConfidence = (decimal?)Convert.ToDecimal(ai.ConfidenceScore);
            //            sp.AiSummaryModel = ai.ModelUsed;
            //            var detailedData = new { ai.Breakdown, ai.KeyInsights, ai.Recommendations, ai.CompletionStatus, generatedAt = DateTime.UtcNow };
            //            sp.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(detailedData);
            //        }
            //        else
            //        {
            //            // Fallback placeholder to ensure frontend sees a value immediately; schedule retry in background
            //            sp.AiSummary = "AI summary is being generated...";
            //            sp.AiSummaryGeneratedAt = DateTime.UtcNow;
            //            sp.AiSummaryConfidence = null;
            //            sp.AiSummaryModel = null;
            //            sp.AiSummaryData = null;
            //            _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
            //            {
            //                try
            //                {
            //                    var retry = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //                    if (retry == null || !retry.Success)
            //                    {
            //                        try { await Task.Delay(3000); } catch {}
            //                        retry = await _stageService.GenerateAISummaryAsync(stageToComplete.Id, null, opts);
            //                    }
            //                    if (retry != null && retry.Success)
            //                    {
            //                        LoadStagesProgressFromJson(entity);
            //                        var sp2 = entity.StagesProgress?.FirstOrDefault(s => s.StageId == stageToComplete.Id);
            //                        if (sp2 != null)
            //                        {
            //                            sp2.AiSummary = retry.Summary;
            //                            sp2.AiSummaryGeneratedAt = DateTime.UtcNow;
            //                            sp2.AiSummaryConfidence = (decimal?)Convert.ToDecimal(retry.ConfidenceScore);
            //                            sp2.AiSummaryModel = retry.ModelUsed;
            //                            var dd = new { retry.Breakdown, retry.KeyInsights, retry.Recommendations, retry.CompletionStatus, generatedAt = DateTime.UtcNow };
            //                            sp2.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(dd);
            //                            entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            //                            await SafeUpdateOnboardingAsync(entity);
            //                        }
            //                    }
            //                }
            //                catch { }
            //            });
            //        }
            //        entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            //        await SafeUpdateOnboardingAsync(entity);
            //    }
            //}
            //catch { /* keep non-blocking if AI fails */ }

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

                LoggingExtensions.WriteLine($"[DEBUG] CompleteCurrentStageAsync - Before auto-advance: CurrentStageId={entity.CurrentStageId}, CompletedStageId={stageToComplete.Id}, NextIndex={nextStageIndex}");

                // If current stage is the completed stage and there's a next stage, advance to it
                if (entity.CurrentStageId == stageToComplete.Id && nextStageIndex < orderedStages.Count)
                {
                    var nextStage = orderedStages[nextStageIndex];
                    var oldStageId = entity.CurrentStageId;
                    entity.CurrentStageId = nextStage.Id;
                    entity.CurrentStageOrder = nextStage.Order;
                    entity.CurrentStageStartTime = DateTimeOffset.UtcNow;
                    // Debug logging handled by structured logging
                    LoggingExtensions.WriteLine($"[DEBUG] CompleteCurrentStageAsync - Advanced to next stage: OldStageId={oldStageId}, NewStageId={entity.CurrentStageId}, StageName={nextStage.Name}");
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
            // Publish stage completion event
            if (result)
            {
                // Debug logging handled by structured logging
                await PublishStageCompletionEventForCurrentStageAsync(entity, stageToComplete, allStagesCompleted);
            }

            // Debug logging handled by structured logging End ===");
            return result;
        }

        /// <summary>
        /// Pause onboarding
        /// </summary>
    }
}

