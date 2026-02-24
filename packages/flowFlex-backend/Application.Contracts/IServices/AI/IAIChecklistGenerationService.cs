using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI checklist generation service interface.
    /// Responsible for checklist generation (synchronous and streaming).
    /// </summary>
    public interface IAIChecklistGenerationService : IScopedService
    {
        /// <summary>
        /// Generate checklist from natural language description
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Generated checklist structure</returns>
        Task<AIChecklistGenerationResult> GenerateChecklistAsync(AIChecklistGenerationInput input);

        /// <summary>
        /// Stream generate checklist with real-time updates
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Streaming checklist generation</returns>
        IAsyncEnumerable<AIChecklistStreamResult> StreamGenerateChecklistAsync(AIChecklistGenerationInput input);
    }
}
