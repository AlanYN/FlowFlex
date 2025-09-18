using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Stages Progress Sync Service
    /// Handles synchronization of onboarding stages progress when workflow stages are modified
    /// </summary>
    public class StagesProgressSyncService : IStagesProgressSyncService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly ILogger<StagesProgressSyncService> _logger;

        public StagesProgressSyncService(
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            ILogger<StagesProgressSyncService> logger)
        {
            _onboardingRepository = onboardingRepository;
            _stageRepository = stageRepository;
            _logger = logger;
        }

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage update
        /// </summary>
        public async Task<int> SyncAfterStageUpdateAsync(long workflowId, long? updatedStageId = null)
        {
            try
            {
                _logger.LogInformation("Starting sync after stage update for workflow {WorkflowId}, stage {StageId}",
                    workflowId, updatedStageId);

                // Get all active onboardings for this workflow
                var onboardings = await _onboardingRepository.GetListByWorkflowIdAsync(workflowId);
                if (onboardings == null || !onboardings.Any())
                {
                    _logger.LogInformation("No onboardings found for workflow {WorkflowId}", workflowId);
                    return 0;
                }

                var syncedCount = 0;
                var failedCount = 0;

                foreach (var onboarding in onboardings)
                {
                    try
                    {
                        await SyncSingleOnboardingInternalAsync(onboarding);
                        syncedCount++;

                        _logger.LogDebug("Successfully synced stages progress for onboarding {OnboardingId}",
                            onboarding.Id);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex,
                            "Failed to sync stages progress for onboarding {OnboardingId} after stage update",
                            onboarding.Id);

                        // Log the error but continue with other onboardings
                    }
                }

                _logger.LogInformation(
                    "Completed sync after stage update for workflow {WorkflowId}. Synced: {SyncedCount}, Failed: {FailedCount}",
                    workflowId, syncedCount, failedCount);

                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync after stage update for workflow {WorkflowId}", workflowId);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to sync stages progress after stage update: {ex.Message}");
            }
        }

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage deletion
        /// </summary>
        public async Task<int> SyncAfterStageDeleteAsync(long workflowId, long deletedStageId)
        {
            try
            {
                _logger.LogInformation("Starting sync after stage deletion for workflow {WorkflowId}, deleted stage {StageId}",
                    workflowId, deletedStageId);

                // Get all active onboardings for this workflow
                var onboardings = await _onboardingRepository.GetListByWorkflowIdAsync(workflowId);
                if (onboardings == null || !onboardings.Any())
                {
                    _logger.LogInformation("No onboardings found for workflow {WorkflowId}", workflowId);
                    return 0;
                }

                var syncedCount = 0;
                var failedCount = 0;

                foreach (var onboarding in onboardings)
                {
                    try
                    {
                        // Remove the deleted stage from stages progress
                        await RemoveDeletedStageFromProgressAsync(onboarding.Id, deletedStageId);

                        // Sync the remaining stages progress
                        await SyncSingleOnboardingInternalAsync(onboarding);
                        syncedCount++;

                        _logger.LogDebug("Successfully synced stages progress for onboarding {OnboardingId} after stage deletion",
                            onboarding.Id);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex,
                            "Failed to sync stages progress for onboarding {OnboardingId} after stage deletion",
                            onboarding.Id);


                    }
                }

                _logger.LogInformation(
                    "Completed sync after stage deletion for workflow {WorkflowId}. Synced: {SyncedCount}, Failed: {FailedCount}",
                    workflowId, syncedCount, failedCount);

                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync after stage deletion for workflow {WorkflowId}", workflowId);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to sync stages progress after stage deletion: {ex.Message}");
            }
        }

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage sorting
        /// </summary>
        public async Task<int> SyncAfterStagesSortAsync(long workflowId, List<long> stageIds)
        {
            try
            {
                _logger.LogInformation("Starting sync after stages sorting for workflow {WorkflowId}, affected stages: {StageIds}",
                    workflowId, string.Join(",", stageIds));

                // Get all active onboardings for this workflow
                var onboardings = await _onboardingRepository.GetListByWorkflowIdAsync(workflowId);
                if (onboardings == null || !onboardings.Any())
                {
                    _logger.LogInformation("No onboardings found for workflow {WorkflowId}", workflowId);
                    return 0;
                }

                var syncedCount = 0;
                var failedCount = 0;

                foreach (var onboarding in onboardings)
                {
                    try
                    {
                        await SyncSingleOnboardingInternalAsync(onboarding);
                        syncedCount++;

                        _logger.LogDebug("Successfully synced stages progress for onboarding {OnboardingId} after stages sorting",
                            onboarding.Id);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex,
                            "Failed to sync stages progress for onboarding {OnboardingId} after stages sorting",
                            onboarding.Id);


                    }
                }

                _logger.LogInformation(
                    "Completed sync after stages sorting for workflow {WorkflowId}. Synced: {SyncedCount}, Failed: {FailedCount}",
                    workflowId, syncedCount, failedCount);

                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync after stages sorting for workflow {WorkflowId}", workflowId);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to sync stages progress after stages sorting: {ex.Message}");
            }
        }

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage combination
        /// </summary>
        public async Task<int> SyncAfterStagesCombineAsync(long workflowId, List<long> deletedStageIds, long newStageId)
        {
            try
            {
                _logger.LogInformation(
                    "Starting sync after stages combination for workflow {WorkflowId}, deleted stages: {DeletedStageIds}, new stage: {NewStageId}",
                    workflowId, string.Join(",", deletedStageIds), newStageId);

                // Get all active onboardings for this workflow
                var onboardings = await _onboardingRepository.GetListByWorkflowIdAsync(workflowId);
                if (onboardings == null || !onboardings.Any())
                {
                    _logger.LogInformation("No onboardings found for workflow {WorkflowId}", workflowId);
                    return 0;
                }

                var syncedCount = 0;
                var failedCount = 0;

                foreach (var onboarding in onboardings)
                {
                    try
                    {
                        // Remove deleted stages and handle the combined stage logic
                        await HandleStagesCombinationAsync(onboarding.Id, deletedStageIds, newStageId);

                        // Sync the updated stages progress
                        await SyncSingleOnboardingInternalAsync(onboarding);
                        syncedCount++;

                        _logger.LogDebug("Successfully synced stages progress for onboarding {OnboardingId} after stages combination",
                            onboarding.Id);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex,
                            "Failed to sync stages progress for onboarding {OnboardingId} after stages combination",
                            onboarding.Id);


                    }
                }

                _logger.LogInformation(
                    "Completed sync after stages combination for workflow {WorkflowId}. Synced: {SyncedCount}, Failed: {FailedCount}",
                    workflowId, syncedCount, failedCount);

                return syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync after stages combination for workflow {WorkflowId}", workflowId);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to sync stages progress after stages combination: {ex.Message}");
            }
        }

        /// <summary>
        /// Sync stages progress for a specific onboarding
        /// </summary>
        public async Task<bool> SyncSingleOnboardingAsync(long onboardingId)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    _logger.LogWarning("Onboarding {OnboardingId} not found", onboardingId);
                    return false;
                }

                await SyncSingleOnboardingInternalAsync(onboarding);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync stages progress for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Batch sync stages progress for multiple onboardings
        /// </summary>
        public async Task<int> BatchSyncOnboardingsAsync(List<long> onboardingIds)
        {
            if (onboardingIds == null || !onboardingIds.Any())
            {
                return 0;
            }

            var syncedCount = 0;

            foreach (var onboardingId in onboardingIds)
            {
                try
                {
                    if (await SyncSingleOnboardingAsync(onboardingId))
                    {
                        syncedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync onboarding {OnboardingId} in batch operation", onboardingId);
                }
            }

            return syncedCount;
        }

        #region Private Helper Methods

        /// <summary>
        /// Remove deleted stage from onboarding stages progress
        /// </summary>
        private async Task RemoveDeletedStageFromProgressAsync(long onboardingId, long deletedStageId)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null) return;

                // Load current stages progress
                if (onboarding.StagesProgress != null)
                {
                    var updatedProgress = onboarding.StagesProgress
                        .Where(sp => sp.StageId != deletedStageId)
                        .ToList();

                    onboarding.StagesProgress = updatedProgress;

                    // Update the JSON as well - this should be handled by the service layer
                    // For now, we'll let the sync method handle the JSON serialization
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove deleted stage {StageId} from onboarding {OnboardingId}",
                    deletedStageId, onboardingId);
                throw;
            }
        }

        /// <summary>
        /// Handle stages combination logic for an onboarding
        /// </summary>
        private async Task HandleStagesCombinationAsync(long onboardingId, List<long> deletedStageIds, long newStageId)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null) return;

                // Load current stages progress
                if (onboarding.StagesProgress != null)
                {
                    // Find the combined stages in the current progress
                    var combinedStages = onboarding.StagesProgress
                        .Where(sp => deletedStageIds.Contains(sp.StageId))
                        .ToList();

                    // Remove the combined stages
                    var updatedProgress = onboarding.StagesProgress
                        .Where(sp => !deletedStageIds.Contains(sp.StageId))
                        .ToList();

                    // Determine the status for the new combined stage based on the combined stages
                    var newStageStatus = DetermineCombinedStageStatus(combinedStages);

                    // The new stage will be added during the sync process
                    // We just need to remove the old ones here
                    onboarding.StagesProgress = updatedProgress;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle stages combination for onboarding {OnboardingId}", onboardingId);
                throw;
            }
        }

        /// <summary>
        /// Determine the status for a new combined stage based on the statuses of combined stages
        /// </summary>
        private string DetermineCombinedStageStatus(List<Domain.Entities.OW.OnboardingStageProgress> combinedStages)
        {
            if (!combinedStages.Any()) return "Pending";

            // If any stage is completed, the combined stage should be completed
            if (combinedStages.Any(s => s.IsCompleted)) return "Completed";

            // If any stage is in progress, the combined stage should be in progress
            if (combinedStages.Any(s => s.Status == "InProgress")) return "InProgress";

            // Otherwise, use pending
            return "Pending";
        }



        /// <summary>
        /// Internal method to sync stages progress for a single onboarding
        /// This method breaks the circular dependency by directly working with repositories
        /// </summary>
        private async Task SyncSingleOnboardingInternalAsync(Domain.Entities.OW.Onboarding onboarding)
        {
            try
            {
                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    _logger.LogWarning("No stages found for workflow {WorkflowId}", onboarding.WorkflowId);
                    return;
                }

                // Load current stages progress from JSON
                var currentProgress = LoadStagesProgressFromJson(onboarding);

                _logger.LogDebug("Loaded {ProgressCount} stages progress entries for onboarding {OnboardingId}",
                    currentProgress.Count, onboarding.Id);

                // Sync with current workflow stages
                var updatedProgress = SyncProgressWithStages(currentProgress, stages);

                _logger.LogDebug("Synced to {UpdatedCount} stages progress entries for onboarding {OnboardingId}",
                    updatedProgress.Count, onboarding.Id);

                // Update the onboarding entity
                onboarding.StagesProgress = updatedProgress;
                var jsonData = SerializeStagesProgress(updatedProgress);

                // Save to database using JSONB-safe approach
                await SafeUpdateOnboardingWithJsonbAsync(onboarding, jsonData);

                _logger.LogDebug("Successfully synced stages progress for onboarding {OnboardingId}", onboarding.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync stages progress for onboarding {OnboardingId}", onboarding.Id);
                throw;
            }
        }

        /// <summary>
        /// Load stages progress from JSON string
        /// </summary>
        private List<Domain.Entities.OW.OnboardingStageProgress> LoadStagesProgressFromJson(Domain.Entities.OW.Onboarding onboarding)
        {
            if (string.IsNullOrEmpty(onboarding.StagesProgressJson))
            {
                _logger.LogDebug("No stages progress JSON found for onboarding {OnboardingId}", onboarding.Id);
                return new List<Domain.Entities.OW.OnboardingStageProgress>();
            }

            try
            {
                _logger.LogDebug("Deserializing stages progress JSON for onboarding {OnboardingId}: {Json}",
                    onboarding.Id, onboarding.StagesProgressJson);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var progress = System.Text.Json.JsonSerializer.Deserialize<List<Domain.Entities.OW.OnboardingStageProgress>>(
                    onboarding.StagesProgressJson, options);

                var validProgress = progress ?? new List<Domain.Entities.OW.OnboardingStageProgress>();

                _logger.LogDebug("Successfully deserialized {Count} progress entries for onboarding {OnboardingId}",
                    validProgress.Count, onboarding.Id);

                // Log details of each progress entry for debugging
                foreach (var item in validProgress)
                {
                    _logger.LogDebug("Progress entry: StageId={StageId}, Status={Status}, IsCompleted={IsCompleted}",
                        item.StageId, item.Status, item.IsCompleted);
                }

                return validProgress;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize stages progress JSON for onboarding {OnboardingId}. JSON: {Json}",
                    onboarding.Id, onboarding.StagesProgressJson);
                return new List<Domain.Entities.OW.OnboardingStageProgress>();
            }
        }

        /// <summary>
        /// Sync progress with current workflow stages
        /// IMPORTANT: This method PRESERVES all completion status and related fields for existing stages.
        /// Only the StageOrder is updated for existing stages to reflect the new ordering.
        /// Completed stages will remain completed after synchronization.
        /// </summary>
        private List<Domain.Entities.OW.OnboardingStageProgress> SyncProgressWithStages(
            List<Domain.Entities.OW.OnboardingStageProgress> currentProgress,
            List<Domain.Entities.OW.Stage> stages)
        {
            var orderedStages = stages.OrderBy(s => s.Order).ToList();

            // Filter out invalid progress entries (StageId = 0 or duplicates) and create dictionary safely
            var invalidEntries = currentProgress.Where(p => p.StageId <= 0).ToList();
            if (invalidEntries.Any())
            {
                _logger.LogWarning("Found {InvalidCount} invalid progress entries with StageId <= 0", invalidEntries.Count);
            }

            var duplicateGroups = currentProgress
                .Where(p => p.StageId > 0)
                .GroupBy(p => p.StageId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateGroups.Any())
            {
                _logger.LogWarning("Found {DuplicateGroupCount} duplicate StageId groups: {StageIds}",
                    duplicateGroups.Count,
                    string.Join(", ", duplicateGroups.Select(g => g.Key)));
            }

            var validProgress = currentProgress
                .Where(p => p.StageId > 0)  // Filter out invalid stage IDs
                .GroupBy(p => p.StageId)    // Group by StageId to handle duplicates
                .ToDictionary(g => g.Key, g => g.First()); // Take first entry for each StageId

            var syncedProgress = new List<Domain.Entities.OW.OnboardingStageProgress>();

            for (int i = 0; i < orderedStages.Count; i++)
            {
                var stage = orderedStages[i];
                var sequentialOrder = i + 1;

                if (validProgress.TryGetValue(stage.Id, out var existingProgress))
                {
                    // Update existing progress - ONLY update StageOrder, preserve all completion status and related fields
                    // This ensures that completed stages remain completed after sync
                    var originalStatus = existingProgress.Status;
                    var originalIsCompleted = existingProgress.IsCompleted;

                    existingProgress.StageOrder = sequentialOrder;
                    syncedProgress.Add(existingProgress);

                    _logger.LogDebug("Preserved existing progress for Stage {StageId}: Status={Status}, IsCompleted={IsCompleted}, Order updated to {NewOrder}",
                        stage.Id, originalStatus, originalIsCompleted, sequentialOrder);
                }
                else
                {
                    // Create new progress for new stage
                    var newProgress = new Domain.Entities.OW.OnboardingStageProgress
                    {
                        StageId = stage.Id,
                        Status = "Pending",
                        IsCompleted = false,
                        StartTime = null,
                        CompletionTime = null,
                        CompletedById = null,
                        CompletedBy = null,
                        Notes = null,
                        IsCurrent = false,
                        StageOrder = sequentialOrder
                    };
                    syncedProgress.Add(newProgress);
                }
            }

            return syncedProgress;
        }

        /// <summary>
        /// Serialize stages progress to JSON
        /// IMPORTANT: This method now preserves AI summary fields to prevent data loss during synchronization
        /// </summary>
        private string SerializeStagesProgress(List<Domain.Entities.OW.OnboardingStageProgress> progress)
        {
            try
            {
                // Serialize core progress fields AND AI summary fields to preserve all onboarding-specific data
                var serializableProgress = progress.Select(p => new
                {
                    p.StageId,
                    p.Status,
                    p.IsCompleted,
                    p.StartTime,
                    p.CompletionTime,
                    p.CompletedById,
                    p.CompletedBy,
                    p.Notes,
                    p.IsCurrent,
                    // Preserve AI Summary fields to prevent data loss during sync
                    p.AiSummary,
                    p.AiSummaryGeneratedAt,
                    p.AiSummaryConfidence,
                    p.AiSummaryModel,
                    p.AiSummaryData
                }).ToList();

                return System.Text.Json.JsonSerializer.Serialize(serializableProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize stages progress");
                return "[]";
            }
        }

        /// <summary>
        /// Safely update onboarding entity with JSONB compatibility
        /// This method handles the JSONB type conversion issue for stages_progress_json
        /// </summary>
        private async Task SafeUpdateOnboardingWithJsonbAsync(Domain.Entities.OW.Onboarding onboarding, string stagesProgressJson)
        {
            try
            {
                // Get SqlSugar client for direct SQL execution
                var db = _onboardingRepository.GetSqlSugarClient();

                // Update stages_progress_json using explicit JSONB casting
                if (!string.IsNullOrEmpty(stagesProgressJson))
                {
                    try
                    {
                        var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                        await db.Ado.ExecuteCommandAsync(progressSql, new
                        {
                            StagesProgressJson = stagesProgressJson,
                            Id = onboarding.Id
                        });

                        _logger.LogDebug("Successfully updated stages_progress_json with JSONB casting for onboarding {OnboardingId}", onboarding.Id);
                    }
                    catch (Exception progressEx)
                    {
                        // Log but don't fail the main update
                        _logger.LogWarning(progressEx, "Warning: Failed to update stages_progress_json for onboarding {OnboardingId}", onboarding.Id);

                        // Try alternative approach with parameter substitution
                        try
                        {
                            var escapedJson = stagesProgressJson.Replace("'", "''");
                            var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {onboarding.Id}";
                            await db.Ado.ExecuteCommandAsync(directSql);

                            _logger.LogDebug("Successfully updated stages_progress_json with direct SQL for onboarding {OnboardingId}", onboarding.Id);
                        }
                        catch (Exception directEx)
                        {
                            _logger.LogError(directEx, "Error: Both parameterized and direct JSONB update failed for onboarding {OnboardingId}", onboarding.Id);
                            throw;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Stages progress JSON is empty for onboarding {OnboardingId}", onboarding.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to safely update onboarding with JSONB for onboarding {OnboardingId}", onboarding.Id);
                throw;
            }
        }

        /// <summary>
        /// Debug method to check onboarding stages progress data
        /// </summary>
        public async Task<string> DebugOnboardingStagesProgressAsync(long onboardingId)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    return $"Onboarding {onboardingId} not found";
                }

                var result = $"Onboarding {onboardingId} Debug Info:\n";
                result += $"StagesProgressJson: {onboarding.StagesProgressJson}\n";
                result += $"StagesProgressJson IsNull: {string.IsNullOrEmpty(onboarding.StagesProgressJson)}\n";

                var currentProgress = LoadStagesProgressFromJson(onboarding);
                result += $"Loaded Progress Count: {currentProgress.Count}\n";

                for (int i = 0; i < currentProgress.Count; i++)
                {
                    var p = currentProgress[i];
                    result += $"Progress[{i}]: StageId={p.StageId}, Status={p.Status}, IsCompleted={p.IsCompleted}, " +
                             $"StartTime={p.StartTime}, CompletionTime={p.CompletionTime}, CompletedBy={p.CompletedBy}\n";
                }

                var invalidEntries = currentProgress.Where(p => p.StageId <= 0).ToList();
                result += $"Invalid Entries Count: {invalidEntries.Count}\n";

                return result;
            }
            catch (Exception ex)
            {
                return $"Error debugging onboarding {onboardingId}: {ex.Message}";
            }
        }

        #endregion
    }
}