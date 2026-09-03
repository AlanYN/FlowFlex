using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Per-user read status record for each What's New entry.
    /// Inherits only IdEntityBase — no audit fields, no multi-tenancy.
    /// Read status is keyed by (whats_new_id, user_id) only.
    /// </summary>
    [SugarTable("ff_whats_new_read_status")]
    public class WhatsNewReadStatus : IdEntityBase
    {
        /// <summary>
        /// Reference to the WhatsNew entry
        /// </summary>
        [SugarColumn(ColumnName = "whats_new_id")]
        public long WhatsNewId { get; set; }

        /// <summary>
        /// The user who read the entry
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// Time the entry was read
        /// </summary>
        [SugarColumn(ColumnName = "read_time")]
        public DateTimeOffset ReadTime { get; set; }
    }
}
