using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Message Attachment Entity
    /// </summary>
    [SugarTable("ff_message_attachments")]
    public class MessageAttachment : OwEntityBase
    {
        /// <summary>
        /// Message ID
        /// </summary>
        [SugarColumn(ColumnName = "message_id")]
        public long MessageId { get; set; }

        /// <summary>
        /// File Name
        /// </summary>
        [StringLength(255)]
        [SugarColumn(ColumnName = "file_name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File Size in bytes
        /// </summary>
        [SugarColumn(ColumnName = "file_size")]
        public long FileSize { get; set; }

        /// <summary>
        /// Content Type (MIME type)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "content_type")]
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Storage Path (blob storage path)
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "storage_path")]
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// External Attachment ID (for Outlook integration)
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "external_attachment_id")]
        public string? ExternalAttachmentId { get; set; }

        /// <summary>
        /// Content ID (for inline images in HTML body)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "content_id")]
        public string? ContentId { get; set; }

        /// <summary>
        /// Is Inline (embedded in message body)
        /// </summary>
        [SugarColumn(ColumnName = "is_inline")]
        public bool IsInline { get; set; } = false;

        // Navigation Property
        [SugarColumn(IsIgnore = true)]
        public virtual Message? Message { get; set; }
    }
}
