using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stage component name synchronization service interface
    /// </summary>
    public interface IStageComponentNameSyncService : IScopedService
    {
        /// <summary>
        /// Sync checklist name changes to all related stages
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <param name="newName">New checklist name</param>
        /// <returns>Number of stages updated</returns>
        Task<int> SyncChecklistNameChangeAsync(long checklistId, string newName);

        /// <summary>
        /// Sync questionnaire name changes to all related stages
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <param name="newName">New questionnaire name</param>
        /// <returns>Number of stages updated</returns>
        Task<int> SyncQuestionnaireNameChangeAsync(long questionnaireId, string newName);

        /// <summary>
        /// Batch sync multiple checklist name changes
        /// </summary>
        /// <param name="nameChanges">Dictionary of checklist ID to new name mappings</param>
        /// <returns>Number of stages updated</returns>
        Task<int> BatchSyncChecklistNameChangesAsync(Dictionary<long, string> nameChanges);

        /// <summary>
        /// Batch sync multiple questionnaire name changes
        /// </summary>
        /// <param name="nameChanges">Dictionary of questionnaire ID to new name mappings</param>
        /// <returns>Number of stages updated</returns>
        Task<int> BatchSyncQuestionnaireNameChangesAsync(Dictionary<long, string> nameChanges);

        /// <summary>
        /// Get all stages that use a specific checklist
        /// </summary>
        /// <param name="checklistId">Checklist ID</param>
        /// <returns>List of stage IDs that use this checklist</returns>
        Task<List<long>> GetStagesUsingChecklistAsync(long checklistId);

        /// <summary>
        /// Get all stages that use a specific questionnaire
        /// </summary>
        /// <param name="questionnaireId">Questionnaire ID</param>
        /// <returns>List of stage IDs that use this questionnaire</returns>
        Task<List<long>> GetStagesUsingQuestionnaireAsync(long questionnaireId);

        /// <summary>
        /// Force refresh all component names in a specific stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Whether the refresh was successful</returns>
        Task<bool> RefreshStageComponentNamesAsync(long stageId);

        /// <summary>
        /// Validate and fix any missing or incorrect component names across all stages
        /// </summary>
        /// <returns>Number of stages fixed</returns>
        Task<int> ValidateAndFixAllStageComponentNamesAsync();
    }
}