using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Contact sorting
    /// </summary>
    public enum ContactSortEnum
    {
        [Description("Sort by Create Date")]
        CreateDate = 1,

        [Description("Sort by First Name")]
        FirstName = 2,

        [Description("Sort by Last Name")]
        LastName = 3
    }
}
