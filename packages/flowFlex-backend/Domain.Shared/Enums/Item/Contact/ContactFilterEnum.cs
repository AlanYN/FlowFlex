using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// ��ϵ��ɸѡ
    /// </summary>
    public enum ContactFilterEnum
    {
        /// <summary>
        /// ������˾��ȫ��
        /// </summary>
        [Description("All Contacts")]
        All = 1,

        /// <summary>
        /// �Լ�������
        /// </summary>
        [Description("My Contacts")]
        My = 2
    }
}
