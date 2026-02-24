using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// Unified prompt builder interface for constructing AI prompts
    /// Centralizes all prompt building logic from the various AIService partial classes
    /// </summary>
    public interface IAIPromptBuilder : IScopedService
    {
        #region Workflow Prompts

        /// <summary>
        /// Build prompt for workflow generation from natural language input
        /// </summary>
        /// <param name="input">Workflow generation input containing description, context, requirements, etc.</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildWorkflowGenerationPrompt(AIWorkflowGenerationInput input);

        /// <summary>
        /// Build prompt for workflow modification based on existing workflow information
        /// </summary>
        /// <param name="input">Workflow modification input</param>
        /// <param name="existingWorkflowInfo">Existing workflow information for context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        Task<string> BuildWorkflowModificationPromptAsync(AIWorkflowModificationInput input, WorkflowInfo existingWorkflowInfo);

        #endregion

        #region Questionnaire Prompts

        /// <summary>
        /// Build prompt for questionnaire generation from input parameters
        /// </summary>
        /// <param name="input">Questionnaire generation input</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildQuestionnaireGenerationPrompt(AIQuestionnaireGenerationInput input);

        /// <summary>
        /// Build prompt for questionnaire generation based on a specific stage
        /// </summary>
        /// <param name="stage">Stage generation result containing stage details</param>
        /// <param name="originalDescription">Original project description for context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildQuestionnaireGenerationPrompt(AIStageGenerationResult stage, string originalDescription);

        /// <summary>
        /// Build prompt for batch questionnaire generation across multiple stages
        /// </summary>
        /// <param name="stages">List of stage generation results</param>
        /// <param name="originalDescription">Original project description for context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildBatchQuestionnaireGenerationPrompt(List<AIStageGenerationResult> stages, string originalDescription);

        #endregion

        #region Checklist Prompts

        /// <summary>
        /// Build prompt for checklist generation from input parameters
        /// </summary>
        /// <param name="input">Checklist generation input</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildChecklistGenerationPrompt(AIChecklistGenerationInput input);

        /// <summary>
        /// Build prompt for checklist generation based on a specific stage
        /// </summary>
        /// <param name="stage">Stage generation result containing stage details</param>
        /// <param name="originalDescription">Original project description for context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildChecklistGenerationPrompt(AIStageGenerationResult stage, string originalDescription);

        /// <summary>
        /// Build prompt for batch checklist generation across multiple stages
        /// </summary>
        /// <param name="stages">List of stage generation results</param>
        /// <param name="originalDescription">Original project description for context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildBatchChecklistGenerationPrompt(List<AIStageGenerationResult> stages, string originalDescription);

        #endregion

        #region Chat Prompts

        /// <summary>
        /// Build prompt for chat conversation based on input messages and context
        /// </summary>
        /// <param name="input">Chat input containing messages, mode, and context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildChatPrompt(AIChatInput input);

        /// <summary>
        /// Get system prompt for chat based on mode
        /// </summary>
        /// <param name="mode">Chat mode (e.g., "workflow_planning", "generate_code")</param>
        /// <param name="input">Additional input for mode-specific prompts</param>
        /// <returns>System prompt string</returns>
        string GetChatSystemPrompt(string mode, string input);

        /// <summary>
        /// Get prompt for code generation
        /// </summary>
        /// <param name="instruction">Code generation instruction</param>
        /// <param name="codeLanguage">Target programming language (default: "python")</param>
        /// <returns>Formatted code generation prompt</returns>
        string GetGenerateCodePrompt(string instruction, string codeLanguage = "python");

        /// <summary>
        /// Process template variables in a prompt template string
        /// </summary>
        /// <param name="template">Template string containing {{INSTRUCTION}} and {{CODE_LANGUAGE}} placeholders</param>
        /// <param name="instruction">Instruction value to substitute</param>
        /// <param name="codeLanguage">Code language value to substitute</param>
        /// <returns>Processed template with variables replaced</returns>
        string ProcessTemplateVariables(string template, string instruction, string codeLanguage);

        #endregion

        #region Action Prompts

        /// <summary>
        /// Build prompt for action analysis from conversation history
        /// </summary>
        /// <param name="input">Action analysis input containing conversation and context</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildActionAnalysisPrompt(AIActionAnalysisInput input);

        /// <summary>
        /// Build prompt for action plan creation
        /// </summary>
        /// <param name="input">Action creation input containing analysis results or description</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildActionCreationPrompt(AIActionCreationInput input);

        #endregion

        #region Summary Prompts

        /// <summary>
        /// Build prompt for stage summary generation
        /// </summary>
        /// <param name="input">Stage summary input containing tasks, questions, and field data</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildStageSummaryPrompt(AIStageSummaryInput input);

        #endregion

        #region Requirements Parsing Prompts

        /// <summary>
        /// Build prompt for requirements parsing from natural language
        /// </summary>
        /// <param name="naturalLanguage">Natural language description to parse</param>
        /// <returns>Formatted prompt string for AI provider</returns>
        string BuildRequirementsParsingPrompt(string naturalLanguage);

        #endregion
    }

    /// <summary>
    /// Workflow information DTO used for workflow modification prompts
    /// </summary>
    public class WorkflowInfo
    {
        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Workflow description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// List of stages in the workflow
        /// </summary>
        public List<WorkflowStageInfo> Stages { get; set; } = new();
    }

    /// <summary>
    /// Stage information DTO used for workflow modification prompts
    /// </summary>
    public class WorkflowStageInfo
    {
        /// <summary>
        /// Stage name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Stage description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Estimated duration in days
        /// </summary>
        public int EstimatedDuration { get; set; }

        /// <summary>
        /// Assigned group/team name
        /// </summary>
        public string AssignedGroup { get; set; } = string.Empty;
    }
}
