using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI summary service interface.
    /// Responsible for stage summary generation.
    /// </summary>
    public interface IAISummaryService : IScopedService
    {
        /// <summary>
        /// Generate AI summary for stage based on checklist tasks and questionnaire questions
        /// </summary>
        /// <param name="input">Stage summary generation input</param>
        /// <returns>Generated stage summary</returns>
        Task<AIStageSummaryResult> GenerateStageSummaryAsync(AIStageSummaryInput input);
    }
}
