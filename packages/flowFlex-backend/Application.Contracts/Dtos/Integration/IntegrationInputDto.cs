using System.ComponentModel.DataAnnotations;
using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Integration create/update input DTO
    /// </summary>
    public class IntegrationInputDto
    {
        /// <summary>
        /// Integration type (CRM, ERP, Marketing, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Integration name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Integration description
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// External system name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// External system API endpoint URL
        /// </summary>
        [Required]
        [StringLength(500)]
        public string EndpointUrl { get; set; } = string.Empty;

        /// <summary>
        /// Authentication method
        /// </summary>
        [Required]
        public AuthenticationMethod AuthMethod { get; set; }

        /// <summary>
        /// Authentication credentials (will be encrypted)
        /// </summary>
        [Required]
        public Dictionary<string, string> Credentials { get; set; } = new();

        /// <summary>
        /// Connection status
        /// </summary>
        public IntegrationStatus Status { get; set; } = IntegrationStatus.Disconnected;

        /// <summary>
        /// Whether this integration is valid (not soft deleted)
        /// </summary>
        public bool? IsValid { get; set; }
    }
}

