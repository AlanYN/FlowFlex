namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class DropdownItemDto
{
    /// <summary>
    /// Unique identifier for the dropdown item
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// ID of the field this dropdown item belongs to
    /// </summary>
    public long FieldId { get; set; }

    /// <summary>
    /// reference item id
    /// </summary>
    public long? RefFieldId { get; set; }

    /// <summary>
    /// Display name of the dropdown item
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Internal name of the dropdown item
    /// </summary>
    public string ItemName { get; set; }

    /// <summary>
    /// Description of the dropdown item
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Sort order of the dropdown item
    /// </summary>
    public int? Sort { get; set; }

    public bool IsDefault { get; set; }

    /// <summary>
    /// Order adjustment is not allowed
    /// </summary>
    public bool IsAllowDelete { get; set; } = true;
}
