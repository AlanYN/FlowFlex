using Domain.Shared.Enums;

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
    }
}

