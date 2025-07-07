using Item.Common.Lib.Attr;

namespace FlowFlex.Domain.Shared.Enums;

public enum FileType
{
    [EnumValue(Name = "application/pdf")]
    Pdf = 1,

    [EnumValue(Name = "application/msword")]
    Docx
}
