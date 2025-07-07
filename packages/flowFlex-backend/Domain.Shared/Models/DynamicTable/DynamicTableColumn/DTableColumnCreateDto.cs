using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Domain.Shared.Models;

public class DTableColumnCreateDto
{
    /// <summary>
    /// Associated table ID
    /// </summary>
    
    public long TableId { get; set; }

    /// <summary>
    /// Dynamic field ID
    /// </summary>
    
    public long DynamicFieldId { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Column width (unit: px)
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Minimum width
    /// </summary>
    public int? MinWidth { get; set; }

    /// <summary>
    /// Fixed position (left/right)
    /// </summary>
    public string FixedPosition { get; set; }

    /// <summary>
    /// Whether sortable
    /// </summary>
    public bool IsSortable { get; set; } = false;

    /// <summary>
    /// Whether in link format
    /// </summary>
    public bool IsLink { get; set; } = false;
}
