namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Stage query request DTO
    /// </summary>
    public class StageQueryRequest
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
        /// Filter by workflow id
        /// </summary>
        public long? WorkflowId { get; set; }

        /// <summary>
        /// Filter by stage name
        /// </summary>
        public string Name { get; set; }
    }
}