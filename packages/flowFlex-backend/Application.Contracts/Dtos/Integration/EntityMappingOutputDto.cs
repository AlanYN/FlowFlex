namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Entity mapping output DTO
    /// </summary>
    public class EntityMappingOutputDto
    {
        /// <summary>
        /// Entity mapping ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// External system ID (unique identifier in external system)
        /// </summary>
        public string? SystemId { get; set; }

        /// <summary>
        /// External entity display name
        /// </summary>
        public string ExternalEntityName { get; set; } = string.Empty;

        /// <summary>
        /// External entity technical identifier
        /// </summary>
        public string ExternalEntityType { get; set; } = string.Empty;

        /// <summary>
        /// WFE entity type
        /// </summary>
        public string WfeEntityType { get; set; } = string.Empty;

        /// <summary>
        /// Associated workflow IDs
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();

        /// <summary>
        /// Whether this mapping is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Create date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }
    }
}

