using System.Collections.Generic;
using Domain.Shared.Enums;
using FlowFlex.Application.Contracts.Dtos.Action;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Integration output DTO
    /// </summary>
    public class IntegrationOutputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public long TenantId { get; set; }

        /// <summary>
        /// Integration type
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
        /// Authentication credentials (decrypted, only returned in GetWithDetails)
        /// </summary>
        public Dictionary<string, string>? Credentials { get; set; }

        /// <summary>
        /// Number of configured entity types
        /// </summary>
        public int ConfiguredEntityTypes { get; set; }

        /// <summary>
        /// Configured entity type names (e.g., ["Cases", "Contacts"])
        /// </summary>
        public List<string> ConfiguredEntityTypeNames { get; set; } = new();

        /// <summary>
        /// Create date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// Creator name
        /// </summary>
        public string CreateBy { get; set; } = string.Empty;

        /// <summary>
        /// Modifier name
        /// </summary>
        public string ModifyBy { get; set; } = string.Empty;

        /// <summary>
        /// Whether this integration is valid (not soft deleted)
        /// </summary>
        public bool IsValid { get; set; } = true;

        // Optional fields for details view

        /// <summary>
        /// Connection configuration (only returned in GetWithDetails)
        /// </summary>
        public ConnectionConfigDto? Connection { get; set; }

        /// <summary>
        /// Entity mappings (only returned in GetWithDetails)
        /// </summary>
        public List<EntityMappingOutputDto>? EntityMappings { get; set; }

        /// <summary>
        /// Quick links (only returned in GetWithDetails)
        /// </summary>
        public List<QuickLinkOutputDto>? QuickLinks { get; set; }

        /// <summary>
        /// Inbound field mappings (only returned in GetWithDetails)
        /// Field mappings for receiving data from external system
        /// </summary>
        public List<ActionFieldMappingDto>? InboundFieldMappings { get; set; }

        /// <summary>
        /// Outbound field mappings (only returned in GetWithDetails)
        /// Field mappings for sending data to external system
        /// </summary>
        public List<ActionFieldMappingDto>? OutboundFieldMappings { get; set; }

        /// <summary>
        /// Inbound attachments configuration (only returned in GetWithDetails)
        /// Contains: module_name, workflow_id, action_id
        /// </summary>
        public List<InboundAttachmentItemDto>? InboundAttachments { get; set; }

        /// <summary>
        /// Outbound attachments configuration (only returned in GetWithDetails)
        /// Contains: workflow_id, stage_ids
        /// </summary>
        public List<OutboundAttachmentItemDto>? OutboundAttachments { get; set; }

        /// <summary>
        /// Last days sync seconds statistics (date -> seconds)
        /// Returns last 30 days data, key format: yyyy-MM-dd
        /// </summary>
        public Dictionary<string, string>? LastDaysSeconds { get; set; }
    }
}

