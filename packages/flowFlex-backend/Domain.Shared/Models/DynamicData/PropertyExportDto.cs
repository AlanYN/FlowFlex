namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Property export DTO for Excel
/// </summary>
public class PropertyExportDto
{
    /// <summary>
    /// Field ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Field name (also used as display name)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Data type name
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Is required
    /// </summary>
    public string IsRequired { get; set; } = string.Empty;

    /// <summary>
    /// Is system define
    /// </summary>
    public string IsSystemDefine { get; set; } = string.Empty;

    /// <summary>
    /// Created by
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Create date
    /// </summary>
    public string CreateDate { get; set; } = string.Empty;

    /// <summary>
    /// Modified by
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>
    /// Modify date
    /// </summary>
    public string ModifyDate { get; set; } = string.Empty;
}
