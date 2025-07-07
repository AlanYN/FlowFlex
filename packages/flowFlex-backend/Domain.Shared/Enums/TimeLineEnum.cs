using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums;

public enum TimeLineEnum
{
    [EnumValue(Name = "Current")]
    Current = 1,

    [EnumValue(Name = "Due today")]
    DueToday = 2,

    [EnumValue(Name = "Future")]
    Future = 3,

    [EnumValue(Name = "Past")]
    Past = 4
}
