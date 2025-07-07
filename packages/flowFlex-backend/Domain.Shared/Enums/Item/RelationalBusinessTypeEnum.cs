using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Item的关系的业务类型
    /// </summary>
    public enum RelationalBusinessTypeEnum
    {
        /// <summary>
        /// 活动业务类型
        /// </summary>
        [Description("Activity")]
        Activity = 1,

        /// <summary>
        /// 关联业务类型
        /// </summary>
        [Description("Connections")]
        Connections = 2
    }
}
