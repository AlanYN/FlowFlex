using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// �����ֶ�
    /// </summary>
    public enum DealSortEnum
    {
        /// <summary>
        /// �˴�Ϊ�û�������Ĵ���ʱ��
        /// </summary>
        [Description("Sort by Create Date")]
        DealDate = 1,

        [Description("Sort by Name")]
        DealName = 2
    }
}
