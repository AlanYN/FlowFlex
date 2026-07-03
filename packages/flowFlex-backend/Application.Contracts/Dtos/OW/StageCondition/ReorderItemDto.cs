using System.Collections.Generic;
using FlowFlex.Domain.Shared.JsonConverters;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Item for reordering conditions
    /// </summary>
    public class ReorderItemDto
    {
        /// <summary>
        /// Condition ID
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// New order value
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Request for reordering conditions
    /// </summary>
    public class ReorderConditionsRequest
    {
        /// <summary>
        /// List of condition ID + order pairs
        /// </summary>
        public List<ReorderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Request for updating stage condition fallback
    /// </summary>
    public class UpdateConditionFallbackRequest
    {
        /// <summary>
        /// Fallback Stage ID. NULL means "Continue to next stage".
        /// </summary>
        [JsonConverter(typeof(LongToStringConverter))]
        public long? FallbackStageId { get; set; }
    }
}
