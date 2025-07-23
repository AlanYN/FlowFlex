using FlowFlex.Domain.Shared;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// Service for executing individual actions
    /// </summary>
    public interface IActionExecutionService : IScopedService
    {
        /// <summary>
        /// Execute a specific action
        /// </summary>
        /// <param name="actionDefinitionId">Action definition ID</param>
        /// <param name="contextData">Context data for execution</param>
        /// <param name="userId">User ID who triggered the execution</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task<JToken> ExecuteActionAsync(
            long actionDefinitionId,
            object contextData = null,
            long? userId = null,
            CancellationToken cancellationToken = default);
    }
}