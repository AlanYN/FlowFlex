using System.ComponentModel.DataAnnotations;
using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Inbound field mapping create/update input DTO
    /// </summary>
    public class InboundFieldMappingInputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// Action ID - associates this field mapping with a specific action
        /// </summary>
        public long? ActionId { get; set; }

        /// <summary>
        /// External system field name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ExternalFieldName { get; set; } = string.Empty;

        /// <summary>
        /// WFE field ID
        /// </summary>
        [Required]
        [StringLength(100)]
        public string WfeFieldId { get; set; } = string.Empty;

        /// <summary>
        /// Field data type (Text, Number, Date, Boolean, Lookup)
        /// </summary>
        [Required]
        public FieldType FieldType { get; set; }

        /// <summary>
        /// Sync direction (ViewOnly, Editable, OutboundOnly)
        /// </summary>
        [Required]
        public SyncDirection SyncDirection { get; set; }

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether this field is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Default value for this field
        /// </summary>
        [StringLength(500)]
        public string? DefaultValue { get; set; }
    }

    /// <summary>
    /// Backward compatibility alias
    /// </summary>
    [Obsolete("Use InboundFieldMappingInputDto instead")]
    public class FieldMappingInputDto : InboundFieldMappingInputDto { }
}

