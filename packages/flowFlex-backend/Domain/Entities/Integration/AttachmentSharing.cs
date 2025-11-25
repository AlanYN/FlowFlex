using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.Integration;

/// <summary>
/// Attachment Sharing - Configure which attachments to receive from external system modules
/// </summary>
[SugarTable("ff_attachment_sharing")]
public class AttachmentSharing : EntityBaseCreateInfo
{
    /// <summary>
    /// Integration ID
    /// </summary>
    [SugarColumn(ColumnName = "integration_id")]
    public long IntegrationId { get; set; }
    
    /// <summary>
    /// External module name (user input, e.g., "Documents", "Files")
    /// </summary>
    [SugarColumn(ColumnName = "external_module_name", Length = 200)]
    public string ExternalModuleName { get; set; } = string.Empty;
    
    /// <summary>
    /// System ID (auto-generated unique identifier for API calls)
    /// </summary>
    [SugarColumn(ColumnName = "system_id", Length = 100)]
    public string SystemId { get; set; } = string.Empty;
    
    /// <summary>
    /// Available workflow IDs (JSON array)
    /// </summary>
    [SugarColumn(ColumnName = "workflow_ids", ColumnDataType = "text")]
    public string WorkflowIds { get; set; } = "[]";
    
    /// <summary>
    /// Whether this attachment sharing is active
    /// </summary>
    [SugarColumn(ColumnName = "is_active")]
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Description of the attachment sharing configuration
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Allowed file types (JSON array, e.g., ["pdf", "docx", "xlsx"])
    /// </summary>
    [SugarColumn(ColumnName = "allowed_file_types", ColumnDataType = "text", IsNullable = true)]
    public string? AllowedFileTypes { get; set; }
    
    /// <summary>
    /// Maximum file size in MB (null means no limit)
    /// </summary>
    [SugarColumn(ColumnName = "max_file_size_mb", IsNullable = true)]
    public int? MaxFileSizeMB { get; set; }
    
    // Navigation Properties (ignored by SqlSugar)
    
    /// <summary>
    /// Parent integration
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public virtual Integration? Integration { get; set; }
}

