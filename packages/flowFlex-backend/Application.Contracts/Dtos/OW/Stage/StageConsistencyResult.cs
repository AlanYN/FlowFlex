using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Stage data consistency validation result
    /// </summary>
    public class StageConsistencyResult
    {
        /// <summary>
        /// Stage ID that was validated
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Whether the stage data is consistent
        /// </summary>
        public bool IsConsistent { get; set; }

        /// <summary>
        /// Validation timestamp
        /// </summary>
        public DateTime ValidationTime { get; set; }

        /// <summary>
        /// Whether an automatic repair was attempted
        /// </summary>
        public bool RepairAttempted { get; set; }

        /// <summary>
        /// Whether the repair was successful (only meaningful if RepairAttempted is true)
        /// </summary>
        public bool? RepairSuccessful { get; set; }

        /// <summary>
        /// Error message during validation (if any)
        /// </summary>
        public string ValidationError { get; set; }

        /// <summary>
        /// Error message during repair (if any)
        /// </summary>
        public string RepairError { get; set; }

        /// <summary>
        /// Additional details about the validation/repair process
        /// </summary>
        public string Details { get; set; }
    }
}