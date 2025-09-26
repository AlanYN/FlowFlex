using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW
{
    /// <summary>
    /// Result of stages progress validation and repair operation
    /// </summary>
    public class StagesProgressValidationResult
    {
        /// <summary>
        /// Onboarding ID that was validated
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Whether the stages progress data is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation issues found
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();

        /// <summary>
        /// Whether repair was attempted
        /// </summary>
        public bool RepairAttempted { get; set; }

        /// <summary>
        /// Whether repair was successful
        /// </summary>
        public bool RepairSuccessful { get; set; }

        /// <summary>
        /// Error message if validation or repair failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Number of stages before validation/repair
        /// </summary>
        public int StagesCountBefore { get; set; }

        /// <summary>
        /// Number of stages after validation/repair
        /// </summary>
        public int StagesCountAfter { get; set; }

        /// <summary>
        /// Number of completed stages before validation/repair
        /// </summary>
        public int CompletedStagesCountBefore { get; set; }

        /// <summary>
        /// Number of completed stages after validation/repair
        /// </summary>
        public int CompletedStagesCountAfter { get; set; }

        /// <summary>
        /// Timestamp of validation
        /// </summary>
        public DateTime ValidationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional details about the validation/repair process
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of stages progress emergency recovery operation
    /// </summary>
    public class StagesProgressRecoveryResult
    {
        /// <summary>
        /// Onboarding ID that was recovered
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Whether recovery was successful
        /// </summary>
        public bool RecoverySuccessful { get; set; }

        /// <summary>
        /// Error message if recovery failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Number of stages recovered
        /// </summary>
        public int StagesRecovered { get; set; }

        /// <summary>
        /// Number of completed stages preserved
        /// </summary>
        public int CompletedStagesPreservedCount { get; set; }

        /// <summary>
        /// Whether completed stage information was preserved
        /// </summary>
        public bool CompletedStagesPreserved { get; set; }

        /// <summary>
        /// Timestamp of recovery
        /// </summary>
        public DateTime RecoveryTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Original stages progress JSON (if available)
        /// </summary>
        public string OriginalStagesProgressJson { get; set; }

        /// <summary>
        /// Recovered stages progress JSON
        /// </summary>
        public string RecoveredStagesProgressJson { get; set; }

        /// <summary>
        /// Additional details about the recovery process
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }
}
