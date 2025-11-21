using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration entity - represents a connection to an external system
/// </summary>
public class Integration : EntityBaseCreateInfo
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public new string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// Integration type (CRM, ERP, Marketing, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Integration name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection status
    /// </summary>
    public IntegrationStatus Status { get; set; }
    
    /// <summary>
    /// External system name
    /// </summary>
    public string SystemName { get; set; } = string.Empty;
    
    /// <summary>
    /// External system API endpoint URL
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Authentication method
    /// </summary>
    public AuthenticationMethod AuthMethod { get; set; }
    
    /// <summary>
    /// Encrypted authentication credentials (JSON)
    /// </summary>
    public string EncryptedCredentials { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of configured entity types
    /// </summary>
    public int ConfiguredEntityTypes { get; set; }
    
    // Navigation Properties
    
    /// <summary>
    /// Entity mappings
    /// </summary>
    public virtual ICollection<EntityMapping> EntityMappings { get; set; } = new List<EntityMapping>();
    
    /// <summary>
    /// Field mappings
    /// </summary>
    public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
    
    /// <summary>
    /// Integration actions
    /// </summary>
    public virtual ICollection<IntegrationAction> IntegrationActions { get; set; } = new List<IntegrationAction>();
    
    /// <summary>
    /// Quick links
    /// </summary>
    public virtual ICollection<QuickLink> QuickLinks { get; set; } = new List<QuickLink>();
    
    /// <summary>
    /// Sync logs
    /// </summary>
    public virtual ICollection<IntegrationSyncLog> SyncLogs { get; set; } = new List<IntegrationSyncLog>();
    
    /// <summary>
    /// Inbound configuration
    /// </summary>
    public virtual InboundConfiguration? InboundConfig { get; set; }
    
    /// <summary>
    /// Outbound configuration
    /// </summary>
    public virtual OutboundConfiguration? OutboundConfig { get; set; }
}

