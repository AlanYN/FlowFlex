using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Gantt;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Gantt chart service — data query and blocker management
    /// </summary>
    public interface IGanttService : IScopedService
    {
        /// <summary>
        /// Returns the full Gantt chart data for the specified case,
        /// including case-level summary and all stage items with derived status.
        /// Applies view-permission check; throws 403 if the caller lacks access.
        /// </summary>
        /// <param name="onboardingId">ID of the onboarding / case</param>
        /// <returns>GanttDataResponseDto containing Summary and Stages</returns>
        Task<GanttDataResponseDto> GetGanttDataAsync(long onboardingId);

        /// <summary>
        /// Marks the specified stage as blocked and appends a new blocker record
        /// to its BlockerHistory. Triggers Projected-time recalculation for all
        /// subsequent stages (sets them to null).
        /// Applies operate-permission check; throws 403 if the caller lacks access.
        /// Returns 400 if the stage is already blocked.
        /// </summary>
        /// <param name="onboardingId">ID of the onboarding / case</param>
        /// <param name="input">Block input containing StageId, BlockerReason and optional ExpectedResolutionDate</param>
        Task<bool> BlockStageAsync(long onboardingId, BlockStageInputDto input);

        /// <summary>
        /// Clears the blocked status of the specified stage, fills in the resolution
        /// fields on the latest BlockerRecord, and triggers Projected-time recalculation.
        /// Applies operate-permission check; throws 403 if the caller lacks access.
        /// Returns 400 if the stage is not currently blocked.
        /// </summary>
        /// <param name="onboardingId">ID of the onboarding / case</param>
        /// <param name="input">Unblock input containing StageId and optional ResolutionNotes</param>
        Task<bool> UnblockStageAsync(long onboardingId, UnblockStageInputDto input);
    }
}
