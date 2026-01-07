using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Field data item - represents a single field value in dynamic data
/// </summary>
public class FieldDataItem
{
    /// <summary>
    /// Business data ID
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// Field group ID
    /// </summary>
    public long? GroupId { get; set; }

    /// <summary>
    /// Field name (identifier)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Text value for dropdown options
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Field definition ID
    /// </summary>
    public long FieldId { get; set; }

    /// <summary>
    /// Data type
    /// </summary>
    public DataType DataType { get; set; }

    /// <summary>
    /// Field description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Whether field is hidden
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Whether field is display field
    /// </summary>
    public bool IsDisplayField { get; set; }
}
