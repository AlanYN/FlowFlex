using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Action
{
    /// <summary>
    /// Action execution status enumeration
    /// </summary>
    public enum ActionExecutionStatusEnum
    {
        /// <summary>
        /// Action is pending execution
        /// </summary>
        [Description("Pending")]
        Pending = 1,

        /// <summary>
        /// Action is currently running
        /// </summary>
        [Description("Running")]
        Running = 2,

        /// <summary>
        /// Action completed successfully
        /// </summary>
        [Description("Completed")]
        Completed = 3,

        /// <summary>
        /// Action failed with error
        /// </summary>
        [Description("Failed")]
        Failed = 4,

        /// <summary>
        /// Action was cancelled
        /// </summary>
        [Description("Cancelled")]
        Cancelled = 5,

        /// <summary>
        /// Action timed out
        /// </summary>
        [Description("Timeout")]
        Timeout = 6,

        /// <summary>
        /// Action is retrying after failure
        /// </summary>
        [Description("Retrying")]
        Retrying = 7
    }
} 