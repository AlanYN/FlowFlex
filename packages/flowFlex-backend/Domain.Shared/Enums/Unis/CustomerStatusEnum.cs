using Item.Common.Lib.Attr;
using System.ComponentModel;
using FlowFlex.Domain.Shared.Attr;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    /// <summary>
    /// Customer account status enumeration
    /// </summary>
    public enum CustomerStatusEnum
    {
        [IgnoreEnumField]
        None = 0,

        /// <summary>
        /// Draft
        /// </summary>
        [Description("Draft")]
        [EnumValue(Name = "Draft")]
        Draft = 1,

        /// <summary>
        /// Active
        /// </summary>
        [Description("Active")]
        [EnumValue(Name = "Active")]
        Active = 2,

        /// <summary>
        /// On hold
        /// </summary>
        [Description("On hold")]
        [IgnoreEnumField]
        OnHold = 3,

        /// <summary>
        /// Inactive/Invalid
        /// </summary>
        [Description("Inactive")]
        [EnumValue(Name = "Inactive")]
        Inactive = 4
    }
}
