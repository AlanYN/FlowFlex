using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum CompaniesFilterEnum
    {
        /// <summary>
        /// 所属公司下全部
        /// </summary>
        [Description("All  Companies")]
        All = 1,

        /// <summary>
        /// 自己创建的
        /// </summary>
        [Description("My  Companies")]
        My = 2
    }
}
