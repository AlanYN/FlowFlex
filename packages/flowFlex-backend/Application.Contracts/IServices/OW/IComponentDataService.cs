using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Component data retrieval service interface
    /// </summary>
    public interface IComponentDataService : IScopedService
    {
        /// <summary>
        /// Get checklist data for a stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Checklist data</returns>
        Task<ChecklistData> GetChecklistDataAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get questionnaire data for a stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Questionnaire data</returns>
        Task<QuestionnaireData> GetQuestionnaireDataAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get attachment data for a stage
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Attachment data</returns>
        Task<AttachmentData> GetAttachmentDataAsync(long onboardingId, long stageId);

        /// <summary>
        /// Get fields data from onboarding DynamicData
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Fields data dictionary</returns>
        Task<Dictionary<string, object>> GetFieldsDataAsync(long onboardingId);

        /// <summary>
        /// Get available components for a stage (for condition configuration UI)
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>List of available components</returns>
        Task<List<AvailableComponent>> GetAvailableComponentsAsync(long stageId);

        /// <summary>
        /// Get available fields for a component
        /// </summary>
        /// <param name="componentId">Component ID</param>
        /// <param name="componentType">Component Type</param>
        /// <returns>List of available fields</returns>
        Task<List<AvailableField>> GetAvailableFieldsAsync(long componentId, string componentType);
    }
}
