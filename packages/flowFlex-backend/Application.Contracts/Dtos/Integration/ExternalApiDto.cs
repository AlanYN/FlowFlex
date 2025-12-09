using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Workflow info DTO for external API
    /// </summary>
    public class WorkflowInfoDto
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Workflow description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether this is the default workflow
        /// </summary>
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Request DTO for creating a case from external system
    /// </summary>
    public class CreateCaseFromExternalRequest
    {
        /// <summary>
        /// System ID (unique identifier for entity mapping)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SystemId { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID to use for the case
        /// </summary>
        [Required]
        public long WorkflowId { get; set; }

        /// <summary>
        /// Lead/Customer ID from external system
        /// </summary>
        [StringLength(100)]
        public string? LeadId { get; set; }

        /// <summary>
        /// Customer/Lead name
        /// </summary>
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Contact person name
        /// </summary>
        [StringLength(200)]
        public string? ContactName { get; set; }

        /// <summary>
        /// Contact email
        /// </summary>
        [StringLength(200)]
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Contact phone
        /// </summary>
        [StringLength(50)]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Custom fields as JSON
        /// </summary>
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    /// <summary>
    /// Response DTO for created case
    /// </summary>
    public class CreateCaseFromExternalResponse
    {
        /// <summary>
        /// Created case/onboarding ID
        /// </summary>
        public long CaseId { get; set; }

        /// <summary>
        /// Case code
        /// </summary>
        public string CaseCode { get; set; } = string.Empty;

        /// <summary>
        /// Workflow ID
        /// </summary>
        public long WorkflowId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string WorkflowName { get; set; } = string.Empty;

        /// <summary>
        /// Current stage ID
        /// </summary>
        public long CurrentStageId { get; set; }

        /// <summary>
        /// Current stage name
        /// </summary>
        public string CurrentStageName { get; set; } = string.Empty;

        /// <summary>
        /// Case status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// Request DTO for case info query (matches the external system parameters)
    /// Supports both camelCase and space-separated property names
    /// </summary>
    public class CaseInfoRequest
    {
        /// <summary>
        /// Lead ID
        /// </summary>
        [JsonProperty("Lead ID")]
        public string? LeadId { get; set; }

        /// <summary>
        /// Customer Name
        /// </summary>
        [JsonProperty("Customer Name")]
        public string? CustomerName { get; set; }

        /// <summary>
        /// Contact Name
        /// </summary>
        [JsonProperty("Contact Name")]
        public string? ContactName { get; set; }

        /// <summary>
        /// Contact Email
        /// </summary>
        [JsonProperty("Contact Email")]
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Contact Phone
        /// </summary>
        [JsonProperty("Contact Phone")]
        public string? ContactPhone { get; set; }
    }

    /// <summary>
    /// Response DTO for case info (returns same structure as request with additional case data)
    /// Uses the same property names as request for consistency
    /// </summary>
    public class CaseInfoResponse
    {
        /// <summary>
        /// Lead ID
        /// </summary>
        [JsonProperty("Lead ID")]
        public string? LeadId { get; set; }

        /// <summary>
        /// Customer Name
        /// </summary>
        [JsonProperty("Customer Name")]
        public string? CustomerName { get; set; }

        /// <summary>
        /// Contact Name
        /// </summary>
        [JsonProperty("Contact Name")]
        public string? ContactName { get; set; }

        /// <summary>
        /// Contact Email
        /// </summary>
        [JsonProperty("Contact Email")]
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Contact Phone
        /// </summary>
        [JsonProperty("Contact Phone")]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Case ID (if found)
        /// </summary>
        public long? CaseId { get; set; }

        /// <summary>
        /// Case Code (if found)
        /// </summary>
        public string? CaseCode { get; set; }

        /// <summary>
        /// Case Status (if found)
        /// </summary>
        public string? CaseStatus { get; set; }

        /// <summary>
        /// Current Stage Name (if found)
        /// </summary>
        public string? CurrentStageName { get; set; }

        /// <summary>
        /// Workflow Name (if found)
        /// </summary>
        public string? WorkflowName { get; set; }
    }

    /// <summary>
    /// Attachment information DTO from external platform
    /// Used for WFE Attachment integration protocol
    /// </summary>
    public class ExternalAttachmentDto
    {
        /// <summary>
        /// Attachment primary key ID
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// File name (with extension)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes (string format)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string FileSize { get; set; } = string.Empty;

        /// <summary>
        /// File MIME type
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// File extension (without dot)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string FileExt { get; set; } = string.Empty;

        /// <summary>
        /// Creation date (ISO 8601 format or "YYYY-MM-DD HH:mm:ss +00:00")
        /// </summary>
        [Required]
        public string CreateDate { get; set; } = string.Empty;

        /// <summary>
        /// Download link (full URL)
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string DownloadLink { get; set; } = string.Empty;

        /// <summary>
        /// Integration name (source integration)
        /// </summary>
        [StringLength(200)]
        public string? IntegrationName { get; set; }

        /// <summary>
        /// Module name (external module source)
        /// </summary>
        [StringLength(200)]
        public string? ModuleName { get; set; }
    }

    /// <summary>
    /// Request DTO for getting attachments from external platform
    /// </summary>
    public class GetAttachmentsFromExternalRequest
    {
        /// <summary>
        /// Entity ID (e.g., Case ID, Lead ID)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Entity type (optional, for distinguishing different entity types)
        /// </summary>
        [StringLength(100)]
        public string? EntityType { get; set; }

        /// <summary>
        /// Module name (optional, for distinguishing different modules)
        /// </summary>
        [StringLength(200)]
        public string? ModuleName { get; set; }
    }

    /// <summary>
    /// Response DTO for attachments list from external platform
    /// </summary>
    public class GetAttachmentsFromExternalResponse
    {
        /// <summary>
        /// Success flag
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response data
        /// </summary>
        public AttachmentsData? Data { get; set; }

        /// <summary>
        /// Response message or error message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Short message
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// Response code
        /// </summary>
        public string Code { get; set; } = "200";
    }

    /// <summary>
    /// Attachments data container
    /// </summary>
    public class AttachmentsData
    {
        /// <summary>
        /// List of attachments
        /// </summary>
        public List<ExternalAttachmentDto> Attachments { get; set; } = new();

        /// <summary>
        /// Total count of attachments
        /// </summary>
        public int Total { get; set; }
    }
}

