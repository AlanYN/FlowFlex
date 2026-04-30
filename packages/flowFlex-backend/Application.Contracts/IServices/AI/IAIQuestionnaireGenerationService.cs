using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI questionnaire generation service interface.
    /// Responsible for questionnaire generation (synchronous and streaming).
    /// </summary>
    public interface IAIQuestionnaireGenerationService : IScopedService
    {
        /// <summary>
        /// Generate questionnaire from natural language description
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Generated questionnaire structure</returns>
        Task<AIQuestionnaireGenerationResult> GenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input);

        /// <summary>
        /// Stream generate questionnaire with real-time updates
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Streaming questionnaire generation</returns>
        IAsyncEnumerable<AIQuestionnaireStreamResult> StreamGenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input);
    }
}
