using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Condition action executor interface for Stage Condition feature
    /// </summary>
    public interface IConditionActionExecutor : IScopedService
    {
        /// <summary>
        /// Execute all actions for a condition
        /// </summary>
        /// <param name="actionsJson">Actions JSON configuration</param>
        /// <param name="context">Execution context</param>
        /// <returns>Action execution result</returns>
        Task<ActionExecutionResult> ExecuteActionsAsync(string actionsJson, ActionExecutionContext context);
    }
}
