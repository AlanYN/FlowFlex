using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    /// <summary>
    /// Address type
    /// </summary>
    public enum AddressTypeEnum
    {
        [Description("Primary")]
        Primary = 1,
        [Description("Shipto")]
        Shipto = 2,
        [Description("Billto")]
        Billto = 3,
        [Description("Statementto")]
        Statementto = 4
    }
}
