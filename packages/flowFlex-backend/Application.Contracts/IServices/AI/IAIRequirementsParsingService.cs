using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI requirements parsing service interface.
    /// Responsible for parsing natural language into structured requirements.
    /// </summary>
    public interface IAIRequirementsParsingService : IScopedService
    {
        /// <summary>
        /// Parse natural language into structured requirements
        /// </summary>
        /// <param name="naturalLanguage">Natural language input</param>
        /// <returns>Structured requirements</returns>
        Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage);

        /// <summary>
        /// Parse natural language with explicit AI model override
        /// </summary>
        /// <param name="naturalLanguage">Natural language input</param>
        /// <param name="modelProvider">AI provider name, e.g. zhipuai/openai/anthropic</param>
        /// <param name="modelName">Model name, e.g. glm-4/gpt-4o/claude-3</param>
        /// <param name="modelId">Optional user model configuration id</param>
        /// <returns>Structured requirements</returns>
        Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage,
            string? modelProvider, string? modelName, string? modelId);
    }
}
