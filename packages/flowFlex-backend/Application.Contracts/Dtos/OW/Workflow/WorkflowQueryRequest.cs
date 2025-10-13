using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow query request DTO
    /// </summary>
    public class WorkflowQueryRequest
    {
        /// <summary>
        /// Page number (starting from 1)
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; } = 15;

        /// <summary>
        /// Filter by workflow name
        /// </summary>
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filter by default status
        /// </summary>
        public bool? IsDefault { get; set; }

        /// <summary>
        /// Sort field
        /// </summary>
        [StringLength(50)]
        public string SortField { get; set; } = "CreateDate";

        /// <summary>
        /// Sort direction (asc/desc)
        /// </summary>
        [StringLength(10)]
        public string SortDirection { get; set; } = "desc";
    }
}