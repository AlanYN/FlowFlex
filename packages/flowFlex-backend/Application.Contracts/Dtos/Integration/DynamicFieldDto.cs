using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.Integration
{
    /// <summary>
    /// Dynamic field create/update input DTO
    /// </summary>
    public class DynamicFieldInputDto
    {
        /// <summary>
        /// Field ID (API Name) - unique identifier for the field
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FieldId { get; set; } = string.Empty;

        /// <summary>
        /// Field label (display name)
        /// </summary>
        [Required]
        [StringLength(200)]
        public string FieldLabel { get; set; } = string.Empty;

        /// <summary>
        /// Form property name
        /// </summary>
        [StringLength(100)]
        public string FormProp { get; set; } = string.Empty;

        /// <summary>
        /// Field category
        /// </summary>
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Field data type (0=Text, 1=Number, 2=Date, 3=Boolean, 4=Lookup)
        /// </summary>
        public int FieldType { get; set; } = 0;

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Whether this field is required
        /// </summary>
        public bool IsRequired { get; set; } = false;
    }

    /// <summary>
    /// Dynamic field output DTO
    /// </summary>
    public class DynamicFieldOutputDto
    {
        /// <summary>
        /// Field ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Field ID (API Name)
        /// </summary>
        public string FieldId { get; set; } = string.Empty;

        /// <summary>
        /// Field label (display name)
        /// </summary>
        public string FieldLabel { get; set; } = string.Empty;

        /// <summary>
        /// Form property name
        /// </summary>
        public string FormProp { get; set; } = string.Empty;

        /// <summary>
        /// Field category
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Field data type
        /// </summary>
        public int FieldType { get; set; }

        /// <summary>
        /// Sort order for display
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether this field is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Whether this is a system field (cannot be deleted)
        /// </summary>
        public bool IsSystem { get; set; }

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

