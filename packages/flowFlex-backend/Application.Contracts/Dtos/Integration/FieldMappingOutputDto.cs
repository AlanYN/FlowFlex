namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Inbound field mapping output DTO
    /// </summary>
    public class InboundFieldMappingOutputDto
    {
        /// <summary>
        /// Field mapping ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Action ID
        /// </summary>
        public long ActionId { get; set; }

        /// <summary>
        /// External system field name
        /// </summary>
        public string ExternalFieldName { get; set; } = string.Empty;

        /// <summary>
        /// WFE field ID
        /// </summary>
        public string WfeFieldId { get; set; } = string.Empty;

        /// <summary>
        /// WFE field display name
        /// </summary>
        public string WfeFieldName { get; set; } = string.Empty;

        /// <summary>
        /// Field data type (Text, Number, Date, Boolean, Lookup)
        /// </summary>
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// Sync direction (ViewOnly, Editable, OutboundOnly)
        /// </summary>
        public string SyncDirection { get; set; } = string.Empty;

        /// <summary>
        /// Whether this field is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Default value for this field
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Backward compatibility alias
    /// </summary>
    [Obsolete("Use InboundFieldMappingOutputDto instead")]
    public class FieldMappingOutputDto : InboundFieldMappingOutputDto { }
}
