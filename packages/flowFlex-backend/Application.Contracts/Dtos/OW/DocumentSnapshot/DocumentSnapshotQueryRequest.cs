namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;

/// <summary>
/// Document snapshot paged query request
/// </summary>
public class DocumentSnapshotQueryRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Title keyword filter (optional)
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Document type filter: Sheet / Doc / Slide (optional, empty = all)
    /// </summary>
    public string? DocType { get; set; }

    /// <summary>
    /// Sort field: modify_date (default) / create_date / title
    /// </summary>
    public string? SortBy { get; set; } = "modify_date";

    /// <summary>
    /// Sort order: desc (default) / asc
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}
