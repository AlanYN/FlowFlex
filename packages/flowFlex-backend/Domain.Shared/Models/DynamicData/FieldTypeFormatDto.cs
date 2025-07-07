using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class FieldTypeFormatDto
{
    /// <summary>
    /// ID of the format settings
    /// </summary>
    public long FormatId { get; set; }

    /// <summary>
    /// Number format settings for the field
    /// </summary>
    public NumberTypeFormat NumberFormat { get; set; }
}
