using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis
{
    /// <summary>
    /// Customer account category - internal/external
    /// </summary>
    public enum CustomerAccountsCategoryEnum
    {
        [Description("Internal")]
        Internal = 1,

        [Description("Normal")]
        Normal = 2
    }
}
