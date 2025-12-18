using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Message Entity - Unified message storage for Internal, Email, and Portal messages
    /// </summary>
    [SugarTable("ff_messages")]
    public class Message : OwEntityBase
    {
        /// <summary>
        /// Message Subject
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Message Body (HTML or plain text)
        /// </summary>
        [SugarColumn(ColumnName = "body", ColumnDataType = "text")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Body Preview (first 200 characters)
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "body_preview")]
        public string BodyPreview { get; set; } = string.Empty;

        /// <summary>
        /// Message Type: Internal, Email, Portal
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "message_type")]
        public string MessageType { get; set; } = "Internal";

        /// <summary>
        /// Folder: Inbox, Sent, Starred, Archive, Trash
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "folder")]
        public string Folder { get; set; } = "Inbox";

        /// <summary>
        /// Labels (JSON array): Internal, External, Important, Portal
        /// Note: IsJson removed to prevent double serialization - Labels is already a JSON string
        /// </summary>
        [SugarColumn(ColumnName = "labels", ColumnDataType = "jsonb")]
        public string Labels { get; set; } = "[]";

        /// <summary>
        /// Sender User ID
        /// </summary>
        [SugarColumn(ColumnName = "sender_id")]
        public long? SenderId { get; set; }

        /// <summary>
        /// Sender Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "sender_name")]
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// Sender Email Address
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "sender_email")]
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Recipients (JSON array of RecipientDto)
        /// </summary>
        [SugarColumn(ColumnName = "recipients", ColumnDataType = "jsonb", IsJson = true)]
        public string Recipients { get; set; } = "[]";

        /// <summary>
        /// CC Recipients (JSON array, for Email type)
        /// </summary>
        [SugarColumn(ColumnName = "cc_recipients", ColumnDataType = "jsonb", IsJson = true)]
        public string CcRecipients { get; set; } = "[]";

        /// <summary>
        /// BCC Recipients (JSON array, for Email type)
        /// </summary>
        [SugarColumn(ColumnName = "bcc_recipients", ColumnDataType = "jsonb", IsJson = true)]
        public string BccRecipients { get; set; } = "[]";

        /// <summary>
        /// Related Entity Type: Onboarding, Case
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "related_entity_type")]
        public string? RelatedEntityType { get; set; }

        /// <summary>
        /// Related Entity ID
        /// </summary>
        [SugarColumn(ColumnName = "related_entity_id")]
        public long? RelatedEntityId { get; set; }

        /// <summary>
        /// Related Entity Code (e.g., LEAD-001)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "related_entity_code")]
        public string? RelatedEntityCode { get; set; }

        /// <summary>
        /// Is Read
        /// </summary>
        [SugarColumn(ColumnName = "is_read")]
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Is Starred
        /// </summary>
        [SugarColumn(ColumnName = "is_starred")]
        public bool IsStarred { get; set; } = false;

        /// <summary>
        /// Is Archived
        /// </summary>
        [SugarColumn(ColumnName = "is_archived")]
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Is Draft
        /// </summary>
        [SugarColumn(ColumnName = "is_draft")]
        public bool IsDraft { get; set; } = false;

        /// <summary>
        /// Has Attachments
        /// </summary>
        [SugarColumn(ColumnName = "has_attachments")]
        public bool HasAttachments { get; set; } = false;

        /// <summary>
        /// Sent Date
        /// </summary>
        [SugarColumn(ColumnName = "sent_date")]
        public DateTimeOffset? SentDate { get; set; }

        /// <summary>
        /// Received Date
        /// </summary>
        [SugarColumn(ColumnName = "received_date")]
        public DateTimeOffset? ReceivedDate { get; set; }

        /// <summary>
        /// Parent Message ID (for reply/forward thread)
        /// </summary>
        [SugarColumn(ColumnName = "parent_message_id")]
        public long? ParentMessageId { get; set; }

        /// <summary>
        /// Conversation ID (for grouping related messages)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "conversation_id")]
        public string? ConversationId { get; set; }

        /// <summary>
        /// Portal ID (for Portal messages)
        /// </summary>
        [SugarColumn(ColumnName = "portal_id")]
        public long? PortalId { get; set; }

        /// <summary>
        /// External Message ID (for Outlook integration)
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "external_message_id")]
        public string? ExternalMessageId { get; set; }

        /// <summary>
        /// Owner User ID (the user who owns this message in their mailbox)
        /// </summary>
        [SugarColumn(ColumnName = "owner_id")]
        public long OwnerId { get; set; }

        /// <summary>
        /// Original Folder (used for restore from Trash)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "original_folder")]
        public string? OriginalFolder { get; set; }
    }
}
