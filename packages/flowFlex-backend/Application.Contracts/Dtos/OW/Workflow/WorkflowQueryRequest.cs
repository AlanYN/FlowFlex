namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow query request DTO
    /// </summary>
    public class WorkflowQueryRequest
    {
        /// <summary>
        /// Page number
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Filter by workflow name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }
    }
}