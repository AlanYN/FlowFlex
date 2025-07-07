using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums;

// Enum representing different types of unit discounts
public enum UnitDiscountEnum
{
    // Discount by a fixed amount
    [EnumValue(Name = "USD", Order = 1)]
    ByAmount = 1,

    // Discount by a percentage
    [EnumValue(Name = "%", Order = 2)]
    ByPercentage = 2,
}
