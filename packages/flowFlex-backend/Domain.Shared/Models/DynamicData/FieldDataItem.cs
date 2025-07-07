using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Represents a single field data item in the dynamic data structure
/// </summary>
public class FieldDataItem
{
    /// <summary>
    /// Gets or sets the business identifier
    /// </summary>
    public long BusinessId { get; set; }

    public bool IsDisplayField { get; set; }

    /// <summary>
    /// Gets or sets the group identifier
    /// </summary>
    public long? GroupId { get; set; }

    /// <summary>
    /// Gets or sets the field name
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the display name of the field
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the value of the field
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Gets or sets the field identifier
    /// </summary>
    public long FieldId { get; set; }

    /// <summary>
    /// Gets or sets the data type of the field
    /// </summary>
    public DataType DataType { get; set; }

    /// <summary>
    /// Gets or sets the description of the field
    /// </summary>
    public string Description { get; set; }

    public int Sort { get; set; }

    public bool IsHidden { get; set; }
}
