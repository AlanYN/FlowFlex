using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Outbound field configuration - defines which fields to share with external system
/// </summary>
[SugarTable("ff_outbound_field_config")]
public class OutboundFieldConfig : EntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id")]
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// App code
    /// </summary>
    [SugarColumn(ColumnName = "app_code")]
    public string AppCode { get; set; } = "DEFAULT";

    /// <summary>
    /// Outbound configuration ID
    /// </summary>
    [SugarColumn(ColumnName = "outbound_configuration_id")]
    public long OutboundConfigurationId { get; set; }
    
    /// <summary>
    /// Action ID - associates this field config with a specific action
    /// </summary>
    [SugarColumn(ColumnName = "action_id")]
    public long? ActionId { get; set; }
    
    /// <summary>
    /// WFE field ID
    /// </summary>
    [SugarColumn(ColumnName = "wfe_field_id")]
    public string WfeFieldId { get; set; } = string.Empty;
    
    /// <summary>
    /// External system field name
    /// </summary>
    [SugarColumn(ColumnName = "external_field_name")]
    public string ExternalFieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// Sort order for display
    /// </summary>
    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Whether this field is required
    /// </summary>
    [SugarColumn(ColumnName = "is_required")]
    public bool IsRequired { get; set; }
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent outbound configuration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual OutboundConfiguration? OutboundConfiguration { get; set; }
}

