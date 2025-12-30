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
    /// Field name filter (supports comma-separated values for fuzzy search)
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Display name filter (supports comma-separated values for fuzzy search)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Data type filter
    /// </summary>
    public DataType? DataType { get; set; }

    /// <summary>
    /// Created by filter (supports comma-separated values for fuzzy search)
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// Modified by filter (supports comma-separated values for fuzzy search)
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

    /// <summary>
    /// Get field names as list (splits comma-separated values)
    /// </summary>
    public List<string> GetFieldNameList()
    {
        if (string.IsNullOrEmpty(FieldName))
            return new List<string>();

        return FieldName.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(n => n.Trim())
                       .Where(n => !string.IsNullOrEmpty(n))
                       .ToList();
    }

    /// <summary>
    /// Get display names as list (splits comma-separated values)
    /// </summary>
    public List<string> GetDisplayNameList()
    {
        if (string.IsNullOrEmpty(DisplayName))
            return new List<string>();

        return DisplayName.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(n => n.Trim())
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToList();
    }

    /// <summary>
    /// Get created by users as list (splits comma-separated values)
    /// </summary>
    public List<string> GetCreateByList()
    {
        if (string.IsNullOrEmpty(CreateBy))
            return new List<string>();

        return CreateBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(n => n.Trim())
                      .Where(n => !string.IsNullOrEmpty(n))
                      .ToList();
    }

    /// <summary>
    /// Get modified by users as list (splits comma-separated values)
    /// </summary>
    public List<string> GetModifyByList()
    {
        if (string.IsNullOrEmpty(ModifyBy))
            return new List<string>();

        return ModifyBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(n => n.Trim())
                      .Where(n => !string.IsNullOrEmpty(n))
                      .ToList();
    }
}
