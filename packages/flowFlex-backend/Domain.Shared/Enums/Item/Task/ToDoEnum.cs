using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum ToDoEnum
    {
        /// <summary>
        /// Call task
        /// </summary>
        [Description("Call")]
        Call = 1,

        /// <summary>
        /// Email task
        /// </summary>
        [Description("Email")]
        Email = 2,

        /// <summary>
        /// ToDo task
        /// </summary>
        [Description("ToDo")]
        ToDo = 3
    }
}
