using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// ��˾����ö��
    /// </summary>
    public enum CompaniesSortEnum
    {
        [Description("Sort by Name")]
        Name = 1,

        [Description("Sort by Created Date")]
        CreateDate = 2
    }
}
