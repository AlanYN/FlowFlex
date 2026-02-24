using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI action service interface.
    /// Responsible for action analysis and creation (synchronous and streaming),
    /// and HTTP configuration generation.
    /// </summary>
    public interface IAIActionService : IScopedService
    {
        /// <summary>
        /// Analyze conversation to extract action insights
        /// </summary>
        /// <param name="input">Action analysis input</param>
        /// <returns>Action analysis result</returns>
        Task<AIActionAnalysisResult> AnalyzeActionAsync(AIActionAnalysisInput input);

        /// <summary>
        /// Create action plan based on analysis or description
        /// </summary>
        /// <param name="input">Action creation input</param>
        /// <returns>Action creation result</returns>
        Task<AIActionCreationResult> CreateActionAsync(AIActionCreationInput input);

        /// <summary>
        /// Stream analyze action with real-time updates
        /// </summary>
        /// <param name="input">Action analysis input</param>
        /// <returns>Streaming action analysis</returns>
        IAsyncEnumerable<AIActionStreamResult> StreamAnalyzeActionAsync(AIActionAnalysisInput input);

        /// <summary>
        /// Stream create action with real-time updates
        /// </summary>
        /// <param name="input">Action creation input</param>
        /// <returns>Streaming action creation</returns>
        IAsyncEnumerable<AIActionStreamResult> StreamCreateActionAsync(AIActionCreationInput input);

        /// <summary>
        /// Generate HTTP configuration directly from user input (optimized single-step process)
        /// </summary>
        /// <param name="input">HTTP configuration generation input</param>
        /// <returns>Streaming HTTP configuration generation</returns>
        IAsyncEnumerable<AIActionStreamResult> StreamGenerateHttpConfigAsync(AIHttpConfigGenerationInput input);
    }
}
