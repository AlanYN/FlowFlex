using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// What's New update record.
    /// Global product announcement — no app_code / tenant_id scoping.
    /// app_code and tenant_id inherited from EntityBaseCreateInfo are explicitly ignored.
    /// </summary>
    [SugarTable("ff_whats_new")]
    public class WhatsNew : EntityBaseCreateInfo
    {
        // Inherited app_code / tenant_id are not stored — What's New is cross-tenant.
        [SugarColumn(IsIgnore = true)]
        public override string AppCode { get; set; }

        [SugarColumn(IsIgnore = true)]
        public override string TenantId { get; set; }

        /// <summary>
        /// Update title
        /// </summary>
        [SugarColumn(ColumnName = "title", Length = 100)]
        public string Title { get; set; }

        /// <summary>
        /// Short summary
        /// </summary>
        [SugarColumn(ColumnName = "summary", Length = 200)]
        public string Summary { get; set; }

        /// <summary>
        /// HTML body; XSS whitelist filtering applied before storage
        /// </summary>
        [SugarColumn(ColumnName = "content", ColumnDataType = "text")]
        public string Content { get; set; }

        /// <summary>
        /// Category: NewFeature / Improvement / BugFix / Announcement
        /// </summary>
        [SugarColumn(ColumnName = "category", Length = 50)]
        public string Category { get; set; }

        /// <summary>
        /// 0 = Draft, 1 = Published
        /// </summary>
        [SugarColumn(ColumnName = "status")]
        public int Status { get; set; } = 0;

        /// <summary>
        /// Actual publish time; written when Status becomes Published
        /// </summary>
        [SugarColumn(ColumnName = "publish_time", IsNullable = true)]
        public DateTimeOffset? PublishTime { get; set; }

        /// <summary>
        /// Scheduled publish time (Phase 2); reserved field
        /// </summary>
        [SugarColumn(ColumnName = "scheduled_time", IsNullable = true)]
        public DateTimeOffset? ScheduledTime { get; set; }
    }
}
