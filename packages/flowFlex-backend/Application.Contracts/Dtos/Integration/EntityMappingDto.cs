namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Entity mapping DTO
    /// </summary>
    public class EntityMappingDto
    {
        public long Id { get; set; }
        public long IntegrationId { get; set; }
        public string ExternalEntityType { get; set; }
        public string ExternalEntityName { get; set; }
        public string WfeMasterDataType { get; set; }
        public string EntityKeyField { get; set; }
        public string SyncDirection { get; set; }
        public bool EnableAutoCreate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    /// <summary>
    /// Create entity mapping DTO
    /// </summary>
    public class CreateEntityMappingDto
    {
        public long IntegrationId { get; set; }
        public string ExternalEntityType { get; set; }
        public string ExternalEntityName { get; set; }
        public string WfeMasterDataType { get; set; }
        public string EntityKeyField { get; set; }
        public string SyncDirection { get; set; }
        public bool EnableAutoCreate { get; set; }
    }

    /// <summary>
    /// Update entity mapping DTO
    /// </summary>
    public class UpdateEntityMappingDto
    {
        public string ExternalEntityName { get; set; }
        public string WfeMasterDataType { get; set; }
        public string EntityKeyField { get; set; }
        public string SyncDirection { get; set; }
        public bool EnableAutoCreate { get; set; }
    }
}
