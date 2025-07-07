// Ignore Spelling: Uom
using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums.Product;

public enum UomEnum
{
    [EnumValue(Name = "Case")]
    Case = 1,

    [EnumValue(Name = "CNTR")]
    CNTR = 2,

    [EnumValue(Name = "Document")]
    Document = 3,

    [EnumValue(Name = "Hour")]
    Hour = 4,

    [EnumValue(Name = "Label")]
    Label = 5,

    [EnumValue(Name = "Order")]
    Order = 6,

    [EnumValue(Name = "Pallet")]
    Pallet = 7,

    [EnumValue(Name = "Receipt")]
    Receipt = 8,

    [EnumValue(Name = "ReceiptLine")]
    ReceiptLine = 9,

    [EnumValue(Name = "Scan")]
    Scan = 10,

    [EnumValue(Name = "SKU")]
    SKU = 11,

    [EnumValue(Name = "Area")]
    Area = 12,

    [EnumValue(Name = "Trailer")]
    Trailer = 13,

    [EnumValue(Name = "Unit")]
    Unit = 14,

    [EnumValue(Name = "Volume")]
    Volume = 15,

    [EnumValue(Name = "Weight")]
    Weight = 16,

    [EnumValue(Name = "Carton")]
    Carton = 17,

    [EnumValue(Name = "Inner")]
    Inner = 35,

    [EnumValue(Name = "Piece")]
    Piece = 36,

    [EnumValue(Name = "Load")]
    Load = 39,

    [EnumValue(Name = "ItemLine")]
    ItemLine = 40,

    [EnumValue(Name = "Box")]
    Box = 41,

    [EnumValue(Name = "OrderLine")]
    OrderLine = 42,

    [EnumValue(Name = "Rail")]
    Rail = 43,

    [EnumValue(Name = "Pick")]
    Pick = 68,

    [EnumValue(Name = "CapacityType")]
    CapacityType = 70,

    [EnumValue(Name = "Length")]
    Length = 74,

    [EnumValue(Name = "Cubic foot")]
    Cubicfoot = 77,

    [EnumValue(Name = "Cubic meter")]
    CubicMeter = 78,

    [EnumValue(Name = "Pound")]
    Pound = 79,

    [EnumValue(Name = "Metric Ton")]
    MetricTon = 80,

    [EnumValue(Name = "US Ton")]
    USTon = 81,

    [EnumValue(Name = "Each")]
    Each = 82
}
