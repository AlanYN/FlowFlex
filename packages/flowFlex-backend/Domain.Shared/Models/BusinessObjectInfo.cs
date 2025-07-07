using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.Item;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Business object information model, used to transfer basic business object information between different business modules
    /// </summary>
    public class BusinessObjectInfo
    {
        /// <summary>
        /// Business object ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Business object type
        /// </summary>
        public RelationalTypeEnum BusinessType { get; set; }

        /// <summary>
        /// Business object name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Business object assigned user ID
        /// </summary>
        public long AssignedTo { get; set; }

        /// <summary>
        /// Business object detail page URL
        /// </summary>
        public string DetailUrl { get; set; }
    }
}
