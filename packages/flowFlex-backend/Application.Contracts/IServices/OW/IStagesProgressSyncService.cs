using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stages Progress Sync Service Interface
    /// Handles synchronization of onboarding stages progress when workflow stages are modified
    /// </summary>
    public interface IStagesProgressSyncService : IScopedService
    {
        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage update
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="updatedStageId">Updated stage ID (optional, for targeted sync)</param>
        /// <returns>Number of onboarding records synced</returns>
        Task<int> SyncAfterStageUpdateAsync(long workflowId, long? updatedStageId = null);

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage deletion
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="deletedStageId">Deleted stage ID</param>
        /// <returns>Number of onboarding records synced</returns>
        Task<int> SyncAfterStageDeleteAsync(long workflowId, long deletedStageId);

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage sorting
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="stageIds">List of stage IDs that were reordered</param>
        /// <returns>Number of onboarding records synced</returns>
        Task<int> SyncAfterStagesSortAsync(long workflowId, List<long> stageIds);

        /// <summary>
        /// Validate and repair stages progress data for a specific onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID to validate and repair</param>
        /// <param name="autoRepair">Whether to automatically repair detected issues</param>
        /// <returns>Validation and repair result</returns>
        Task<StagesProgressValidationResult> ValidateAndRepairOnboardingAsync(long onboardingId, bool autoRepair = true);

        /// <summary>
        /// Batch validate and repair stages progress data for multiple onboardings
        /// </summary>
        /// <param name="onboardingIds">List of onboarding IDs to validate</param>
        /// <param name="autoRepair">Whether to automatically repair detected issues</param>
        /// <returns>List of validation results</returns>
        Task<List<StagesProgressValidationResult>> BatchValidateAndRepairAsync(List<long> onboardingIds, bool autoRepair = true);

        /// <summary>
        /// Emergency recovery method to restore stages progress from workflow stages
        /// Use this method when stages progress data is corrupted or lost
        /// </summary>
        /// <param name="onboardingId">Onboarding ID to recover</param>
        /// <param name="preserveCompletedStages">Whether to try to preserve completed stage information</param>
        /// <returns>Recovery result</returns>
        Task<StagesProgressRecoveryResult> EmergencyRecoverStagesProgressAsync(long onboardingId, bool preserveCompletedStages = true);

        /// <summary>
        /// Sync stages progress for all onboardings in a specific workflow after stage combination
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="deletedStageIds">IDs of stages that were combined and deleted</param>
        /// <param name="newStageId">ID of the new combined stage</param>
        /// <returns>Number of onboarding records synced</returns>
        Task<int> SyncAfterStagesCombineAsync(long workflowId, List<long> deletedStageIds, long newStageId);

        /// <summary>
        /// Sync stages progress for a specific onboarding
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Success status</returns>
        Task<bool> SyncSingleOnboardingAsync(long onboardingId);

        /// <summary>
        /// Batch sync stages progress for multiple onboardings
        /// </summary>
        /// <param name="onboardingIds">List of onboarding IDs</param>
        /// <returns>Number of successfully synced records</returns>
        Task<int> BatchSyncOnboardingsAsync(List<long> onboardingIds);
    }
}