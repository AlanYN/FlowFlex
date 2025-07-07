using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Item�Ĺ�ϵ��ҵ������
    /// </summary>
    public enum RelationalBusinessTypeEnum
    {
        /// <summary>
        /// �ҵ������
        /// </summary>
        [Description("Activity")]
        Activity = 1,

        /// <summary>
        /// ����ҵ������
        /// </summary>
        [Description("Connections")]
        Connections = 2
    }
}
