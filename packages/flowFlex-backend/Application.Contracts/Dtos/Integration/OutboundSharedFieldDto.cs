namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Outbound shared field DTO for read-only view
/// </summary>
public class OutboundSharedFieldDto
{
    /// <summary>
    /// Action ID
    /// </summary>
    public long ActionId { get; set; }
    
    /// <summary>
    /// Action name
    /// </summary>
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Field display name
    /// </summary>
    public string FieldDisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Field API name
    /// </summary>
    public string FieldApiName { get; set; } = string.Empty;
}

