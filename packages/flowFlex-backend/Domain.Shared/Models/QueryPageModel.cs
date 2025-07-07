using FlowFlex.Domain.Shared.Const;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Generic pagination query model
    /// </summary>
    public class QueryPageModel
    {
        /// <summary>
        /// Current page
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
