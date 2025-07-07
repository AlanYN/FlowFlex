using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// 联系人筛选
    /// </summary>
    public enum ContactFilterEnum
    {
        /// <summary>
        /// 所属公司下全部
        /// </summary>
        [Description("All Contacts")]
        All = 1,

        /// <summary>
        /// 自己创建的
        /// </summary>
        [Description("My Contacts")]
        My = 2
    }
}
