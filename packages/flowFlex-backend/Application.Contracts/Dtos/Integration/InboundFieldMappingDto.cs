namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Inbound field mapping DTO for read-only view
/// </summary>
public class InboundFieldMappingDto
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
    /// External system field name (API Name)
    /// </summary>
    public string ExternalFieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// WFE field ID (API Name)
    /// </summary>
    public string WfeFieldId { get; set; } = string.Empty;
    
    /// <summary>
    /// WFE field display name
    /// </summary>
    public string WfeFieldName { get; set; } = string.Empty;
}

