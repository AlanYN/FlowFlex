using FlowFlex.Domain.Shared.Enums.Action;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Action definition summary DTO - lightweight version for listing all enabled actions
    /// </summary>
    public class ActionDefinitionSummaryDto
    {
        /// <summary>
        /// Action definition ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Action type
        /// </summary>
        public ActionTypeEnum ActionType { get; set; }

        /// <summary>
        /// Action code
        /// </summary>
        public string ActionCode { get; set; } = string.Empty;

        /// <summary>
        /// Trigger type for the action (Stage, Task, Question, Workflow, Integration)
        /// </summary>
        public string? TriggerType { get; set; }
    }
}
