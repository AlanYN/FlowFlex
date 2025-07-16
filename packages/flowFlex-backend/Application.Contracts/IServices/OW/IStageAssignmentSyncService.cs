using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stage and Checklist/Questionnaire assignments bidirectional sync service interface
    /// </summary>
    public interface IStageAssignmentSyncService : IScopedService
    {
        /// <summary>
        /// Sync assignments when stage components change
        /// Called when checklistIds or questionnaireIds in stage components are updated
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="oldChecklistIds">Previous checklist IDs</param>
        /// <param name="newChecklistIds">New checklist IDs</param>
        /// <param name="oldQuestionnaireIds">Previous questionnaire IDs</param>
        /// <param name="newQuestionnaireIds">New questionnaire IDs</param>
        /// <returns>Success status</returns>
        Task<bool> SyncAssignmentsFromStageComponentsAsync(
            long stageId, 
            long workflowId,
            List<long> oldChecklistIds, 
            List<long> newChecklistIds,
            List<long> oldQuestionnaireIds, 
            List<long> newQuestionnaireIds);

        /// <summary>
        /// Sync stage components when checklist assignments change
        /// Called when assignments of a checklist are updated
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="oldAssignments">Previous assignments</param>
        /// <param name="newAssignments">New assignments</param>
        /// <returns>Success status</returns>
        Task<bool> SyncStageComponentsFromChecklistAssignmentsAsync(
            long checklistId,
            List<(long workflowId, long stageId)> oldAssignments,
            List<(long workflowId, long stageId)> newAssignments);

        /// <summary>
        /// Sync stage components when questionnaire assignments change
        /// Called when assignments of a questionnaire are updated
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="oldAssignments">Previous assignments</param>
        /// <param name="newAssignments">New assignments</param>
        /// <returns>Success status</returns>
        Task<bool> SyncStageComponentsFromQuestionnaireAssignmentsAsync(
            long questionnaireId,
            List<(long workflowId, long stageId)> oldAssignments,
            List<(long workflowId, long stageId)> newAssignments);

        /// <summary>
        /// Force sync all relationships (maintenance operation)
        /// </summary>
        /// <returns>Success status</returns>
        Task<bool> ForceSyncAllRelationshipsAsync();
    }
} 