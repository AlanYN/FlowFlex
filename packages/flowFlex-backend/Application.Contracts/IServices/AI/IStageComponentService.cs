using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// Stage component creation service interface.
    /// Responsible for creating Checklists and Questionnaires from AI generation results
    /// and associating them with workflow stages.
    /// </summary>
    public interface IStageComponentService : IScopedService
    {
        /// <summary>
        /// Create stage components (Checklists and Questionnaires) from AI generation results
        /// and associate them with the corresponding workflow stages.
        /// </summary>
        /// <param name="workflowId">The workflow ID to create components for</param>
        /// <param name="stages">AI-generated stage results</param>
        /// <param name="checklists">AI-generated checklist results</param>
        /// <param name="questionnaires">AI-generated questionnaire results</param>
        /// <returns>True if components were created successfully</returns>
        Task<bool> CreateStageComponentsAsync(long workflowId,
            List<AIStageGenerationResult> stages,
            List<AIChecklistGenerationResult> checklists,
            List<AIQuestionnaireGenerationResult> questionnaires);
    }
}
