using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.DealWarning
{
    /// <summary>
    /// 建议优先级枚�?
    /// </summary>
    public enum DwSuggestionPriorityEnum
    {
        /// <summary>
        /// 低优先级
        /// </summary>
        [Description("Low")]
        Low = 1,

        /// <summary>
        /// 中优先级
        /// </summary>
        [Description("Medium")]
        Medium = 2,

        /// <summary>
        /// 高优先级
        /// </summary>
        [Description("High")]
        High = 3
    }
}
