using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Property query request
/// </summary>
public class PropertyQueryRequest
{
    /// <summary>
    /// Page index (1-based)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Field name filter (supports fuzzy search)
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Display name filter (supports fuzzy search)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Data type filter
    /// </summary>
    public DataType? DataType { get; set; }

    /// <summary>
    /// Created by filter
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// Modified by filter
    /// </summary>
    public string? ModifyBy { get; set; }

    /// <summary>
    /// Create date start
    /// </summary>
    public DateTimeOffset? CreateDateStart { get; set; }

    /// <summary>
    /// Create date end
    /// </summary>
    public DateTimeOffset? CreateDateEnd { get; set; }

    /// <summary>
    /// Modify date start
    /// </summary>
    public DateTimeOffset? ModifyDateStart { get; set; }

    /// <summary>
    /// Modify date end
    /// </summary>
    public DateTimeOffset? ModifyDateEnd { get; set; }

    /// <summary>
    /// Sort field (fieldName, displayName, dataType, createDate, modifyDate)
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// Sort ascending
    /// </summary>
    public bool IsAsc { get; set; } = false;
}
