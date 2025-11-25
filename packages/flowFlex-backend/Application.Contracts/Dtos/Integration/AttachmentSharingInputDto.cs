using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Input DTO for creating/updating attachment sharing configuration
    /// </summary>
    public class AttachmentSharingInputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }
        
        /// <summary>
        /// External module name (user input)
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string ExternalModuleName { get; set; } = string.Empty;
        
        /// <summary>
        /// Available workflow IDs
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();
        
        /// <summary>
        /// Whether this attachment sharing is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Description
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Allowed file types (e.g., ["pdf", "docx", "xlsx"])
        /// </summary>
        public List<string>? AllowedFileTypes { get; set; }
        
        /// <summary>
        /// Maximum file size in MB (null means no limit)
        /// </summary>
        public int? MaxFileSizeMB { get; set; }
    }
}

