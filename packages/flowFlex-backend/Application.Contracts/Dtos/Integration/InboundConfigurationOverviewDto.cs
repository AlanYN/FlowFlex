namespace FlowFlex.Application.Contracts.Dtos.Integration;

/// <summary>
/// Overview DTO for Inbound Configuration - displays summary of all actions
/// </summary>
public class InboundConfigurationOverviewDto
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
    /// Number of entity mappings configured
    /// </summary>
    public int EntityMappingCount { get; set; }
    
    /// <summary>
    /// Number of field mappings configured
    /// </summary>
    public int FieldMappingCount { get; set; }
    
    /// <summary>
    /// Whether attachment configuration exists
    /// </summary>
    public bool HasAttachmentConfig { get; set; }
    
    /// <summary>
    /// Whether auto-create entities is enabled
    /// </summary>
    public bool AutoCreateEntities { get; set; }
    
    /// <summary>
    /// Configuration status
    /// </summary>
    public string Status { get; set; } = "Not Configured";
}

