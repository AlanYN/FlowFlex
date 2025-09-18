using FlowFlex.Domain.Shared.Enums.Action;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// System action definition DTO
    /// </summary>
    public class SystemActionDefinitionDto
    {
        /// <summary>
        /// Action name (internal identifier)
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Action description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Configuration schema (for documentation)
        /// </summary>
        public object ConfigSchema { get; set; } = new object();

        /// <summary>
        /// Example configuration JSON
        /// </summary>
        public string ExampleConfig { get; set; } = string.Empty;

        /// <summary>
        /// Trigger type scope - defines where this system action can be used (Stage, Task, Question, Workflow)
        /// </summary>
        public TriggerTypeEnum TriggerType { get; set; } = TriggerTypeEnum.Task;
    }
}