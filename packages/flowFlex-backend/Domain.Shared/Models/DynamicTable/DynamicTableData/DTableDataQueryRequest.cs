using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models;

/// <summary>
/// Dynamic table data query request parameters
/// </summary>
public class DTableDataQueryRequest
{
    /// <summary>
    /// Page number, starting from 1
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Number of records per page
    /// </summary>
    public int PageSize { get; set; } = 15;

    public SortColumn Sortord { get; set; }

    public List<DTableFilterConfig> Filters { get; set; } = [];

    public List<DTableFilterConfig> SearchFilters { get; set; } = [];

    public long? ViewId { get; set; }

    public bool IsCardView { get; set; }
}
