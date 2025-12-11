using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Entity mapping batch save input DTO
    /// Used for batch create/update/delete operations
    /// Items not in the list will be automatically deleted
    /// </summary>
    public class EntityMappingBatchSaveDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// Items to save (create or update)
        /// Items with ID will be updated, items without ID will be created
        /// Existing items not in this list will be automatically deleted
        /// </summary>
        public List<EntityMappingBatchItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Entity mapping batch item DTO
    /// </summary>
    public class EntityMappingBatchItemDto
    {
        /// <summary>
        /// Entity mapping ID (null or 0 for new items)
        /// </summary>
        public long? Id { get; set; }

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

    /// <summary>
    /// Entity mapping batch save result DTO
    /// </summary>
    public class EntityMappingBatchSaveResultDto
    {
        /// <summary>
        /// Number of items created
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// Number of items updated
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Number of items deleted
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// Saved items
        /// </summary>
        public List<EntityMappingOutputDto> Items { get; set; } = new();
    }
}

