using System.ComponentModel.DataAnnotations;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Internal Note Entity
    /// </summary>
    [SugarTable("ff_internal_notes")]
    public class InternalNote : OwEntityBase
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>

        [SugarColumn(ColumnName = "onboarding_id")]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID (optional, if empty then it's a note for the entire Onboarding)
        /// </summary>
        [SugarColumn(ColumnName = "stage_id")]
        public long? StageId { get; set; }

        /// <summary>
        /// Note Title (optional)
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "title")]
        public string? Title { get; set; } = string.Empty;

        /// <summary>
        /// Note Content
        /// </summary>

        [StringLength(4000)]
        [SugarColumn(ColumnName = "content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Note Type (General, Important, Warning, Question, etc.)
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "note_type")]
        public string NoteType { get; set; } = "General";

        /// <summary>
        /// Priority (Low, Normal, High, Critical)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "priority")]
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Is Resolved
        /// </summary>
        [SugarColumn(ColumnName = "is_resolved")]
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// Resolution Time
        /// </summary>
        [SugarColumn(ColumnName = "resolved_time")]
        public DateTimeOffset? ResolvedTime { get; set; }

        /// <summary>
        /// Resolver ID
        /// </summary>
        [SugarColumn(ColumnName = "resolved_by_id")]
        public long? ResolvedById { get; set; }

        /// <summary>
        /// Resolver Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "resolved_by")]
        public string ResolvedBy { get; set; } = string.Empty;

        /// <summary>
        /// Resolution Notes
        /// </summary>
        [StringLength(1000)]
        [SugarColumn(ColumnName = "resolution_notes")]
        public string ResolutionNotes { get; set; } = string.Empty;

        /// <summary>
        /// Tags (JSON array format, e.g. ["urgent", "customer-request"])
        /// </summary>
        [SugarColumn(ColumnName = "tags", ColumnDataType = "jsonb", IsJson = true)]
        public string Tags { get; set; } = "[]";

        /// <summary>
        /// Visibility (Public, Internal, Private)
        /// </summary>
        [StringLength(20)]
        [SugarColumn(ColumnName = "visibility")]
        public string Visibility { get; set; } = "Internal";

        /// <summary>
        /// Mentioned User IDs List (JSON array format, mentioned users)
        /// </summary>
        [SugarColumn(ColumnName = "mentioned_user_ids", ColumnDataType = "jsonb", IsJson = true)]
        public string MentionedUserIds { get; set; } = "[]";

        /// <summary>
        /// Author Name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Author ID
        /// </summary>
        [SugarColumn(ColumnName = "author_id")]
        public long? AuthorId { get; set; }

        /// <summary>
        /// Parent Note ID (for reply functionality)
        /// </summary>
        [SugarColumn(ColumnName = "parent_note_id")]
        public long? ParentNoteId { get; set; }

        /// <summary>
        /// Data Source
        /// </summary>
        [StringLength(50)]
        [SugarColumn(ColumnName = "source")]
        public string Source { get; set; } = "customer_portal";

        // Navigation Properties
        [SugarColumn(IsIgnore = true)]
        public virtual Onboarding? Onboarding { get; set; }

        [SugarColumn(IsIgnore = true)]
        public virtual Stage? Stage { get; set; }
    }
}
