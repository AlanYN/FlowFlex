using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// Unified AI response parser interface for parsing and processing AI provider responses
    /// Centralizes all response parsing, JSON repair, fallback generation, and confidence scoring logic
    /// extracted from the various AIService partial classes
    /// </summary>
    public interface IAIResponseParser : IScopedService
    {
        #region Workflow Parsing

        /// <summary>
        /// Parse AI response into a workflow generation result, including JSON repair and fallback generation
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <returns>Parsed workflow generation result</returns>
        AIWorkflowGenerationResult ParseWorkflowResponse(string aiResponse);

        /// <summary>
        /// Attempt to repair potentially corrupted JSON and parse into a workflow result
        /// </summary>
        /// <param name="aiResponse">Raw AI response string with potentially corrupted JSON</param>
        /// <returns>Parsed workflow generation result, or null if repair fails</returns>
        AIWorkflowGenerationResult? TryRepairAndParseWorkflow(string aiResponse);

        /// <summary>
        /// Generate a fallback workflow result when JSON parsing fails completely
        /// </summary>
        /// <param name="aiResponse">Raw AI response string used for text-based extraction</param>
        /// <returns>Fallback workflow generation result with reduced confidence</returns>
        AIWorkflowGenerationResult GenerateFallbackWorkflow(string aiResponse);

        #endregion

        #region Questionnaire Parsing

        /// <summary>
        /// Parse AI response into a questionnaire generation result
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <returns>Parsed questionnaire generation result</returns>
        AIQuestionnaireGenerationResult ParseQuestionnaireResponse(string aiResponse);

        /// <summary>
        /// Parse AI response into a questionnaire generation result for a specific stage
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <param name="stage">Stage context for the questionnaire</param>
        /// <returns>Parsed questionnaire generation result</returns>
        AIQuestionnaireGenerationResult ParseAIQuestionnaireResponse(string aiResponse, AIStageGenerationResult stage);

        /// <summary>
        /// Parse batch AI response into multiple questionnaire generation results
        /// </summary>
        /// <param name="aiResponse">Raw AI response string containing multiple questionnaires</param>
        /// <param name="stages">List of stages for context</param>
        /// <returns>List of parsed questionnaire generation results, or null if parsing fails</returns>
        List<AIQuestionnaireGenerationResult>? ParseBatchQuestionnaireResponse(string aiResponse, List<AIStageGenerationResult> stages);

        #endregion

        #region Checklist Parsing

        /// <summary>
        /// Parse AI response into a checklist generation result
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <returns>Parsed checklist generation result</returns>
        AIChecklistGenerationResult ParseChecklistResponse(string aiResponse);

        /// <summary>
        /// Parse AI response into a checklist generation result for a specific stage
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <param name="stage">Stage context for the checklist</param>
        /// <returns>Parsed checklist generation result</returns>
        AIChecklistGenerationResult ParseAIChecklistResponse(string aiResponse, AIStageGenerationResult stage);

        /// <summary>
        /// Parse batch AI response into multiple checklist generation results
        /// </summary>
        /// <param name="aiResponse">Raw AI response string containing multiple checklists</param>
        /// <param name="stages">List of stages for context</param>
        /// <returns>List of parsed checklist generation results, or null if parsing fails</returns>
        List<AIChecklistGenerationResult>? ParseBatchChecklistResponse(string aiResponse, List<AIStageGenerationResult> stages);

        #endregion

        #region Chat Parsing

        /// <summary>
        /// Parse AI chat response content into a structured chat response
        /// </summary>
        /// <param name="content">AI response content string</param>
        /// <param name="input">Original chat input for context</param>
        /// <returns>Structured chat response</returns>
        AIChatResponse ParseChatResponse(string content, AIChatInput input);

        /// <summary>
        /// Generate a fallback chat response when AI service is unavailable
        /// </summary>
        /// <param name="input">Original chat input for context</param>
        /// <returns>Fallback chat response with helpful suggestions</returns>
        AIChatResponse GenerateFallbackChatResponse(AIChatInput input);

        /// <summary>
        /// Generate an error chat response with specific error information
        /// </summary>
        /// <param name="input">Original chat input for context</param>
        /// <param name="errorMessage">Error message describing the failure</param>
        /// <returns>Error chat response with troubleshooting suggestions</returns>
        AIChatResponse GenerateErrorChatResponse(AIChatInput input, string errorMessage);

        #endregion

        #region Summary Parsing

        /// <summary>
        /// Parse AI response into a stage summary result
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <param name="input">Original summary input for context</param>
        /// <returns>Structured stage summary result</returns>
        AIStageSummaryResult ParseStageSummaryResponse(string aiResponse, AIStageSummaryInput input);

        #endregion

        #region Requirements Parsing

        /// <summary>
        /// Parse AI response into a requirements parsing result
        /// </summary>
        /// <param name="aiResponse">Raw AI response string</param>
        /// <returns>Structured requirements parsing result</returns>
        AIRequirementsParsingResult ParseRequirementsResponse(string aiResponse);

        #endregion

        #region Utility

        /// <summary>
        /// Fix common JSON formatting issues in AI responses
        /// Handles: unquoted property names, single quotes, trailing commas, missing commas between objects
        /// </summary>
        /// <param name="jsonContent">JSON string with potential formatting issues</param>
        /// <returns>Fixed JSON string</returns>
        string FixJsonContent(string jsonContent);

        /// <summary>
        /// Parse question options from AI response, supporting both string array and object array formats
        /// </summary>
        /// <param name="options">List of option strings (may be plain text or JSON objects)</param>
        /// <param name="questionIndex">Index of the question for generating option IDs</param>
        /// <returns>List of structured question option DTOs</returns>
        List<QuestionOptionDto> ParseQuestionOptions(List<string> options, int questionIndex);

        #endregion

        #region Confidence Scoring

        /// <summary>
        /// Calculate confidence score for a generated workflow
        /// </summary>
        /// <param name="workflow">Generated workflow DTO</param>
        /// <returns>Confidence score between 0.0 and 1.0</returns>
        double CalculateConfidenceScore(WorkflowInputDto workflow);

        /// <summary>
        /// Calculate confidence score for a generated questionnaire
        /// </summary>
        /// <param name="questionnaire">Generated questionnaire DTO</param>
        /// <returns>Confidence score between 0.0 and 1.0</returns>
        double CalculateQuestionnaireConfidenceScore(QuestionnaireInputDto questionnaire);

        /// <summary>
        /// Calculate confidence score for a generated checklist
        /// </summary>
        /// <param name="checklist">Generated checklist DTO</param>
        /// <returns>Confidence score between 0.0 and 1.0</returns>
        double CalculateChecklistConfidenceScore(ChecklistInputDto checklist);

        /// <summary>
        /// Calculate quality score for a workflow based on validation issues
        /// </summary>
        /// <param name="workflow">Workflow DTO to evaluate</param>
        /// <param name="issues">List of validation issues found</param>
        /// <returns>Quality score between 0.0 and 1.0</returns>
        double CalculateWorkflowQualityScore(WorkflowInputDto workflow, List<AIValidationIssue> issues);

        #endregion
    }
}
