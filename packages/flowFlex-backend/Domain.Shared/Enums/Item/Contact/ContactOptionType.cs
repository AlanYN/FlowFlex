using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum ContactOptionType
    {
        /// <summary>
        /// 排序列表
        /// </summary>
        [Description("Sorts")]
        Sort = 1,
        /// <summary>
        /// 筛选列表
        /// </summary>
        [Description("Filters")]
        Filter = 2,

        /// <summary>
        /// 添加时的选项数据
        /// </summary>
        [Description("AddOptionDatas")]
        AddOption = 3

    }
}
