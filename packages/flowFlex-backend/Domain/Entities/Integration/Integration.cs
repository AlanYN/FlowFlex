using FlowFlex.Domain.Entities.Base;
using Domain.Shared.Enums;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Integration entity - represents a connection to an external system
/// </summary>
[SugarTable("ff_integration")]
public class Integration : EntityBaseCreateInfo
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id")]
    public new string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// App code
    /// </summary>
    [SugarColumn(ColumnName = "app_code")]
    public new string AppCode { get; set; } = "default";

    /// <summary>
    /// Integration type (CRM, ERP, Marketing, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Integration name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Integration description
    /// </summary>
    public string? Description { get; set; }

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
    [SugarColumn(ColumnName = "credentials")]
    public string EncryptedCredentials { get; set; } = string.Empty;

    /// <summary>
    /// Number of configured entity types (computed property, not stored in database)
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public int ConfiguredEntityTypes { get; set; }

    /// <summary>
    /// Last synchronization date
    /// </summary>
    public DateTimeOffset? LastSyncDate { get; set; }

    /// <summary>
    /// Error message if connection failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Inbound attachments configuration (JSON array)
    /// Contains: module_name, workflow_id, stage_id
    /// </summary>
    [SugarColumn(ColumnName = "inbound_attachments", ColumnDataType = "text", IsNullable = true)]
    public string? InboundAttachments { get; set; }

    /// <summary>
    /// Outbound attachments configuration (JSON array)
    /// Contains: workflow_id, stage_id
    /// </summary>
    [SugarColumn(ColumnName = "outbound_attachments", ColumnDataType = "text", IsNullable = true)]
    public string? OutboundAttachments { get; set; }

    // Navigation Properties (ignored by SqlSugar)

    /// <summary>
    /// Entity mappings
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual ICollection<EntityMapping> EntityMappings { get; set; } = new List<EntityMapping>();

    /// <summary>
    /// Integration actions
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual ICollection<IntegrationAction> IntegrationActions { get; set; } = new List<IntegrationAction>();

    /// <summary>
    /// Quick links
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual ICollection<QuickLink> QuickLinks { get; set; } = new List<QuickLink>();
}

