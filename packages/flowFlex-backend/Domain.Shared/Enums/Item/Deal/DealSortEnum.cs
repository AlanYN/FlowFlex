using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// 排序字段
    /// </summary>
    public enum DealSortEnum
    {
        /// <summary>
        /// 此处为用户手输入的创建时间
        /// </summary>
        [Description("Sort by Create Date")]
        DealDate = 1,

        [Description("Sort by Name")]
        DealName = 2
    }
}
