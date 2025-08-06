using FlowFlex.Domain.Shared;

namespace Application.Contracts.IServices.Action
{
    /// <summary>
    /// Action code generator service
    /// </summary>
    public interface IActionCodeGeneratorService : IScopedService
    {
        /// <summary>
        /// Generator code
        /// </summary>
        /// <returns></returns>
        Task<string> GeneratorActionCodeAsync();
    }
}
