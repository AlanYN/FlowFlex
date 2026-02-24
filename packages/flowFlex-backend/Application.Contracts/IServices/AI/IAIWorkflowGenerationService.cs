using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI workflow generation service interface.
    /// Responsible for workflow generation (sync + streaming), enhancement, validation,
    /// and stage component creation.
    /// </summary>
    public interface IAIWorkflowGenerationService : IScopedService
    {
        /// <summary>
        /// Generate workflow from natural language description
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Generated workflow structure</returns>
        Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input);

        /// <summary>
        /// Stream generate workflow with real-time updates
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Streaming workflow generation</returns>
        IAsyncEnumerable<AIWorkflowStreamResult> StreamGenerateWorkflowAsync(AIWorkflowGenerationInput input);

        /// <summary>
        /// Enhance existing workflow with AI suggestions
        /// </summary>
        /// <param name="workflowId">Existing workflow ID</param>
        /// <param name="enhancement">Enhancement request</param>
        /// <returns>Enhanced workflow suggestions</returns>
        Task<AIWorkflowEnhancementResult> EnhanceWorkflowAsync(long workflowId, string enhancement);

        /// <summary>
        /// Enhance existing workflow using modification input
        /// </summary>
        /// <param name="input">Modification input</param>
        /// <returns>Enhanced workflow result</returns>
        Task<AIWorkflowGenerationResult> EnhanceWorkflowAsync(AIWorkflowModificationInput input);

        /// <summary>
        /// Validate and suggest improvements for workflow
        /// </summary>
        /// <param name="workflow">Workflow to validate</param>
        /// <returns>Validation results and suggestions</returns>
        Task<AIValidationResult> ValidateWorkflowAsync(WorkflowInputDto workflow);

        /// <summary>
        /// Create actual checklist and questionnaire records and associate them with stages
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="stages">Generated stages</param>
        /// <param name="checklists">Generated checklists</param>
        /// <param name="questionnaires">Generated questionnaires</param>
        /// <returns>Success status</returns>
        Task<bool> CreateStageComponentsAsync(long workflowId, List<AIStageGenerationResult> stages,
            List<AIChecklistGenerationResult> checklists, List<AIQuestionnaireGenerationResult> questionnaires);
    }
}
