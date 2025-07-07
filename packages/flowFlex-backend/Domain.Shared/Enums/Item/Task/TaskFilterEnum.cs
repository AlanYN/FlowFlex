using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Task filter
    /// </summary>
    public enum TaskFilterEnum
    {
        /// <summary>
        /// Default all
        /// </summary>
        [Description("All Types")]
        AllTypes = 0,

        /// <summary>
        /// Call type
        /// </summary>
        [Description("Call Tasks")]
        CallTasks = 1,

        /// <summary>
        /// Email type
        /// </summary>
        [Description("Email Tasks")]
        EmailTasks = 2,

        /// <summary>
        /// To-do type
        /// </summary>
        [Description("To-do Tasks")]
        ToDoTasks = 3
    }
}
