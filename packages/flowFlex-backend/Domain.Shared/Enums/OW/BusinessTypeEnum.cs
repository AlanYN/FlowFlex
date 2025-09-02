using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.OW
{
    /// <summary>
    /// Business type enumeration for operation log queries
    /// </summary>
    public enum BusinessTypeEnum
    {
        /// <summary>
        /// Workflow business type
        /// </summary>
        [Description("Workflow")]
        Workflow = 1,

        /// <summary>
        /// Stage business type
        /// </summary>
        [Description("Stage")]
        Stage = 2,

        /// <summary>
        /// Checklist business type
        /// </summary>
        [Description("Checklist")]
        Checklist = 3,

        /// <summary>
        /// Questionnaire business type
        /// </summary>
        [Description("Questionnaire")]
        Questionnaire = 4,

        /// <summary>
        /// ChecklistTask business type
        /// </summary>
        [Description("ChecklistTask")]
        ChecklistTask = 5
    }
}