using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Unis.OperationalAttributes
{
    public enum FreightPackagedEnum
    {
        [Description("Bundles")]
        Bundles = 1,
        [Description("Cartons")]
        Cartons = 2,
        [Description("Crates")]
        Crates = 3,
        [Description("Drums")]
        Drums = 4,
        [Description("Loose")]
        Loose = 5,
        [Description("Pallets")]
        Pallets = 6
    }
}
