using System.ComponentModel.DataAnnotations;
using Domain.Shared.Enums;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Field mapping create/update input DTO
    /// </summary>
    public class FieldMappingInputDto
    {
        /// <summary>
        /// Integration ID
        /// </summary>
        [Required]
        public long IntegrationId { get; set; }

        /// <summary>
        /// Entity mapping ID
        /// </summary>
        [Required]
        public long EntityMappingId { get; set; }

        /// <summary>
        /// Action ID - associates this field mapping with a specific action (optional)
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
        /// Field data type
        /// </summary>
        [Required]
        public FieldType FieldType { get; set; }

        /// <summary>
        /// Sync direction
        /// </summary>
        [Required]
        public SyncDirection SyncDirection { get; set; }

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
        /// Default value for this field
        /// </summary>
        [StringLength(500)]
        public string? DefaultValue { get; set; }
    }
}

