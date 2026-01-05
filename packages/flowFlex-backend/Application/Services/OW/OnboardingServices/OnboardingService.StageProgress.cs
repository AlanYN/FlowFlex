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
    /// Onboarding service - Stage progress management
    /// </summary>
    public partial class OnboardingService
    {
        private async Task InitializeStagesProgressAsync(Onboarding entity, List<Stage> stages)
        {
            try
            {
                entity.StagesProgress = new List<OnboardingStageProgress>();

                if (stages == null || !stages.Any())
                {
                    // Debug logging handled by structured logging
                    entity.StagesProgressJson = "[]";
                    return;
                }

                var orderedStages = stages.OrderBy(s => s.Order).ToList();
                var currentTime = DateTimeOffset.UtcNow;

                // Use sequential stage order (1, 2, 3, 4, 5...) instead of the original stage.Order
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var sequentialOrder = i + 1; // Sequential order starting from 1

                    var stageProgress = new OnboardingStageProgress
                    {
                        // Core progress fields (will be serialized to JSON)
                        StageId = stage.Id,
                        Status = sequentialOrder == 1 ? "InProgress" : "Pending", // First stage starts as InProgress
                        IsCompleted = false,
                        StartTime = null, // StartTime is now null by default, will be set when stage is saved or completed
                        CompletionTime = null,
                        CompletedById = null,
                        CompletedBy = null,
                        Notes = null,
                        IsCurrent = sequentialOrder == 1, // First stage is current
                        Assignee = ParseDefaultAssignee(stage.DefaultAssignee), // Initialize from Stage.DefaultAssignee
                        CoAssignees = GetFilteredCoAssignees(stage.CoAssignees, stage.DefaultAssignee), // Initialize from Stage.CoAssignees, excluding DefaultAssignee

                        // Stage configuration fields (not serialized, populated dynamically)
                        StageName = stage.Name,
                        StageDescription = stage.Description,
                        StageOrder = sequentialOrder,
                        EstimatedDays = stage.EstimatedDuration,
                        VisibleInPortal = stage.VisibleInPortal,
                        PortalPermission = stage.PortalPermission,
                        AttachmentManagementNeeded = stage.AttachmentManagementNeeded,
                        Required = stage.Required,
                        ComponentsJson = stage.ComponentsJson,
                        Components = stage.Components
                    };

                    entity.StagesProgress.Add(stageProgress);

                    // Debug logging handled by structured logging");
                }

                // Serialize to JSON for database storage (only progress fields, not stage configuration)
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                entity.StagesProgress = new List<OnboardingStageProgress>();
                entity.StagesProgressJson = "[]";
            }
        }
        /// <summary>
        /// Update stages progress - supports non-sequential stage completion
        /// </summary>
        private async Task UpdateStagesProgressAsync(Onboarding entity, long completedStageId, string completedBy = null, long? completedById = null, string notes = null)
        {
            try
            {
                // Load current progress using the proper method that handles JSON formatting
                LoadStagesProgressFromJson(entity);

                // Debug: Check if stageIds are correctly loaded
                LoggingExtensions.WriteLine($"[DEBUG] UpdateStagesProgressAsync - After LoadStagesProgressFromJson:");
                LoggingExtensions.WriteLine($"[DEBUG] StagesProgress count: {entity.StagesProgress?.Count ?? 0}");
                if (entity.StagesProgress != null)
                {
                    foreach (var sp in entity.StagesProgress)
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
                    }
                }

                var currentTime = DateTimeOffset.UtcNow;
                var completedStage = entity.StagesProgress.FirstOrDefault(s => s.StageId == completedStageId);

                if (completedStage != null)
                {
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging");

                    // Check if stage can be re-completed
                    var wasAlreadyCompleted = completedStage.IsCompleted;

                    // Mark current stage as completed
                    completedStage.Status = "Completed";
                    completedStage.IsCompleted = true;
                    completedStage.CompletionTime = currentTime;
                    completedStage.CompletedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();
                    completedStage.CompletedById = completedById ?? _operatorContextService.GetOperatorId();
                    completedStage.IsCurrent = false;
                    completedStage.LastUpdatedTime = currentTime;
                    completedStage.LastUpdatedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();

                    // Set StartTime if not already set (only during complete operations)
                    // This ensures StartTime is set when user actually completes work, not during status changes
                    if (!completedStage.StartTime.HasValue)
                    {
                        completedStage.StartTime = currentTime;
                    }

                    if (!string.IsNullOrEmpty(notes))
                    {
                        // Append new notes to existing notes if stage was re-completed
                        if (wasAlreadyCompleted && !string.IsNullOrEmpty(completedStage.Notes))
                        {
                            completedStage.Notes += $"\n[Re-completed {currentTime:yyyy-MM-dd HH:mm:ss}] {notes}";
                        }
                        else
                        {
                            completedStage.Notes = notes;
                        }
                    }

                    // Debug logging handled by structured logging}");

                    // Find next stage to activate (first incomplete stage after current completed stage)
                    var nextStage = entity.StagesProgress
                        .Where(s => s.StageOrder > completedStage.StageOrder && !s.IsCompleted)
                        .OrderBy(s => s.StageOrder)
                        .FirstOrDefault();

                    // Clear all current stage flags first
                    foreach (var stage in entity.StagesProgress)
                    {
                        stage.IsCurrent = false;
                    }

                    if (nextStage != null)
                    {
                        // Activate the next incomplete stage
                        nextStage.Status = "InProgress";
                        // Don't set StartTime here - only set it during save or complete operations
                        nextStage.IsCurrent = true;
                        nextStage.LastUpdatedTime = currentTime;
                        nextStage.LastUpdatedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();

                        // Debug logging handled by structured logging");
                    }
                    else
                    {
                        // All stages after the completed stage are already completed
                        // Don't go backward to find incomplete stages - this maintains forward progression
                        // Debug logging handled by structured logging
                    }

                    // Update completion rate based on completed stages
                    entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress);

                    // Debug logging handled by structured logging");
                }

                // Serialize back to JSON (only progress fields)
                await FilterValidStagesProgress(entity);
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Load stages progress from JSONB - optimized version with JSONB support
        /// Handles both legacy JSON format and new JSONB format with camelCase properties
        /// </summary>
        private void LoadStagesProgressFromJson(Onboarding entity)
        {
            try
            {
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    // Debug: Show input JSON
                    LoggingExtensions.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Input JSON:");
                    LoggingExtensions.WriteLine($"[DEBUG] {entity.StagesProgressJson}");

                    // Configure JsonSerializer options to handle both formats
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        // Allow trailing commas and comments for JSONB compatibility
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                        // Allow reading numbers from string format (e.g., "stageId": "1234" -> long)
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                    };

                    var jsonString = entity.StagesProgressJson.Trim();

                    // Handle double-serialized JSON (e.g., "\"[{...}]\"" instead of "[{...}]")
                    // This can happen when JSONB data is stored as escaped string
                    if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                    {
                        // First deserialize to get the actual JSON string
                        jsonString = JsonSerializer.Deserialize<string>(jsonString, options) ?? "[]";
                        LoggingExtensions.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Unwrapped double-serialized JSON");
                    }

                    entity.StagesProgress = JsonSerializer.Deserialize<List<OnboardingStageProgress>>(
                        jsonString, options) ?? new List<OnboardingStageProgress>();

                    // Debug: Show loaded data
                    LoggingExtensions.WriteLine($"[DEBUG] LoadStagesProgressFromJson - Loaded {entity.StagesProgress.Count} items:");
                    foreach (var sp in entity.StagesProgress)
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] Loaded StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
                    }

                    // Only fix stage order when needed, avoid unnecessary serialization
                    if (NeedsStageOrderFix(entity.StagesProgress))
                    {
                        FixStageOrderSequence(entity.StagesProgress);
                        entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                    }
                }
                else
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }
            }
            catch (JsonException jsonEx)
            {
                // Handle JSON parsing errors specifically
                LoggingExtensions.WriteLine($"JSON parsing error in LoadStagesProgressFromJson: {jsonEx.Message}");
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
            catch (Exception ex)
            {
#if DEBUG
                // Debug logging handled by structured logging
#endif
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Check if stage order needs to be fixed
        /// </summary>
        private bool NeedsStageOrderFix(List<OnboardingStageProgress> stagesProgress)
        {
            if (stagesProgress == null || !stagesProgress.Any())
                return false;

            var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();
            for (int i = 0; i < orderedStages.Count; i++)
            {
                if (orderedStages[i].StageOrder != i + 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Fix stage order to be sequential (1, 2, 3, 4, 5...) instead of potentially non-consecutive orders
        /// </summary>
        private void FixStageOrderSequence(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return;
                }

                // Sort by current stage order to maintain the original sequence
                var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();

                // Check if stage orders are already sequential
                bool needsFixing = false;
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    if (orderedStages[i].StageOrder != i + 1)
                    {
                        needsFixing = true;
                        break;
                    }
                }

                if (!needsFixing)
                {
                    // Debug logging handled by structured logging
                    return;
                }
                // Debug logging handled by structured logging
                // Reassign sequential stage orders
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var oldOrder = orderedStages[i].StageOrder;
                    var newOrder = i + 1;

                    orderedStages[i].StageOrder = newOrder;
                    // Debug logging handled by structured logging
                }

                // Update the original list with fixed orders safely
                // Instead of modifying the list during enumeration, replace each item individually
                for (int i = 0; i < stagesProgress.Count; i++)
                {
                    var matchingStage = orderedStages.FirstOrDefault(s => s.StageId == stagesProgress[i].StageId);
                    if (matchingStage != null)
                    {
                        stagesProgress[i] = matchingStage;
                    }
                }
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Validate if a stage can be completed based on business rules
        /// </summary>
        private async Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(Onboarding entity, long stageId)
        {
            try
            {
                // Debug logging handled by structured logging
                // Load stages progress
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return (false, "No stages progress found");
                }

                var stageToComplete = entity.StagesProgress.FirstOrDefault(s => s.StageId == stageId);
                if (stageToComplete == null)
                {
                    return (false, "Stage not found in progress");
                }

                // Debug logging handled by structured logging");

                // Check if stage is already completed
                if (stageToComplete.IsCompleted)
                {
                    // Debug logging handled by structured logging
                    // Don't return false, allow re-completion but log the warning
                }

                // Check onboarding status
                if (entity.Status == "Completed")
                {
                    return (false, "Onboarding is already completed");
                }

                if (entity.Status == "Cancelled" || entity.Status == "Rejected")
                {
                    return (false, $"Cannot complete stages when onboarding status is {entity.Status}");
                }

                // Allow completing any non-completed stage (removed sequential restriction)
                // Debug logging handled by structured logging");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return (false, $"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current user name from OperatorContextService
        /// </summary>
        private string GetCurrentUserName()
        {
            return _operatorContextService.GetOperatorDisplayName();
        }

        /// <summary>
        /// Get current user email from OperatorContextService 
        /// </summary>
        private string GetCurrentUserEmail()
        {
            var displayName = _operatorContextService.GetOperatorDisplayName();
            // If display name looks like an email, return it; otherwise fallback
            if (!string.IsNullOrEmpty(displayName) && displayName.Contains("@"))
            {
                return displayName;
            }
            return !string.IsNullOrEmpty(_userContext?.Email) ? _userContext.Email : "system@example.com";
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
        /// Get current user full name from OperatorContextService
        /// </summary>
        private string GetCurrentUserFullName()
        {
            return _operatorContextService.GetOperatorDisplayName();
        }

        /// <summary>
        /// Calculate completion rate based on completed stages count
        /// This method calculates progress based on how many stages are completed vs total stages
        /// Supports non-sequential stage completion
        /// </summary>
        private decimal CalculateCompletionRateByCompletedStages(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return 0;
                }

                var totalStagesCount = stagesProgress.Count;
                var completedStagesCount = stagesProgress.Count(s => s.IsCompleted);

                if (totalStagesCount == 0)
                {
                    return 0;
                }

                var completionRate = Math.Round((decimal)completedStagesCount / totalStagesCount * 100, 2);
                // Debug logging handled by structured logging
                // Debug logging handled by structured logging.Select(s => $"{s.StageOrder}:{s.StageName}"))}]");
                // Debug logging handled by structured logging.Select(s => $"{s.StageOrder}:{s.StageName}"))}]");

                return completionRate;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return 0;
            }
        }

        /// <summary>
        /// Clear related cache data
        /// </summary>
        private async Task ClearRelatedCacheAsync(long? workflowId = null, long? stageId = null)
        {
            try
            {
                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                var tasks = new List<Task>();

                if (workflowId.HasValue)
                {
                    var workflowCacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflowId.Value}";
                    // Redis cache temporarily disabled
                    // tasks.Add(_redisService.KeyDelAsync(workflowCacheKey));
                }

                if (stageId.HasValue)
                {
                    var stageCacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stageId.Value}";
                    // Redis cache temporarily disabled
                    // tasks.Add(_redisService.KeyDelAsync(stageCacheKey));
                }

                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
#if DEBUG
                    // Debug logging handled by structured logging
#endif
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Cache cleanup failure should not affect main flow
            }
        }

        /// <summary>
        /// Clear Onboarding query cache
        /// </summary>
        private async Task ClearOnboardingQueryCacheAsync()
        {
            try
            {
                string tenantId = _userContext?.TenantId ?? "default";

                // Use Keys method to get all matching keys, then batch delete
                var pattern = $"ow:onboarding:query:{tenantId}:*";
                // Redis cache temporarily disabled
                var keys = new List<string>();

                if (keys != null && keys.Any())
                {
                    // Batch delete all matching keys
                    // Redis cache temporarily disabled
                    var deleteTasks = keys.Select(key => Task.CompletedTask);
                    await Task.WhenAll(deleteTasks);

#if DEBUG
                    // Debug logging handled by structured logging
#endif
                }
                else
                {
#if DEBUG
                    // Debug logging handled by structured logging
#endif
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Cache cleanup failure should not affect main flow
            }
        }

        /// <summary>
        /// Enrich stages progress with data from Stage entities
        /// This method dynamically populates fields like stageName, stageOrder, estimatedDays etc.
        /// from the Stage entities, ensuring consistency and reducing data duplication.
        /// </summary>
        private async Task EnrichStagesProgressWithStageDataAsync(Onboarding entity)
        {
            try
            {
                if (entity?.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return;
                }

                // Get all stages for this workflow
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    return;
                }

                // Create a dictionary for fast lookup
                var stageDict = stages.ToDictionary(s => s.Id, s => s);

                // Enrich each stage progress with stage data
                foreach (var stageProgress in entity.StagesProgress)
                {
                    if (stageDict.TryGetValue(stageProgress.StageId, out var stage))
                    {
                        // Populate fields from Stage entity
                        stageProgress.StageName = stage.Name;
                        stageProgress.StageDescription = stage.Description;

                        // IMPORTANT: Only update EstimatedDays if CustomEstimatedDays is not set
                        // This preserves the custom values set by users and maintains the correct priority:
                        // CustomEstimatedDays > EstimatedDays (from Stage)
                        if (!stageProgress.CustomEstimatedDays.HasValue)
                        {
                            stageProgress.EstimatedDays = stage.EstimatedDuration;
                        }
                        // If CustomEstimatedDays exists, EstimatedDays should show the custom value
                        // (This will be handled by AutoMapper: EstimatedDays = CustomEstimatedDays ?? EstimatedDays)

                        // Auto-fill Assignee from Stage.DefaultAssignee if not set
                        if (stageProgress.Assignee == null || !stageProgress.Assignee.Any())
                        {
                            stageProgress.Assignee = ParseDefaultAssignee(stage.DefaultAssignee);
                            _logger.LogDebug("Auto-filled Assignee from DefaultAssignee: StageId={StageId}, DefaultAssignee={DefaultAssignee}, ParsedAssignee={ParsedAssignee}",
                                stage.Id, stage.DefaultAssignee, string.Join(",", stageProgress.Assignee ?? new List<string>()));
                        }

                        // Always sync DefaultAssignee from Stage configuration (read-only field)
                        stageProgress.DefaultAssignee = ParseDefaultAssignee(stage.DefaultAssignee);

                        // Auto-fill CoAssignees from Stage.CoAssignees if not set
                        if (stageProgress.CoAssignees == null || !stageProgress.CoAssignees.Any())
                        {
                            stageProgress.CoAssignees = GetFilteredCoAssignees(stage.CoAssignees, stage.DefaultAssignee);
                            _logger.LogDebug("Auto-filled CoAssignees from Stage: StageId={StageId}, CoAssignees={CoAssignees}",
                                stage.Id, string.Join(",", stageProgress.CoAssignees ?? new List<string>()));
                        }
                        else
                        {
                            // Filter out any IDs that are already in Assignee
                            var assigneeIds = stageProgress.Assignee ?? new List<string>();
                            stageProgress.CoAssignees = stageProgress.CoAssignees
                                .Where(id => !assigneeIds.Contains(id))
                                .ToList();
                        }

                        stageProgress.VisibleInPortal = stage.VisibleInPortal;
                        stageProgress.PortalPermission = stage.PortalPermission;
                        stageProgress.AttachmentManagementNeeded = stage.AttachmentManagementNeeded;
                        stageProgress.Required = stage.Required;
                        stageProgress.ComponentsJson = stage.ComponentsJson;
                        stageProgress.Components = stage.Components;
                        // AI Summary auto-generation removed - should only be triggered explicitly by user action
                    }
                }

                // Set stage orders based on the order in workflow (sequential: 1, 2, 3, ...)
                var orderedStages = stages.OrderBy(s => s.Order).ToList();
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var stageProgress = entity.StagesProgress.FirstOrDefault(sp => sp.StageId == stage.Id);
                    if (stageProgress != null)
                    {
                        stageProgress.StageOrder = i + 1; // Sequential order starting from 1
                    }
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Don't throw exception here to avoid breaking the main flow
            }
        }

        /// <summary>
        /// Sync stages progress with workflow stages - handle new stages addition
        /// This method ensures that if workflow has new stages, they are added to stagesProgress.
        /// </summary>
        private async Task SyncStagesProgressWithWorkflowAsync(Onboarding entity)
        {
            try
            {
                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    return;
                }

                // Load current stages progress
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null)
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }
                // 杩囨护鏃犳晥鐨?stageId
                var validStageIds = stages.Select(s => s.Id).ToHashSet();
                entity.StagesProgress?.RemoveAll(x => !validStageIds.Contains(x.StageId));

                // Get existing stage IDs
                var existingStageIds = entity.StagesProgress.Select(sp => sp.StageId).ToHashSet();

                // Find new stages that are not in stagesProgress
                var newStages = stages.Where(s => !existingStageIds.Contains(s.Id)).ToList();

                if (newStages.Any())
                {
                    // Order all stages properly
                    var orderedStages = stages.OrderBy(s => s.Order).ToList();

                    foreach (var newStage in newStages)
                    {
                        // Find the position to insert
                        var stageIndex = orderedStages.FindIndex(s => s.Id == newStage.Id);
                        var sequentialOrder = stageIndex + 1;

                        var newStageProgress = new OnboardingStageProgress
                        {
                            StageId = newStage.Id,
                            Status = "Pending",
                            IsCompleted = false,
                            StartTime = null,
                            CompletionTime = null,
                            CompletedById = null,
                            CompletedBy = null,
                            Notes = null,
                            IsCurrent = false,
                            Assignee = ParseDefaultAssignee(newStage.DefaultAssignee),
                            CoAssignees = GetFilteredCoAssignees(newStage.CoAssignees, newStage.DefaultAssignee)
                        };


                        // Insert at the correct position to maintain order
                        if (stageIndex < entity.StagesProgress.Count)
                        {
                            entity.StagesProgress.Insert(stageIndex, newStageProgress);
                        }
                        else
                        {
                            entity.StagesProgress.Add(newStageProgress);
                        }
                    }

                    // Serialize updated progress back to JSON (only progress fields)
                    entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Check if exception is related to JSONB type conversion error
        /// </summary>
        private static bool IsJsonbTypeError(Exception ex)
        {
            var errorMessage = ex.ToString().ToLower();
            return errorMessage.Contains("42804") ||
                   errorMessage.Contains("jsonb") ||
                   (errorMessage.Contains("column") && errorMessage.Contains("text") && errorMessage.Contains("expression")) ||
                   ex.GetType().Name.Contains("Postgres");
        }

        /// <summary>
        /// Safely append text to Notes field with length validation
        /// Ensures the total length doesn't exceed the database constraint (1000 characters)
        /// </summary>
        private static void SafeAppendToNotes(Onboarding entity, string noteText)
        {
            const int maxNotesLength = 1000;

            if (string.IsNullOrEmpty(noteText))
                return;

            var currentNotes = entity.Notes ?? string.Empty;
            var newContent = string.IsNullOrEmpty(currentNotes)
                ? noteText
                : $"{currentNotes}\n{noteText}";

            // If the new content exceeds the limit, truncate it intelligently
            if (newContent.Length > maxNotesLength)
            {
                // Try to keep the most recent notes by truncating from the beginning
                var truncationMessage = "[...truncated older notes...]\n";
                var availableSpace = maxNotesLength - truncationMessage.Length - noteText.Length - 1; // -1 for newline

                if (availableSpace > 0 && currentNotes.Length > availableSpace)
                {
                    // Keep the most recent part of existing notes
                    var recentNotes = currentNotes.Substring(currentNotes.Length - availableSpace);
                    // Find the first newline to avoid cutting in the middle of a note
                    var firstNewlineIndex = recentNotes.IndexOf('\n');
                    if (firstNewlineIndex > 0)
                    {
                        recentNotes = recentNotes.Substring(firstNewlineIndex + 1);
                    }
                    entity.Notes = $"{truncationMessage}{recentNotes}\n{noteText}";
                }
                else
                {
                    // If even the new note is too long, truncate it
                    var maxNewNoteLength = maxNotesLength - truncationMessage.Length - 1;
                    if (maxNewNoteLength > 0)
                    {
                        entity.Notes = $"{truncationMessage}{noteText.Substring(0, maxNewNoteLength)}";
                    }
                    else
                    {
                        // Fallback: just use the first part of the new note
                        entity.Notes = noteText.Substring(0, Math.Min(noteText.Length, maxNotesLength));
                    }
                }
            }
            else
            {
                entity.Notes = newContent;
            }
        }

        /// <summary>
        /// Safely update onboarding entity with JSONB compatibility
        /// This method handles the JSONB type conversion issue for stages_progress_json
        /// </summary>
        private async Task<bool> SafeUpdateOnboardingAsync(Onboarding entity)
        {
            try
            {
                LoggingExtensions.WriteLine($"[DEBUG] SafeUpdateOnboardingAsync - Updating Onboarding {entity.Id}: CurrentStageId={entity.CurrentStageId}, Status={entity.Status}");

                // Always use the JSONB-safe approach to avoid type conversion errors
                var db = _onboardingRepository.GetSqlSugarClient();

                // First, update permission JSONB fields AND permission mode/type together with explicit JSONB casting
                // This ensures that the database constraints are satisfied in a single transaction
                try
                {
                    var permissionSql = @"
                        UPDATE ff_onboarding 
                        SET view_teams = @ViewTeams::jsonb,
                            view_users = @ViewUsers::jsonb,
                            operate_teams = @OperateTeams::jsonb,
                            operate_users = @OperateUsers::jsonb,
                            view_permission_mode = @ViewPermissionMode,
                            view_permission_subject_type = @ViewPermissionSubjectType,
                            operate_permission_subject_type = @OperatePermissionSubjectType,
                            use_same_team_for_operate = @UseSameTeamForOperate
                        WHERE id = @Id";

                    await db.Ado.ExecuteCommandAsync(permissionSql, new
                    {
                        ViewTeams = entity.ViewTeams,
                        ViewUsers = entity.ViewUsers,
                        OperateTeams = entity.OperateTeams,
                        OperateUsers = entity.OperateUsers,
                        ViewPermissionMode = (int)entity.ViewPermissionMode,
                        ViewPermissionSubjectType = (int)entity.ViewPermissionSubjectType,
                        OperatePermissionSubjectType = (int)entity.OperatePermissionSubjectType,
                        UseSameTeamForOperate = entity.UseSameTeamForOperate,
                        Id = entity.Id
                    });
                }
                catch (Exception permEx)
                {
                    LoggingExtensions.WriteLine($"Error: Failed to update permission JSONB fields: {permEx.Message}");
                    // This is critical, so we should throw
                    throw new CRMException($"Failed to update permission fields: {permEx.Message}");
                }

                // Then update all other fields (permission fields were already updated above)
                // Now the constraint will be satisfied because JSONB fields and permission modes are already updated
                LoggingExtensions.WriteLine($"[DEBUG] SafeUpdateOnboardingAsync - About to update repository with CurrentStageId={entity.CurrentStageId}");

                // Serialize back to JSON (only progress fields)
                await FilterValidStagesProgress(entity);

                // IMPORTANT: Get fresh audit values and save them to local variables BEFORE any update
                // This prevents SqlSugar from resetting entity values
                var modifyDate = DateTimeOffset.UtcNow;
                var modifyBy = _operatorContextService.GetOperatorDisplayName();
                var modifyUserId = _operatorContextService.GetOperatorId();

                LoggingExtensions.WriteLine($"[DEBUG] SafeUpdateOnboardingAsync - Using ModifyBy: '{modifyBy}'");

                // Update entity with fresh audit values
                entity.ModifyDate = modifyDate;
                entity.ModifyBy = modifyBy;
                entity.ModifyUserId = modifyUserId;

                // Use UpdateColumns with the entity (now with fresh audit values)
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
                        it.CustomFieldsJson,
                        it.Notes,
                        it.IsActive,
                        // Note: ViewPermissionSubjectType, OperatePermissionSubjectType, ViewPermissionMode were already updated above
                        // Note: ViewTeams, ViewUsers, OperateTeams, OperateUsers were already updated above
                        it.ModifyDate,
                        it.ModifyBy,
                        it.ModifyUserId,
                        it.IsValid
                    });

                // WORKAROUND: Update audit fields again with manual SQL using saved local variables
                // This ensures the correct values are used, not the entity's potentially reset values
                try
                {
                    var auditSql = @"
                        UPDATE ff_onboarding 
                        SET modify_date = @ModifyDate,
                            modify_by = @ModifyBy,
                            modify_user_id = @ModifyUserId
                        WHERE id = @Id";

                    await db.Ado.ExecuteCommandAsync(auditSql, new
                    {
                        ModifyDate = modifyDate,  // Use local variable, not entity property
                        ModifyBy = modifyBy,      // Use local variable, not entity property
                        ModifyUserId = modifyUserId,  // Use local variable, not entity property
                        Id = entity.Id
                    });

                    LoggingExtensions.WriteLine($"[DEBUG] SafeUpdateOnboardingAsync - Audit fields updated via SQL: ModifyBy='{modifyBy}'");
                }
                catch (Exception auditEx)
                {
                    LoggingExtensions.WriteLine($"Warning: Failed to update audit fields via SQL: {auditEx.Message}");
                    // Don't fail the entire update if audit field update fails
                }

                // Update stages_progress_json separately with explicit JSONB casting
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    try
                    {
                        var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                        await db.Ado.ExecuteCommandAsync(progressSql, new
                        {
                            StagesProgressJson = entity.StagesProgressJson,
                            Id = entity.Id
                        });
                    }
                    catch (Exception progressEx)
                    {
                        // Log but don't fail the main update
                        LoggingExtensions.WriteLine($"Warning: Failed to update stages_progress_json: {progressEx.Message}");
                        // Try alternative approach with parameter substitution
                        try
                        {
                            var escapedJson = entity.StagesProgressJson.Replace("'", "''");
                            var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                            await db.Ado.ExecuteCommandAsync(directSql);
                        }
                        catch (Exception directEx)
                        {
                            LoggingExtensions.WriteLine($"Error: Both parameterized and direct JSONB update failed: {directEx.Message}");
                        }
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
        /// Ensure stages progress is properly initialized and synced with workflow
        /// This method handles cases where stages progress might be empty or outdated
        /// </summary>
        private async Task EnsureStagesProgressInitializedAsync(Onboarding entity)
        {
            // Prevent infinite recursion using thread-safe entity tracking
            lock (_initializationLock)
            {
                if (_initializingEntities.Contains(entity.Id))
                {
                    return; // Already being initialized, avoid recursion
                }
                _initializingEntities.Add(entity.Id);
            }

            try
            {
                // Load current stages progress
                LoadStagesProgressFromJson(entity);

                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "No stages found for workflow");
                }

                // If stages progress is empty, initialize it
                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    await InitializeStagesProgressAsync(entity, stages);
                }
                else
                {
                    // Sync with workflow stages (handle new stages addition)
                    await SyncStagesProgressWithWorkflowAsync(entity);
                }

                // Always enrich with stage data to ensure consistency
                await EnrichStagesProgressWithStageDataAsync(entity);

                // NOTE: Do NOT call SafeUpdateOnboardingAsync here to avoid recursion
                // The caller is responsible for saving changes
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the main flow
                // The validation will catch if stages progress is still missing
            }
            finally
            {
                // Remove from initialization tracking
                lock (_initializationLock)
                {
                    _initializingEntities.Remove(entity.Id);
                }
            }
        }

        /// <summary>
        /// Serialize stages progress to JSON - only stores progress state, not stage configuration
        /// Stage configuration fields (stageName, stageOrder, etc.) are excluded via JsonIgnore attributes
        /// and are populated dynamically from Stage entities when needed.
        /// </summary>
        private string SerializeStagesProgress(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return "[]";
                }

                // Debug: Check input data before serialization
                LoggingExtensions.WriteLine($"[DEBUG] SerializeStagesProgress - Input data:");
                foreach (var sp in stagesProgress)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Input StageProgress: StageId={sp.StageId}, Status={sp.Status}, IsCurrent={sp.IsCurrent}");
                }

                // Serialize OnboardingStageProgress objects with JsonIgnore attributes respected
                // Only progress-related fields will be included, not stage configuration fields
                var result = System.Text.Json.JsonSerializer.Serialize(stagesProgress, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Debug: Check final result
                LoggingExtensions.WriteLine($"[DEBUG] SerializeStagesProgress - Final JSON result:");
                LoggingExtensions.WriteLine($"[DEBUG] {result}");

                return result;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                return "[]";
            }
        }

        /// <summary>
        /// Create default UserInvitation record without sending email
        /// </summary>
        /// <param name="onboarding">Onboarding entity</param>
        private async Task CreateDefaultUserInvitationAsync(Onboarding onboarding)
        {
            try
            {
                // Determine which email to use (prefer ContactEmail, fallback to LeadEmail)
                var emailToUse = !string.IsNullOrWhiteSpace(onboarding.ContactEmail)
                    ? onboarding.ContactEmail
                    : onboarding.LeadEmail;

                // Skip if no email is available
                if (string.IsNullOrWhiteSpace(emailToUse))
                {
                    return;
                }

                // Check if invitation already exists for this onboarding and email
                var existingInvitation = await _userInvitationRepository.GetByEmailAndOnboardingIdAsync(emailToUse, onboarding.Id);
                if (existingInvitation != null)
                {
                    // Invitation already exists, skip creation
                    return;
                }

                // Create new UserInvitation record
                var invitation = new UserInvitation
                {
                    OnboardingId = onboarding.Id,
                    Email = emailToUse,
                    InvitationToken = CryptoHelper.GenerateSecureToken(),
                    Status = "Pending",
                    SentDate = null, // Leave empty - will be set when invitation is actually sent
                    TokenExpiry = null, // No expiry
                    SendCount = 0, // Not sent via email
                    TenantId = onboarding.TenantId,
                    Notes = "Auto-created default invitation (no email sent)"
                };

                // Generate short URL ID and invitation URL
                invitation.ShortUrlId = CryptoHelper.GenerateShortUrlId(
                    onboarding.Id,
                    emailToUse,
                    invitation.InvitationToken);

                // Generate invitation URL (using default base URL)
                invitation.InvitationUrl = GenerateShortInvitationUrl(
                    invitation.ShortUrlId,
                    onboarding.TenantId ?? "DEFAULT",
                    onboarding.AppCode ?? "DEFAULT");

                // Initialize create info
                invitation.InitCreateInfo(_userContext);

                // Insert the invitation record
                await _userInvitationRepository.InsertAsync(invitation);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the onboarding creation
                // This is a non-critical operation
                // Note: In a real implementation, you would use structured logging here
            }
        }

        /// <summary>
        /// Get current time with +08:00 timezone (China Standard Time)
        /// </summary>
        /// <returns>Current time with +08:00 offset</returns>
        private DateTimeOffset GetCurrentTimeWithTimeZone()
        {
            // China Standard Time is UTC+8
            var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            var utcNow = DateTime.UtcNow;
            var chinaTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, chinaTimeZone);

            // Create DateTimeOffset with +08:00 offset
            return new DateTimeOffset(chinaTime, TimeSpan.FromHours(8));
        }

        /// <summary>
        /// Generate short invitation URL (copied from UserInvitationService)
        /// </summary>
        /// <param name="shortUrlId">Short URL ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="appCode">App code</param>
        /// <param name="baseUrl">Base URL (optional)</param>
        /// <returns>Generated invitation URL</returns>
        private string GenerateShortInvitationUrl(string shortUrlId, string tenantId, string appCode, string? baseUrl = null)
        {
            // Use provided base URL or fall back to a default one
            var effectiveBaseUrl = baseUrl ?? "https://portal.flowflex.com"; // Default base URL

            // Generate the short URL format: {baseUrl}/portal/{tenantId}/{appCode}/invite/{shortUrlId}
            return $"{effectiveBaseUrl.TrimEnd('/')}/portal/{tenantId}/{appCode}/invite/{shortUrlId}";
        }
        /// <summary>
        /// Update AI Summary for a specific stage in onboarding's stagesProgress
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <param name="aiSummary">AI Summary content</param>
        /// <param name="generatedAt">Generated timestamp</param>
        /// <param name="confidence">Confidence score</param>
        /// <param name="modelUsed">AI model used</param>
        /// <returns>Success status</returns>
        public async Task<bool> UpdateOnboardingStageAISummaryAsync(long onboardingId, long stageId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed)
        {
            try
            {
                // Get current onboarding without tenant filter (for background tasks where HttpContext is not available)
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(onboardingId);
                if (onboarding == null)
                {
                    LoggingExtensions.WriteLine($"Onboarding {onboardingId} not found for AI summary update");
                    return false;
                }

                // Sync stages progress with workflow to ensure all stages are included
                await SyncStagesProgressWithWorkflowAsync(onboarding);

                // Load stages progress from JSON (after sync)
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress == null)
                {
                    LoggingExtensions.WriteLine($"Stage progress not found for stage {stageId} in onboarding {onboardingId} even after sync. Available stages: {string.Join(", ", onboarding.StagesProgress?.Select(sp => sp.StageId.ToString()) ?? Array.Empty<string>())}");
                    return false;
                }

                // Update AI summary fields - always overwrite for Onboarding-specific summaries
                stageProgress.AiSummary = aiSummary;
                stageProgress.AiSummaryGeneratedAt = generatedAt;
                stageProgress.AiSummaryConfidence = (decimal?)confidence;
                stageProgress.AiSummaryModel = modelUsed;
                stageProgress.AiSummaryData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    trigger = "Stream API onboarding update",
                    generatedAt = generatedAt,
                    confidence = confidence,
                    model = modelUsed,
                    onboardingSpecific = true
                });

                // Save stages progress back to JSON
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                if (result)
                {
                    LoggingExtensions.WriteLine($"鉁?Successfully updated AI summary for stage {stageId} in onboarding {onboardingId}");
                }
                else
                {
                    LoggingExtensions.WriteLine($"鉂?Failed to save AI summary for stage {stageId} in onboarding {onboardingId} - database update failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingExtensions.WriteLine($"鉂?Failed to update AI summary for stage {stageId} in onboarding {onboardingId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update custom fields for a specific stage in onboarding's stagesProgress
        /// Updates CustomEstimatedDays and CustomEndTime fields
        /// </summary>
        public async Task<bool> UpdateStageCustomFieldsAsync(long onboardingId, UpdateStageCustomFieldsInputDto input)
        {
            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Load stages progress from JSON
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == input.StageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {input.StageId} not found in onboarding {onboardingId}");
                }

                // Capture original values for comparison
                var originalEstimatedDays = stageProgress.CustomEstimatedDays;
                var originalEndTime = stageProgress.CustomEndTime;
                var originalAssignee = stageProgress.Assignee?.ToList() ?? new List<string>();
                var originalCoAssignees = stageProgress.CoAssignees?.ToList() ?? new List<string>();

                // Capture before data for change log
                var beforeData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    Assignee = stageProgress.Assignee,
                    CoAssignees = stageProgress.CoAssignees,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                });

                // Update custom fields
                stageProgress.CustomEstimatedDays = input.CustomEstimatedDays;
                stageProgress.CustomEndTime = input.CustomEndTime;

                // Update Assignee if provided
                if (input.Assignee != null)
                {
                    stageProgress.Assignee = input.Assignee;
                }

                // Update CoAssignees if provided
                if (input.CoAssignees != null)
                {
                    stageProgress.CoAssignees = input.CoAssignees;
                }

                // Add notes if provided
                if (!string.IsNullOrEmpty(input.Notes))
                {
                    var currentTime = DateTimeOffset.UtcNow;
                    var currentUser = GetCurrentUserName();
                    var updateNote = $"[Custom fields updated {currentTime:yyyy-MM-dd HH:mm:ss} by {currentUser}] {input.Notes}";

                    if (string.IsNullOrEmpty(stageProgress.Notes))
                    {
                        stageProgress.Notes = updateNote;
                    }
                    else
                    {
                        stageProgress.Notes += $"\n{updateNote}";
                    }
                }

                // Update last modified fields
                stageProgress.LastUpdatedTime = DateTimeOffset.UtcNow;
                stageProgress.LastUpdatedBy = GetCurrentUserName();

                // Capture after data for change log
                var afterData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    Assignee = stageProgress.Assignee,
                    CoAssignees = stageProgress.CoAssignees,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                });

                // Save stages progress back to JSON
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                // Log the operation if update was successful
                if (result)
                {
                    var changedFields = new List<string>();
                    var changeDetails = new List<string>();

                    // Check for actual changes in CustomEstimatedDays
                    if (input.CustomEstimatedDays.HasValue && originalEstimatedDays != input.CustomEstimatedDays)
                    {
                        changedFields.Add("CustomEstimatedDays");
                        var beforeValue = originalEstimatedDays?.ToString() ?? "null";
                        changeDetails.Add($"EstimatedDays: {beforeValue} 鈫?{input.CustomEstimatedDays}");
                    }

                    // Check for actual changes in CustomEndTime
                    if (input.CustomEndTime.HasValue && originalEndTime != input.CustomEndTime)
                    {
                        changedFields.Add("CustomEndTime");
                        var beforeValue = originalEndTime?.ToString("yyyy-MM-dd HH:mm") ?? "null";
                        changeDetails.Add($"EndTime: {beforeValue} → {input.CustomEndTime?.ToString("yyyy-MM-dd HH:mm")}");
                    }

                    // Check for actual changes in Assignee
                    if (input.Assignee != null && !originalAssignee.SequenceEqual(input.Assignee))
                    {
                        changedFields.Add("Assignee");
                        var beforeValue = originalAssignee.Any() ? string.Join(", ", originalAssignee) : "empty";
                        var afterValue = input.Assignee.Any() ? string.Join(", ", input.Assignee) : "empty";
                        changeDetails.Add($"Assignee: {beforeValue} → {afterValue}");
                    }

                    // Check for actual changes in CoAssignees
                    if (input.CoAssignees != null && !originalCoAssignees.SequenceEqual(input.CoAssignees))
                    {
                        changedFields.Add("CoAssignees");
                        var beforeValue = originalCoAssignees.Any() ? string.Join(", ", originalCoAssignees) : "empty";
                        var afterValue = input.CoAssignees.Any() ? string.Join(", ", input.CoAssignees) : "empty";
                        changeDetails.Add($"CoAssignees: {beforeValue} → {afterValue}");
                    }

                    // Check if notes were added
                    if (!string.IsNullOrEmpty(input.Notes))
                    {
                        changedFields.Add("Notes");
                        changeDetails.Add("Notes: Added");
                    }

                    // Only log if there are actual changes or notes added
                    if (changeDetails.Any())
                    {
                        var operationTitle = $"Update Stage Custom Fields: {string.Join(", ", changeDetails)}";
                        var operationDescription = $"Updated custom fields for stage {input.StageId} in onboarding {onboardingId}";

                        // Log the stage custom fields update operation
                        await _operationChangeLogService.LogOperationAsync(
                            operationType: FlowFlex.Domain.Shared.Enums.OW.OperationTypeEnum.StageUpdate,
                            businessModule: BusinessModuleEnum.Stage,
                            businessId: input.StageId,
                            onboardingId: onboardingId,
                            stageId: input.StageId,
                            operationTitle: operationTitle,
                            operationDescription: operationDescription,
                            beforeData: beforeData,
                            afterData: afterData,
                            changedFields: changedFields,
                            extendedData: JsonSerializer.Serialize(new
                            {
                                Notes = input.Notes,
                                OperationSource = "UpdateStageCustomFieldsAsync",
                                HasActualChanges = true
                            })
                        );
                    }
                    // Note: No log entry is created when there are no actual changes
                    // This reduces log noise and focuses on meaningful operations
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log the failed operation
                await _operationChangeLogService.LogOperationAsync(
                    operationType: FlowFlex.Domain.Shared.Enums.OW.OperationTypeEnum.StageUpdate,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: input.StageId,
                    onboardingId: onboardingId,
                    stageId: input.StageId,
                    operationTitle: "Update Stage Custom Fields",
                    operationDescription: $"Failed to update custom fields for stage {input.StageId} in onboarding {onboardingId}",
                    operationStatus: OperationStatusEnum.Failed,
                    errorMessage: ex.Message
                );

                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to update custom fields for stage {input.StageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Save a specific stage in onboarding's stagesProgress
        /// Updates the stage's IsSaved, SaveTime, and SavedById fields
        /// </summary>
        public async Task<bool> SaveStageAsync(long onboardingId, long stageId)
        {
            // Check permission
            if (!await CheckCaseOperatePermissionAsync(onboardingId))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed,
                    $"User does not have permission to operate on case {onboardingId}");
            }

            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Load stages progress from JSON
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {stageId} not found in onboarding {onboardingId}");
                }

                // Update save fields
                stageProgress.IsSaved = true;
                stageProgress.SaveTime = DateTimeOffset.UtcNow;
                stageProgress.SavedById = GetCurrentUserId()?.ToString();
                stageProgress.SavedBy = GetCurrentUserName();

                // Set StartTime if not already set (only during save operations)
                // This ensures StartTime is only set when user actually saves or completes work
                if (!stageProgress.StartTime.HasValue)
                {
                    stageProgress.StartTime = DateTimeOffset.UtcNow;
                }

                // IMPORTANT: If this is the current stage and CurrentStageStartTime is not set, set it now
                if (stageProgress.StageId == onboarding.CurrentStageId && !onboarding.CurrentStageStartTime.HasValue)
                {
                    onboarding.CurrentStageStartTime = stageProgress.StartTime;
                    LoggingExtensions.WriteLine($"[DEBUG] SaveStageAsync - Set CurrentStageStartTime to {onboarding.CurrentStageStartTime} for Stage {stageId}");
                }

                // Save stages progress back to JSON
                await FilterValidStagesProgress(onboarding);
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                // Log stage save to operation_change_log
                if (result)
                {
                    try
                    {
                        var stage = await _stageRepository.GetByIdAsync(stageId);
                        var beforeData = new
                        {
                            StageId = stageId,
                            StageName = stage?.Name,
                            IsSaved = false,
                            SaveTime = (DateTimeOffset?)null
                        };

                        var afterData = new
                        {
                            StageId = stageId,
                            StageName = stage?.Name,
                            IsSaved = true,
                            SaveTime = stageProgress.SaveTime,
                            SavedBy = stageProgress.SavedBy,
                            StartTime = stageProgress.StartTime
                        };

                        var extendedData = new
                        {
                            WorkflowId = onboarding.WorkflowId,
                            IsCurrentStage = stageProgress.StageId == onboarding.CurrentStageId,
                            CurrentStageStartTime = onboarding.CurrentStageStartTime,
                            Source = "manual_save"
                        };

                        await _operationChangeLogService.LogOperationAsync(
                            operationType: OperationTypeEnum.StageSave,
                            businessModule: BusinessModuleEnum.Stage,
                            businessId: stageId,
                            onboardingId: onboardingId,
                            stageId: stageId,
                            operationTitle: $"Stage Saved: {stage?.Name ?? "Unknown"}",
                            operationDescription: $"Stage '{stage?.Name}' has been saved by {stageProgress.SavedBy}",
                            beforeData: System.Text.Json.JsonSerializer.Serialize(beforeData),
                            afterData: System.Text.Json.JsonSerializer.Serialize(afterData),
                            changedFields: new List<string> { "IsSaved", "SaveTime", "SavedBy" },
                            extendedData: System.Text.Json.JsonSerializer.Serialize(extendedData)
                        );

                        _logger.LogInformation("Stage save log recorded: OnboardingId={OnboardingId}, StageId={StageId}, StageName={StageName}",
                            onboardingId, stageId, stage?.Name);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to record Stage save log: OnboardingId={OnboardingId}, StageId={StageId}",
                            onboardingId, stageId);
                        // Don't re-throw to avoid breaking the main flow
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to save stage {stageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// 杩囨护鏃犳晥鐨?stagesProgress锛屼繚鐣欏綋鍓?workflow 涓湁鏁?stage 鐨勮繘搴?
        /// </summary>
        private async Task FilterValidStagesProgress(Onboarding entity)
        {
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            if (stages == null || !stages.Any()) return;
            var validStageIds = stages.Select(s => s.Id).ToHashSet();
            entity.StagesProgress?.RemoveAll(x => !validStageIds.Contains(x.StageId));
        }

        /// <summary>
        /// Helper method to populate workflow/stage names and calculate current stage end time for OnboardingOutputDto lists
        /// </summary>
        /// <summary>
        /// Check if current user has permission to operate on a case
        /// </summary>

        /// <summary>
        /// Parse DefaultAssignee JSON string to List of user IDs
        /// </summary>
        private List<string> ParseDefaultAssignee(string defaultAssigneeJson)
        {
            if (string.IsNullOrWhiteSpace(defaultAssigneeJson))
            {
                return new List<string>();
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Handle double-serialized JSON
                var jsonString = defaultAssigneeJson.Trim();
                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    jsonString = JsonSerializer.Deserialize<string>(jsonString, options) ?? "[]";
                }

                // Try to parse as array
                if (jsonString.StartsWith("["))
                {
                    var result = JsonSerializer.Deserialize<List<string>>(jsonString, options);
                    return result ?? new List<string>();
                }

                // If it's a single value, wrap it in a list
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    return new List<string> { jsonString };
                }

                return new List<string>();
            }
            catch (JsonException)
            {
                // If parsing fails, return empty list
                return new List<string>();
            }
        }

        /// <summary>
        /// Get CoAssignees filtered to exclude any IDs already in DefaultAssignee
        /// </summary>
        private List<string> GetFilteredCoAssignees(string coAssigneesJson, string defaultAssigneeJson)
        {
            var coAssignees = ParseDefaultAssignee(coAssigneesJson);
            var defaultAssignees = ParseDefaultAssignee(defaultAssigneeJson);

            if (!defaultAssignees.Any())
            {
                return coAssignees;
            }

            // Filter out any IDs that are already in DefaultAssignee
            return coAssignees
                .Where(id => !defaultAssignees.Contains(id))
                .ToList();
        }
    }
}

