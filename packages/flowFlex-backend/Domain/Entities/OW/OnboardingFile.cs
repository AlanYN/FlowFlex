using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Onboarding File Entity - Stores file information related to Onboarding and Stage
    /// </summary>
    [SugarTable("ff_onboarding_file")]
    public class OnboardingFile : EntityBaseCreateInfo
    {
        /// <summary>
        /// Onboarding Primary Key ID
        /// </summary>
       
        [SugarColumn(ColumnName = "onboarding_id")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage Primary Key ID (Optional, if specified then associated with specific Stage)
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long? StageId { get; set; }

        /// <summary>
        /// Associated Attachment ID (from attachment table)
        /// </summary>
       
        [SugarColumn(ColumnName = "attachment_id")]
        public long AttachmentId { get; set; }

        /// <summary>
        /// Original File Name
        /// </summary>
       
        [StringLength(255)]
        [SugarColumn(ColumnName = "original_file_name")]
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Stored File Name
        /// </summary>
       
        [StringLength(255)]
        [SugarColumn(ColumnName = "stored_file_name")]
        public string StoredFileName { get; set; }

        /// <summary>
        /// File Extension
        /// </summary>
        [StringLength(10)]
        [SugarColumn(ColumnName = "file_extension")]
        public string FileExtension { get; set; }

        /// <summary>
        /// File Size (bytes)
        /// </summary>
        [SugarColumn(ColumnName = "file_size")]
        public long FileSize { get; set; }

        /// <summary>
        /// File Type (MIME type)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "content_type")]
        public string ContentType { get; set; }

        /// <summary>
        /// File Category (Document, Image, Certificate, Other)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "category")]
        public string Category { get; set; } = "Document";

        /// <summary>
        /// File Description
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Is Required File
        /// </summary>
        [SugarColumn(ColumnName = "is_required")]
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// File Tags (for grouping or searching, stored in JSON format)
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "tags")]
        public string Tags { get; set; }

        /// <summary>
        /// File Access Path
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "access_url")]
        public string AccessUrl { get; set; }

        /// <summary>
        /// File Storage Path
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "storage_path")]
        public string StoragePath { get; set; }

        /// <summary>
        /// Uploaded By ID
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "uploaded_by_id")]
        public string UploadedById { get; set; }

        /// <summary>
        /// Uploaded By Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "uploaded_by_name")]
        public string UploadedByName { get; set; }

        /// <summary>
        /// Uploaded Date
        /// </summary>
        [SugarColumn(ColumnName = "uploaded_date")]
        public DateTimeOffset UploadedDate { get; set; }

        /// <summary>
        /// Last Modified Date
        /// </summary>
        [SugarColumn(ColumnName = "last_modified_date")]
        public DateTimeOffset? LastModifiedDate { get; set; }

        /// <summary>
        /// File Status (Active, Deleted, Archived)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// File Version (for version control)
        /// </summary>
        [SugarColumn(ColumnName = "version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// File Hash (for duplicate detection)
        /// </summary>
        [StringLength(64)]
        [SugarColumn(ColumnName = "file_hash")]
        public string FileHash { get; set; }

        /// <summary>
        /// Sort Order
        /// </summary>
        [SugarColumn(ColumnName = "sort_order")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Extended Properties (JSON format to store additional information)
        /// </summary>
        [SugarColumn(ColumnName = "extended_properties")]
        public string ExtendedProperties { get; set; }

        // Navigation Properties (SqlSugar doesn't need configuration, only for code logic)

        /// <summary>
        /// Associated Onboarding Entity
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public virtual Onboarding Onboarding { get; set; }

        /// <summary>
        /// Associated Stage Entity
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public virtual Stage Stage { get; set; }
    }
}
