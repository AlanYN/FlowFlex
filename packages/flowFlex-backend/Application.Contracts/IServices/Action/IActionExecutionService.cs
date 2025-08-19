using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.Action;
using Newtonsoft.Json.Linq;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared.Models;

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
            long? triggerMappingId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute action directly without saving ActionDefinition
        /// </summary>
        /// <param name="actionType">Action type (Python, HttpApi, SendEmail)</param>
        /// <param name="actionConfig">Action configuration JSON string</param>
        /// <param name="contextData">Context data for execution</param>
        /// <returns>Execution result</returns>
        Task<object> ExecuteActionDirectlyAsync(
            ActionTypeEnum actionType,
            string actionConfig,
            object contextData = null);

        #region Execution History

        /// <summary>
        /// Get executions by trigger source ID with action information
        /// </summary>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated executions with action information</returns>
        Task<PageModelDto<ActionExecutionWithActionInfoDto>> GetExecutionsByTriggerSourceIdAsync(
            long triggerSourceId,
            int pageIndex = 1,
            int pageSize = 10,
            List<JsonQueryCondition>? jsonConditions = null);

        #endregion
    }
}