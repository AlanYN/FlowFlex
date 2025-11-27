using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Action
{
    /// <summary>
    /// Trigger type enumeration
    /// </summary>
    public enum TriggerTypeEnum
    {
        /// <summary>
        /// Stage completion trigger
        /// </summary>
        [Description("Stage")]
        Stage = 1,

        /// <summary>
        /// Checklist task completion trigger
        /// </summary>
        [Description("Task")]
        Task = 2,

        /// <summary>
        /// Questionnaire question answer trigger
        /// </summary>
        [Description("Question")]
        Question = 3,

        /// <summary>
        /// Workflow completion trigger
        /// </summary>
        [Description("Workflow")]
        Workflow = 4,

        /// <summary>
        /// Integration trigger - TriggerSourceId is IntegrationId
        /// </summary>
        [Description("Integration")]
        Integration = 5
    }
}