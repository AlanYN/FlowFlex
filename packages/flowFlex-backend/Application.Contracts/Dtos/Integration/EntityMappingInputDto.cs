using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Entity mapping create/update input DTO
    /// </summary>
    public class EntityMappingInputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// External entity display name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ExternalEntityName { get; set; } = string.Empty;

        /// <summary>
        /// External entity technical identifier
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ExternalEntityType { get; set; } = string.Empty;

        /// <summary>
        /// WFE entity type
        /// </summary>
        [Required]
        [StringLength(100)]
        public string WfeEntityType { get; set; } = string.Empty;

        /// <summary>
        /// Associated workflow IDs
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();

        /// <summary>
        /// Whether this mapping is active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}

