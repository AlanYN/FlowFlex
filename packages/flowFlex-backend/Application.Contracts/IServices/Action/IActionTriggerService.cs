using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// Service for triggering and executing actions based on business events
    /// </summary>
    public interface IActionTriggerService : IScopedService
    {
        /// <summary>
        /// Execute actions for a specific trigger
        /// </summary>
        /// <param name="triggerSourceType">Trigger source type (Stage, Task, Question, etc.)</param>
        /// <param name="triggerSourceId">Trigger source ID</param>
        /// <param name="triggerEventType">Trigger event type (Completed, Created, etc.)</param>
        /// <param name="contextData">Additional context data</param>
        /// <param name="userId">User ID who triggered the event</param>
        /// <param name="workflowId"></param>
        /// <param name="stageId"></param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ExecuteActionsForTriggerAsync(
            string triggerSourceType,
            long triggerSourceId,
            string triggerEventType,
            object contextData = null,
            long? userId = null,
            long? workflowId = null,
            long? stageId = null,
            CancellationToken cancellationToken = default);
    }
}