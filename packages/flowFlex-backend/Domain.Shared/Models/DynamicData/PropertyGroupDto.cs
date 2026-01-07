namespace FlowFlex.Domain.Shared.Models.DynamicData;

/// <summary>
/// Property group DTO
/// </summary>
public class PropertyGroupDto
{
    /// <summary>
    /// Group ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Module ID
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Group name
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Sort order
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Whether is system defined
    /// </summary>
    public bool IsSystemDefine { get; set; }

    /// <summary>
    /// Whether is default group
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Field IDs in this group
    /// </summary>
    public List<long> FieldIds { get; set; } = new();

    /// <summary>
    /// Fields in this group
    /// </summary>
    public List<DefineFieldDto>? Fields { get; set; }
}
