using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// Action executor interface - minimal design for easy extension
    /// </summary>
    public interface IActionExecutor : IScopedService
    {
        /// <summary>
        /// Action type that this executor handles
        /// </summary>
        string ActionType { get; }

        /// <summary>
        /// Execute action with configuration and trigger context
        /// </summary>
        Task<object> ExecuteAsync(string config, object triggerContext);
    }
}