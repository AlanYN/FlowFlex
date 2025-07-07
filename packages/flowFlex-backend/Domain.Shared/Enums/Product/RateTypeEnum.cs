using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums;

public enum RateTypeEnum
{
    [EnumValue(Name = "Unit price")]
    UnitPrice = 1,

    [EnumValue(Name = "Flat rate")]
    FlatRate = 3,
}
