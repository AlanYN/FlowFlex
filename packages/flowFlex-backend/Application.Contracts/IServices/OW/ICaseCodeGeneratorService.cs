using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Case code generator service interface
    /// </summary>
    public interface ICaseCodeGeneratorService : IScopedService
    {
        /// <summary>
        /// Generate case code based on lead name
        /// </summary>
        /// <param name="leadName">Lead name to generate code from</param>
        /// <returns>Generated case code</returns>
        Task<string> GenerateCaseCodeAsync(string leadName);
    }
}

