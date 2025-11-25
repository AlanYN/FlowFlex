using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Output DTO for attachment sharing configuration
    /// </summary>
    public class AttachmentSharingOutputDto
    {
        /// <summary>
        /// Attachment sharing ID
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }
        
        /// <summary>
        /// External module name
        /// </summary>
        public string ExternalModuleName { get; set; } = string.Empty;
        
        /// <summary>
        /// System ID (auto-generated)
        /// </summary>
        public string SystemId { get; set; } = string.Empty;
        
        /// <summary>
        /// Available workflow IDs
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();
        
        /// <summary>
        /// Whether this attachment sharing is active
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Allowed file types
        /// </summary>
        public List<string>? AllowedFileTypes { get; set; }
        
        /// <summary>
        /// Maximum file size in MB
        /// </summary>
        public int? MaxFileSizeMB { get; set; }
        
        /// <summary>
        /// Creation date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
        
        /// <summary>
        /// Last modification date
        /// </summary>
        public DateTimeOffset? ModifyDate { get; set; }
        
        /// <summary>
        /// Created by
        /// </summary>
        public string? CreateBy { get; set; }
        
        /// <summary>
        /// Modified by
        /// </summary>
        public string? ModifyBy { get; set; }
    }
}

