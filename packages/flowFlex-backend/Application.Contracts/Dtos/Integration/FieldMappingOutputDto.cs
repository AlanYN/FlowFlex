using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Field mapping output DTO
    /// </summary>
    public class FieldMappingOutputDto
    {
        /// <summary>
        /// Field mapping ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Integration ID
        /// </summary>
        public long IntegrationId { get; set; }

        /// <summary>
        /// Entity mapping ID
        /// </summary>
        public long EntityMappingId { get; set; }

        /// <summary>
        /// External system field name
        /// </summary>
        public string ExternalFieldName { get; set; } = string.Empty;

        /// <summary>
        /// WFE field ID
        /// </summary>
        public string WfeFieldId { get; set; } = string.Empty;

        /// <summary>
        /// Field data type
        /// </summary>
        public FieldType FieldType { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        public SyncDirection SyncDirection { get; set; }

        /// <summary>
        /// Associated workflow IDs
        /// </summary>
        public List<long> WorkflowIds { get; set; } = new();

        /// <summary>
        /// Transformation rules
        /// </summary>
        public Dictionary<string, object> TransformRules { get; set; } = new();

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether this field is required
        /// </summary>
        public bool IsRequired { get; set; }

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

