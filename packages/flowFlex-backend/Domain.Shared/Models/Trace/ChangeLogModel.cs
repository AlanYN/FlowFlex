using Item.Internal.ChangeLog.Models;

namespace FlowFlex.Domain.Shared.Models.Trace
{
    /// <summary>
    /// Data change model
    /// </summary>
    public class ChangeLogModel : DataChangeLogModel
    {
        /// <summary>
        /// Category keyword
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Table display name
        /// </summary>
        public string TableDisplayName { get; set; }
        /// <summary>
        /// Column display name
        /// </summary>
        public string ColumnDisplayName { get; set; }
    }
}
