using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// User tour seen record — tracks which guided tours a user has completed.
    /// One row per (user_id, tour_key, app_code, tenant_id).
    /// </summary>
    [SugarTable("ff_user_tour_records")]
    public class UserTourRecord : OwEntityBase
    {
        /// <summary>
        /// The user who saw the tour.
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// Unique tour identifier — matches the frontend persistKey, e.g.
        /// "workflow-list-tour", "workflow-detail-tour",
        /// "workflow-condition-tour-{workflowId}", "workflow-stage-form-tour".
        /// </summary>
        [SugarColumn(ColumnName = "tour_key", Length = 200)]
        public string TourKey { get; set; }

        /// <summary>
        /// Timestamp when the tour was first completed / skipped.
        /// </summary>
        [SugarColumn(ColumnName = "seen_at")]
        public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
