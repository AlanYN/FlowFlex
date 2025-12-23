using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.ChangeLog;
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
using Microsoft.Extensions.Logging;


namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Status management operations
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<bool> PauseAsync(long id)
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

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot pause completed onboarding");
            }

            var result = await _onboardingRepository.UpdateStatusAsync(id, "Paused");

            // Log pause operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingPauseAsync(
                            id,
                            entity.LeadName ?? entity.CaseCode ?? "Unknown",
                            reason: null
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding pause operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Resume onboarding
        /// </summary>
        public async Task<bool> ResumeAsync(long id)
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

            if (entity.Status != "Paused")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Only paused onboarding can be resumed");
            }

            var result = await _onboardingRepository.UpdateStatusAsync(id, "InProgress");

            // Log resume operation
            if (result)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        await _onboardingLogService.LogOnboardingResumeAsync(
                            id,
                            entity.LeadName ?? entity.CaseCode ?? "Unknown",
                            reason: null
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log onboarding resume operation for onboarding {OnboardingId}", id);
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Cancel onboarding
        /// </summary>
        public async Task<bool> CancelAsync(long id, string reason)
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

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot cancel completed onboarding");
            }

            // Update notes with cancellation reason
            if (!string.IsNullOrEmpty(reason))
            {
                var cancellationNote = $"Cancelled: {reason}";
                // For cancellation, we want to prepend the note, so we'll handle this specially
                var currentNotes = entity.Notes ?? string.Empty;
                var newContent = string.IsNullOrEmpty(currentNotes)
                    ? cancellationNote
                    : $"{cancellationNote}. {currentNotes}";

                // Ensure we don't exceed the length limit
                if (newContent.Length > 1000)
                {
                    newContent = newContent.Substring(0, 1000);
                }
                entity.Notes = newContent;
                await SafeUpdateOnboardingAsync(entity);
            }

            // Log cancellation to Change Log
            await LogOnboardingActionAsync(entity, "Cancel Onboarding", "onboarding_cancel", true, new
            {
                CancellationReason = reason,
                CancelledAt = DateTimeOffset.UtcNow,
                CancelledBy = _operatorContextService.GetOperatorDisplayName()
            });

            return await _onboardingRepository.UpdateStatusAsync(id, "Cancelled");
        }

        /// <summary>
        /// Reject onboarding application
        /// </summary>
        public async Task<bool> RejectAsync(long id, RejectOnboardingInputDto input)
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

            if (entity.Status == "Completed")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot reject completed onboarding");
            }

            if (entity.Status == "Rejected" || entity.Status == "Terminated")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Onboarding is already rejected or terminated");
            }

            var currentTime = DateTimeOffset.UtcNow;

            // Update onboarding status
            entity.Status = input.TerminateWorkflow ? "Terminated" : "Rejected";
            entity.ModifyDate = currentTime;

            // Update notes with rejection reason
            var rejectionNote = $"[{(input.TerminateWorkflow ? "TERMINATED" : "REJECTED")}] {input.RejectionReason}";
            if (!string.IsNullOrEmpty(input.AdditionalNotes))
            {
                rejectionNote += $" - Additional Notes: {input.AdditionalNotes}";
            }
            rejectionNote += $" - {(input.TerminateWorkflow ? "Terminated" : "Rejected")} by: {input.RejectedBy} at {currentTime:yyyy-MM-dd HH:mm:ss}";

            SafeAppendToNotes(entity, rejectionNote);

            // Update stages progress to reflect rejection/termination
            LoadStagesProgressFromJson(entity);
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

                // Serialize updated stages progress back to JSON (only progress fields)
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }

            // Update stage tracking info
            await UpdateStageTrackingInfoAsync(entity);

            // Save changes
            var result = await SafeUpdateOnboardingAsync(entity);

            if (result)
            {
                // Log rejection to Change Log
                await LogOnboardingActionAsync(entity,
                    input.TerminateWorkflow ? "Terminate Onboarding" : "Reject Application",
                    input.TerminateWorkflow ? "onboarding_terminate" : "application_reject",
                    true,
                    new
                    {
                        RejectionReason = input.RejectionReason,
                        TerminateWorkflow = input.TerminateWorkflow,
                        AdditionalNotes = input.AdditionalNotes,
                        RejectedBy = input.RejectedBy,
                        RejectedById = input.RejectedById,
                        RejectedAt = currentTime,
                        SendNotification = input.SendNotification,
                        PreviousStatus = "InProgress", // Assuming it was in progress
                        NewStatus = entity.Status
                    });

                // Notification sending - future enhancement
                if (input.SendNotification)
                {
                    // Implement notification logic here
                    // Debug logging handled by structured logging} onboarding {id}");
                }
            }

            return result;
        }

        /// <summary>
        /// Assign onboarding to user
        /// </summary>
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

            return await SafeUpdateOnboardingAsync(entity);
        }

        /// <summary>
        /// Update completion rate based on stage progress
        /// </summary>
        public async Task<bool> UpdateCompletionRateAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;

            // Calculate completion rate based on stage progress
            // CurrentStageOrder represents the current stage (1-based), so completed stages = CurrentStageOrder - 1
            var completedStages = Math.Max(0, entity.CurrentStageOrder - 1);
            var stageBasedCompletionRate = totalStages > 0 ? (decimal)completedStages / totalStages * 100 : 0;

            // Log calculation details for debugging
            // Debug logging handled by structured logging
            // Always update to the stage-based completion rate
            // Debug logging handled by structured logging
            return await _onboardingRepository.UpdateCompletionRateAsync(id, stageBasedCompletionRate);
        }

        /// <summary>
        /// Update stage tracking information
        /// </summary>
        private async Task UpdateStageTrackingInfoAsync(Onboarding entity)
        {
            // Use OperatorContextService for consistent user information
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            // Sync isCurrent flag in stagesProgress to match currentStageId
            // Note: Don't reload from JSON here as UpdateStagesProgressAsync may have just updated the data
            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    // Update isCurrent flag based on currentStageId
                    stage.IsCurrent = stage.StageId == entity.CurrentStageId;

                    // Update stage status based on completion and current status, but preserve IsCompleted status
                    if (stage.IsCompleted)
                    {
                        stage.Status = "Completed";
                    }
                    else if (stage.IsCurrent)
                    {
                        stage.Status = "InProgress";
                    }
                    else
                    {
                        stage.Status = "Pending";
                    }
                }

                // Serialize back to JSON (only progress fields)
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }
        }

        /// <summary>
        /// Get onboarding progress
        /// </summary>
    }
}

