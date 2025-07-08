using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Domain.Shared.Models;

/// <summary>
/// Request for reordering columns in a dynamic table
/// </summary>
public class DTableColumnReorderDto
{
    /// <summary>
    /// ID of the column being moved
    /// </summary>

    public long MovedColumn { get; set; }

    /// <summary>
    /// Target position (after which column ID, 0 means at beginning)
    /// </summary>
    public long TargetPosition { get; set; }
}
