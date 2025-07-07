using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Deal type
    /// </summary>
    public enum DealTypeEnum
    {
        [Description("New Business")]
        NewBusiness = 1,

        [Description("Existing Business")]
        ExistingBusiness = 2
    }
}
