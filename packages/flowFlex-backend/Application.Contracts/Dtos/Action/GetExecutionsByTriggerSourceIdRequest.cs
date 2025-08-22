using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// Request DTO for getting executions by trigger source ID with JSON conditions
    /// </summary>
    public class GetExecutionsByTriggerSourceIdRequest : QueryPageModel
    {
        /// <summary>
        /// JSON query conditions for filtering
        /// </summary>
        public List<JsonQueryCondition>? JsonConditions { get; set; }
    }
}
