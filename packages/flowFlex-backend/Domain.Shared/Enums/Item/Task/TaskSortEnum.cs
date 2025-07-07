using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum TaskSortEnum
    {
        /// <summary>
        /// Due date ascending
        /// </summary>
        [Description("Sort by Due Date")]
        DateOfExpiration = 1,
        /// <summary>
        /// Priority descending
        /// </summary>
        [Description("Sort by Priority High-Low")]
        Priority = 2,
        /// <summary>
        /// Date
        /// </summary>
        [Description("Sort by Created Date")]
        CreateDate = 3

    }
}
