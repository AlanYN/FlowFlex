using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Paged search
    /// </summary>
    public class SearchPageDto : QueryPageModel
    {
        /// <summary>
        /// Default sorting field
        /// </summary>
        private string _orderBy = "createdate";

        /// <summary>
        /// Search text
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// Sorting field
        /// </summary>
        public virtual string OrderBy
        {
            get => _orderBy;
            set => _orderBy = string.IsNullOrWhiteSpace(value) ? "createdate" : value;
        }

        /// <summary>
        /// Indicates whether the sorting is in ascending order
        /// </summary>
        public bool IsAsc { get; set; }

        /// <summary>
        /// Indicates whether it's a CRM system. Default is false (item system)
        /// </summary>
        public bool IsPC { get; set; } = false;
    }
}
