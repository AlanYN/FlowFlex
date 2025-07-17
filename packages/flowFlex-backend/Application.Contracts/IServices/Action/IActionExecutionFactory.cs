using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// Action execution factory interface - creates action executor instances
    /// </summary>
    public interface IActionExecutionFactory : IScopedService
    {
        /// <summary>
        /// Create action executor instance by action type
        /// </summary>
        IActionExecutor CreateExecutor(string actionType);
    }
}