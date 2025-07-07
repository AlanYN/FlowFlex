using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    public enum AddressStatusEnum
    {
        /// <summary>
        /// Active
        /// </summary>
        [Description("Active")]
        Active = 1,
        /// <summary>
        /// Inactive/Invalid
        /// </summary>
        [Description("Inactive")]
        Inactive = 2
    }
}
