using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum ContactOptionType
    {
        /// <summary>
        /// �����б�
        /// </summary>
        [Description("Sorts")]
        Sort = 1,
        /// <summary>
        /// ɸѡ�б�
        /// </summary>
        [Description("Filters")]
        Filter = 2,

        /// <summary>
        /// ���ʱ��ѡ������
        /// </summary>
        [Description("AddOptionDatas")]
        AddOption = 3

    }
}
