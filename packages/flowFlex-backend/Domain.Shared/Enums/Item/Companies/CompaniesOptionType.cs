using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    public enum CompaniesOptionType
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
        [Description("AddOptionData")]
        AddOption = 3
    }
}
