using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.StageCondition
{
    /// <summary>
    /// Condition Validation Result
    /// </summary>
    public class ConditionValidationResult
    {
        /// <summary>
        /// Is Valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation Errors
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Validation Warnings
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    }

    /// <summary>
    /// Validation Error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Error Code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Error Message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field Name (if applicable)
        /// </summary>
        public string? Field { get; set; }
    }

    /// <summary>
    /// Validation Warning
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Warning Code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Warning Message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
