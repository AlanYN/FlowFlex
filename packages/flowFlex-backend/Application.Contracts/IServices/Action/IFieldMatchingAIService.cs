using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// AI-powered field matching service that uses LLM to resolve
    /// free-text input to the correct option value from lookup lists
    /// </summary>
    public interface IFieldMatchingAIService : IScopedService
    {
        /// <summary>
        /// Match multiple fields' raw values against their respective option lists using AI
        /// </summary>
        /// <param name="fields">Fields with raw values and available options to match against</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Match results for each field with confidence scores</returns>
        Task<List<FieldMatchResult>> MatchFieldsAsync(
            List<FieldMatchContext> fields,
            CancellationToken cancellationToken = default);
    }
}
