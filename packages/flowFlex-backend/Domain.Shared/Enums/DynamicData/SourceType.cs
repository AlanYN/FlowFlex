using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums.DynamicData;

public enum SourceType
{
    None = 0,

    [EnumValue("country")]
    Country,

    [EnumValue("enum")]
    Enum
}
