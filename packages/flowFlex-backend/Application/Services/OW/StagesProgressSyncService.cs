using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Application.Contracts.Dtos.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private readonly StagesProgressSyncOptions _options;

        public StagesProgressSyncService(
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            ILogger<StagesProgressSyncService> logger,
            IOptions<StagesProgressSyncOptions> options)
        {
            _onboardingRepository = onboardingRepository;
            _stageRepository = stageRepository;
            _logger = logger;
            _options = options?.Value ?? new StagesProgressSyncOptions();
        }

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage update
        /// </summary>
        public async Task<int> SyncAfterStageUpdateAsync(long workflowId, long? updatedStageId = null)
        {
            // EMERGENCY SAFETY CHECK: Respect configuration options
            if (_options.EmergencyMode)
            {
                _logger.LogWarning("EMERGENCY MODE ACTIVE: Skipping stages progress sync for workflow {WorkflowId}", workflowId);
                return 0;
            }

            if (!_options.EnableSync || !_options.EnableSyncAfterStageUpdate)
            {
                _logger.LogInformation("Stages progress sync is disabled via configuration for workflow {WorkflowId}", workflowId);
                return 0;
            }

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

                // Respect batch size limits
                if (onboardings.Count > _options.MaxBatchSize)
                {
                    _logger.LogWarning("Onboarding count {Count} exceeds max batch size {MaxBatch} for workflow {WorkflowId}. " +
                        "Processing first {MaxBatch} onboardings only.", 
                        onboardings.Count, _options.MaxBatchSize, workflowId, _options.MaxBatchSize);
                    onboardings = onboardings.Take(_options.MaxBatchSize).ToList();
                }

                var syncedCount = 0;
                var failedCount = 0;

                foreach (var onboarding in onboardings)
                {
                    try
                    {
                        await SyncSingleOnboardingInternalAsync(onboarding);
                        syncedCount++;

                        if (_options.EnableDetailedLogging)
                        {
                            _logger.LogDebug("Successfully synced stages progress for onboarding {OnboardingId}",
                                onboarding.Id);
                        }
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
        /// IMPROVED: Added comprehensive data validation and integrity checks
        /// </summary>
        private async Task SyncSingleOnboardingInternalAsync(Domain.Entities.OW.Onboarding onboarding)
        {
            try
            {
                _logger.LogInformation("Starting stages progress sync for onboarding {OnboardingId} in workflow {WorkflowId}", 
                    onboarding.Id, onboarding.WorkflowId);

                // Get current workflow stages
                var stages = await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    _logger.LogWarning("No stages found for workflow {WorkflowId} - skipping sync for onboarding {OnboardingId}", 
                        onboarding.WorkflowId, onboarding.Id);
                    return;
                }

                // Load current stages progress from JSON
                var currentProgress = LoadStagesProgressFromJson(onboarding);

                _logger.LogDebug("Loaded {ProgressCount} stages progress entries for onboarding {OnboardingId}",
                    currentProgress.Count, onboarding.Id);

                // CRITICAL VALIDATION: Check if we have existing completed stages
                var completedStages = currentProgress.Where(p => p.IsCompleted).ToList();
                if (completedStages.Any())
                {
                    _logger.LogInformation("Found {CompletedCount} completed stages for onboarding {OnboardingId}: {CompletedStageIds}",
                        completedStages.Count, onboarding.Id, 
                        string.Join(", ", completedStages.Select(s => s.StageId)));
                }

                // Sync with current workflow stages
                var updatedProgress = SyncProgressWithStages(currentProgress, stages);

                _logger.LogDebug("Synced to {UpdatedCount} stages progress entries for onboarding {OnboardingId}",
                    updatedProgress.Count, onboarding.Id);

                // INTEGRITY CHECK: Verify that completed stages are preserved
                var preservedCompletedStages = updatedProgress.Where(p => p.IsCompleted).ToList();
                if (completedStages.Count != preservedCompletedStages.Count)
                {
                    _logger.LogError("CRITICAL: Completed stages count mismatch after sync for onboarding {OnboardingId}. Before: {BeforeCount}, After: {AfterCount}",
                        onboarding.Id, completedStages.Count, preservedCompletedStages.Count);
                    
                    // Log detailed comparison
                    var beforeIds = completedStages.Select(s => s.StageId).OrderBy(x => x).ToList();
                    var afterIds = preservedCompletedStages.Select(s => s.StageId).OrderBy(x => x).ToList();
                    _logger.LogError("Before completed stage IDs: [{BeforeIds}], After completed stage IDs: [{AfterIds}]",
                        string.Join(", ", beforeIds), string.Join(", ", afterIds));
                    
                    throw new InvalidOperationException($"Data integrity violation: Completed stages were lost during sync for onboarding {onboarding.Id}");
                }

                // VALIDATION: Ensure we're not creating empty or invalid progress
                if (!updatedProgress.Any())
                {
                    _logger.LogError("CRITICAL: Sync resulted in empty stages progress for onboarding {OnboardingId}. This is not allowed.", onboarding.Id);
                    throw new InvalidOperationException($"Sync cannot result in empty stages progress for onboarding {onboarding.Id}");
                }

                // Update the onboarding entity
                onboarding.StagesProgress = updatedProgress;
                var jsonData = SerializeStagesProgress(updatedProgress);

                // FINAL VALIDATION: Ensure JSON is not empty
                if (string.IsNullOrEmpty(jsonData) || jsonData == "[]")
                {
                    _logger.LogError("CRITICAL: Serialized stages progress is empty for onboarding {OnboardingId}. Blocking update to prevent data loss.", onboarding.Id);
                    throw new InvalidOperationException($"Cannot save empty stages progress for onboarding {onboarding.Id}");
                }

                // Save to database using JSONB-safe approach
                await SafeUpdateOnboardingWithJsonbAsync(onboarding, jsonData);

                _logger.LogInformation("Successfully synced stages progress for onboarding {OnboardingId}. Final count: {FinalCount}, Completed: {CompletedCount}",
                    onboarding.Id, updatedProgress.Count, preservedCompletedStages.Count);
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
                    // IMPORTANT: Preserve custom fields to prevent data loss during sync
                    p.CustomEstimatedDays,
                    p.CustomEndTime,
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
        /// IMPROVED: Now includes proper transaction handling and data integrity validation
        /// </summary>
        private async Task SafeUpdateOnboardingWithJsonbAsync(Domain.Entities.OW.Onboarding onboarding, string stagesProgressJson)
        {
            try
            {
                // Get SqlSugar client for direct SQL execution
                var db = _onboardingRepository.GetSqlSugarClient();

                // CRITICAL FIX: Validate that we're not clearing existing data unintentionally
                if (string.IsNullOrEmpty(stagesProgressJson))
                {
                    _logger.LogWarning("CRITICAL: Attempting to set empty stages progress JSON for onboarding {OnboardingId}. This operation is blocked to prevent data loss.", onboarding.Id);
                    
                    // Respect configuration for data clearing prevention
                    if (_options.PreventDataClearing)
                    {
                        // Check if onboarding currently has stages progress data
                        var currentOnboarding = await _onboardingRepository.GetByIdAsync(onboarding.Id);
                        if (currentOnboarding != null && !string.IsNullOrEmpty(currentOnboarding.StagesProgressJson))
                        {
                            _logger.LogError("BLOCKED: Prevented clearing of existing stages progress data for onboarding {OnboardingId}. Current data: {CurrentData}", 
                                onboarding.Id, currentOnboarding.StagesProgressJson);
                            return; // Do not proceed with empty update
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Data clearing prevention is disabled - allowing empty update for onboarding {OnboardingId}", onboarding.Id);
                    }
                }

                // Use transaction to ensure data consistency
                await db.Ado.UseTranAsync(async () =>
                {
                    // Update stages_progress_json using explicit JSONB casting with audit fields
                    if (!string.IsNullOrEmpty(stagesProgressJson))
                    {
                        try
                        {
                            // IMPROVED: Include audit fields in the update to maintain data integrity
                            var progressSql = @"UPDATE ff_onboarding 
                                              SET stages_progress_json = @StagesProgressJson::jsonb,
                                                  modify_date = @ModifyDate,
                                                  modify_by = @ModifyBy
                                              WHERE id = @Id";
                            
                            await db.Ado.ExecuteCommandAsync(progressSql, new
                            {
                                StagesProgressJson = stagesProgressJson,
                                Id = onboarding.Id,
                                ModifyDate = DateTimeOffset.UtcNow,
                                ModifyBy = "StagesProgressSyncService"
                            });

                            _logger.LogDebug("Successfully updated stages_progress_json with JSONB casting for onboarding {OnboardingId}", onboarding.Id);
                        }
                        catch (Exception progressEx)
                        {
                            _logger.LogError(progressEx, "CRITICAL: Failed to update stages_progress_json for onboarding {OnboardingId}. Rolling back transaction.", onboarding.Id);
                            throw; // This will cause transaction rollback
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Skipping empty stages progress JSON update for onboarding {OnboardingId} to prevent data loss", onboarding.Id);
                    }
                });

                _logger.LogDebug("Transaction completed successfully for onboarding {OnboardingId}", onboarding.Id);
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

        #region Data Validation and Recovery Methods

        /// <summary>
        /// Validate and repair stages progress data for a specific onboarding
        /// </summary>
        public async Task<StagesProgressValidationResult> ValidateAndRepairOnboardingAsync(long onboardingId, bool autoRepair = true)
        {
            var result = new StagesProgressValidationResult
            {
                OnboardingId = onboardingId,
                RepairAttempted = autoRepair
            };

            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Onboarding not found";
                    return result;
                }

                // Load current stages progress
                var currentProgress = LoadStagesProgressFromJson(onboarding);
                result.StagesCountBefore = currentProgress.Count;
                result.CompletedStagesCountBefore = currentProgress.Count(p => p.IsCompleted);

                // Get workflow stages for validation
                var workflowStages = await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId);
                if (workflowStages == null || !workflowStages.Any())
                {
                    result.Issues.Add("No stages found in workflow");
                    result.IsValid = false;
                    return result;
                }

                // Validate stages progress
                var validationIssues = ValidateStagesProgress(currentProgress, workflowStages);
                result.Issues.AddRange(validationIssues);
                result.IsValid = !validationIssues.Any();

                // Attempt repair if needed and requested
                if (!result.IsValid && autoRepair && _options.EnableDataIntegrityValidation)
                {
                    try
                    {
                        _logger.LogInformation("Attempting to repair stages progress for onboarding {OnboardingId}", onboardingId);
                        
                        var repairedProgress = SyncProgressWithStages(currentProgress, workflowStages);
                        var repairedJson = SerializeStagesProgress(repairedProgress);

                        // Update the onboarding with repaired data
                        await SafeUpdateOnboardingWithJsonbAsync(onboarding, repairedJson);

                        result.RepairSuccessful = true;
                        result.StagesCountAfter = repairedProgress.Count;
                        result.CompletedStagesCountAfter = repairedProgress.Count(p => p.IsCompleted);

                        _logger.LogInformation("Successfully repaired stages progress for onboarding {OnboardingId}", onboardingId);
                    }
                    catch (Exception repairEx)
                    {
                        result.RepairSuccessful = false;
                        result.ErrorMessage = $"Repair failed: {repairEx.Message}";
                        _logger.LogError(repairEx, "Failed to repair stages progress for onboarding {OnboardingId}", onboardingId);
                    }
                }
                else
                {
                    result.StagesCountAfter = result.StagesCountBefore;
                    result.CompletedStagesCountAfter = result.CompletedStagesCountBefore;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error validating stages progress for onboarding {OnboardingId}", onboardingId);
                return result;
            }
        }

        /// <summary>
        /// Batch validate and repair stages progress data for multiple onboardings
        /// </summary>
        public async Task<List<StagesProgressValidationResult>> BatchValidateAndRepairAsync(List<long> onboardingIds, bool autoRepair = true)
        {
            var results = new List<StagesProgressValidationResult>();

            if (onboardingIds == null || !onboardingIds.Any())
            {
                return results;
            }

            // Respect batch size limits
            var batchSize = Math.Min(onboardingIds.Count, _options.MaxBatchSize);
            var onboardingIdsToProcess = onboardingIds.Take(batchSize).ToList();

            if (onboardingIds.Count > batchSize)
            {
                _logger.LogWarning("Batch size {RequestedCount} exceeds limit {MaxBatch}. Processing first {ProcessedCount} onboardings.",
                    onboardingIds.Count, _options.MaxBatchSize, batchSize);
            }

            foreach (var onboardingId in onboardingIdsToProcess)
            {
                try
                {
                    var result = await ValidateAndRepairOnboardingAsync(onboardingId, autoRepair);
                    results.Add(result);

                    // Small delay to avoid overwhelming the database
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    results.Add(new StagesProgressValidationResult
                    {
                        OnboardingId = onboardingId,
                        IsValid = false,
                        ErrorMessage = ex.Message
                    });
                    _logger.LogError(ex, "Error in batch validation for onboarding {OnboardingId}", onboardingId);
                }
            }

            // Log summary
            var validCount = results.Count(r => r.IsValid);
            var repairedCount = results.Count(r => r.RepairSuccessful == true);
            _logger.LogInformation("Batch validation completed: {Total} processed, {Valid} valid, {Repaired} repaired",
                results.Count, validCount, repairedCount);

            return results;
        }

        /// <summary>
        /// Emergency recovery method to restore stages progress from workflow stages
        /// </summary>
        public async Task<StagesProgressRecoveryResult> EmergencyRecoverStagesProgressAsync(long onboardingId, bool preserveCompletedStages = true)
        {
            var result = new StagesProgressRecoveryResult
            {
                OnboardingId = onboardingId,
                CompletedStagesPreserved = preserveCompletedStages
            };

            try
            {
                _logger.LogWarning("Starting emergency recovery for onboarding {OnboardingId}", onboardingId);

                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    result.ErrorMessage = "Onboarding not found";
                    return result;
                }

                // Store original data for reference
                result.OriginalStagesProgressJson = onboarding.StagesProgressJson;

                // Get workflow stages
                var workflowStages = await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId);
                if (workflowStages == null || !workflowStages.Any())
                {
                    result.ErrorMessage = "No stages found in workflow";
                    return result;
                }

                // Try to preserve completed stage information if requested
                var completedStageIds = new HashSet<long>();
                if (preserveCompletedStages && !string.IsNullOrEmpty(onboarding.StagesProgressJson))
                {
                    try
                    {
                        var existingProgress = LoadStagesProgressFromJson(onboarding);
                        completedStageIds = existingProgress
                            .Where(p => p.IsCompleted)
                            .Select(p => p.StageId)
                            .ToHashSet();
                        
                        _logger.LogInformation("Found {CompletedCount} completed stages to preserve for onboarding {OnboardingId}: {StageIds}",
                            completedStageIds.Count, onboardingId, string.Join(", ", completedStageIds));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not parse existing stages progress for preservation in onboarding {OnboardingId}", onboardingId);
                    }
                }

                // Create fresh stages progress from workflow stages
                var recoveredProgress = new List<Domain.Entities.OW.OnboardingStageProgress>();
                var orderedStages = workflowStages.OrderBy(s => s.Order).ToList();

                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var isCompleted = completedStageIds.Contains(stage.Id);

                    var stageProgress = new Domain.Entities.OW.OnboardingStageProgress
                    {
                        StageId = stage.Id,
                        Status = isCompleted ? "Completed" : "Pending",
                        IsCompleted = isCompleted,
                        StartTime = isCompleted ? DateTime.UtcNow.AddDays(-1) : null,
                        CompletionTime = isCompleted ? DateTime.UtcNow : null,
                        CompletedById = isCompleted ? (long?)999999999 : null, // Special ID for recovery system
                        CompletedBy = isCompleted ? "Emergency Recovery System" : null,
                        Notes = isCompleted ? "Stage completion preserved during emergency recovery" : null,
                        IsCurrent = false,
                        StageOrder = i + 1
                    };

                    recoveredProgress.Add(stageProgress);
                }

                // Serialize and save recovered data
                var recoveredJson = SerializeStagesProgress(recoveredProgress);
                result.RecoveredStagesProgressJson = recoveredJson;

                // Update the onboarding with recovered data
                await SafeUpdateOnboardingWithJsonbAsync(onboarding, recoveredJson);

                result.RecoverySuccessful = true;
                result.StagesRecovered = recoveredProgress.Count;
                result.CompletedStagesPreservedCount = completedStageIds.Count;

                _logger.LogInformation("Emergency recovery completed for onboarding {OnboardingId}. Recovered {StageCount} stages, preserved {CompletedCount} completed stages",
                    onboardingId, result.StagesRecovered, result.CompletedStagesPreservedCount);

                return result;
            }
            catch (Exception ex)
            {
                result.RecoverySuccessful = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Emergency recovery failed for onboarding {OnboardingId}", onboardingId);
                return result;
            }
        }

        /// <summary>
        /// Validate stages progress against workflow stages
        /// </summary>
        private List<string> ValidateStagesProgress(List<Domain.Entities.OW.OnboardingStageProgress> progress, List<Domain.Entities.OW.Stage> workflowStages)
        {
            var issues = new List<string>();

            try
            {
                // Check for empty progress
                if (!progress.Any())
                {
                    issues.Add("Stages progress is empty");
                    return issues;
                }

                // Check for invalid stage IDs
                var invalidStages = progress.Where(p => p.StageId <= 0).ToList();
                if (invalidStages.Any())
                {
                    issues.Add($"Found {invalidStages.Count} progress entries with invalid stage IDs");
                }

                // Check for duplicate stage IDs
                var duplicateStageIds = progress
                    .GroupBy(p => p.StageId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateStageIds.Any())
                {
                    issues.Add($"Found duplicate stage IDs: {string.Join(", ", duplicateStageIds)}");
                }

                // Check for missing stages
                var workflowStageIds = workflowStages.Select(s => s.Id).ToHashSet();
                var progressStageIds = progress.Where(p => p.StageId > 0).Select(p => p.StageId).ToHashSet();

                var missingStages = workflowStageIds.Except(progressStageIds).ToList();
                if (missingStages.Any())
                {
                    issues.Add($"Missing stages in progress: {string.Join(", ", missingStages)}");
                }

                // Check for orphaned stages (stages in progress but not in workflow)
                var orphanedStages = progressStageIds.Except(workflowStageIds).ToList();
                if (orphanedStages.Any())
                {
                    issues.Add($"Orphaned stages in progress (not in workflow): {string.Join(", ", orphanedStages)}");
                }

                return issues;
            }
            catch (Exception ex)
            {
                issues.Add($"Validation error: {ex.Message}");
                return issues;
            }
        }

        #endregion
    }
}