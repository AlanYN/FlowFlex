using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Stage Condition Save Result DTO - includes ID and any validation warnings
    /// </summary>
    public class StageConditionSaveResultDto
    {
        /// <summary>
        /// Created/Updated condition ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Whether the save was successful
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Validation warnings (non-blocking issues that user should be aware of)
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    }
}
