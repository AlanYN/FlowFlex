using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// ���ȼ�
    /// </summary>
    public enum PriorityEnum
    {
        [Description("Low")]
        Low = 1,

        [Description("Medium")]
        Medium = 2,

        [Description("High")]
        High = 3
    }
}
