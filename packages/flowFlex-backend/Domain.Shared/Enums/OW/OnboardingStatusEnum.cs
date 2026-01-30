using System;
using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.OW
{
    /// <summary>
    /// Onboarding Status Enumeration
    /// Defines all possible states of an onboarding process
    /// </summary>
    public enum OnboardingStatusEnum
    {
        /// <summary>
        /// Inactive - Initial state, onboarding not yet started
        /// </summary>
        [Description("Inactive")]
        Inactive = 0,

        /// <summary>
        /// Active - Onboarding process is active and in progress
        /// </summary>
        [Description("Active")]
        Active = 1,

        /// <summary>
        /// Completed - Onboarding process completed successfully through normal flow
        /// </summary>
        [Description("Completed")]
        Completed = 2,

        /// <summary>
        /// InProgress - Onboarding process is currently being worked on
        /// </summary>
        [Description("In Progress")]
        InProgress = 3,

        /// <summary>
        /// Paused - Onboarding process temporarily paused
        /// </summary>
        [Description("Paused")]
        Paused = 4,

        /// <summary>
        /// Aborted - Onboarding process terminated/cancelled
        /// </summary>
        [Description("Aborted")]
        Aborted = 5,

        /// <summary>
        /// Cancelled - Onboarding process cancelled by user
        /// </summary>
        [Description("Cancelled")]
        Cancelled = 6,

        /// <summary>
        /// Rejected - Onboarding process rejected
        /// </summary>
        [Description("Rejected")]
        Rejected = 7,

        /// <summary>
        /// Terminated - Onboarding process terminated
        /// </summary>
        [Description("Terminated")]
        Terminated = 8,

        /// <summary>
        /// ForceCompleted - Onboarding process force completed by admin
        /// </summary>
        [Description("Force Completed")]
        ForceCompleted = 9
    }

    /// <summary>
    /// Extension methods for OnboardingStatusEnum
    /// </summary>
    public static class OnboardingStatusEnumExtensions
    {
        /// <summary>
        /// Convert enum to database string value
        /// </summary>
        /// <param name="status">The status enum value</param>
        /// <returns>Database-compatible string representation</returns>
        public static string ToDbString(this OnboardingStatusEnum status)
        {
            return status switch
            {
                OnboardingStatusEnum.Inactive => "Inactive",
                OnboardingStatusEnum.Active => "Active",
                OnboardingStatusEnum.Completed => "Completed",
                OnboardingStatusEnum.InProgress => "In Progress",
                OnboardingStatusEnum.Paused => "Paused",
                OnboardingStatusEnum.Aborted => "Aborted",
                OnboardingStatusEnum.Cancelled => "Cancelled",
                OnboardingStatusEnum.Rejected => "Rejected",
                OnboardingStatusEnum.Terminated => "Terminated",
                OnboardingStatusEnum.ForceCompleted => "Force Completed",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status value")
            };
        }

        /// <summary>
        /// Parse string to OnboardingStatusEnum
        /// </summary>
        /// <param name="status">String status value</param>
        /// <returns>Corresponding enum value</returns>
        /// <exception cref="ArgumentException">Thrown when status string is not recognized</exception>
        public static OnboardingStatusEnum ParseStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status cannot be null or empty", nameof(status));
            }

            return status.Trim().ToLowerInvariant() switch
            {
                "inactive" => OnboardingStatusEnum.Inactive,
                "active" => OnboardingStatusEnum.Active,
                "completed" => OnboardingStatusEnum.Completed,
                "in progress" => OnboardingStatusEnum.InProgress,
                "inprogress" => OnboardingStatusEnum.InProgress,
                "paused" => OnboardingStatusEnum.Paused,
                "aborted" => OnboardingStatusEnum.Aborted,
                "cancelled" => OnboardingStatusEnum.Cancelled,
                "canceled" => OnboardingStatusEnum.Cancelled,
                "rejected" => OnboardingStatusEnum.Rejected,
                "terminated" => OnboardingStatusEnum.Terminated,
                "force completed" => OnboardingStatusEnum.ForceCompleted,
                "forcecompleted" => OnboardingStatusEnum.ForceCompleted,
                _ => throw new ArgumentException($"Unknown status: {status}", nameof(status))
            };
        }

        /// <summary>
        /// Try to parse string to OnboardingStatusEnum
        /// </summary>
        /// <param name="status">String status value</param>
        /// <param name="result">Parsed enum value if successful</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseStatus(string status, out OnboardingStatusEnum result)
        {
            result = OnboardingStatusEnum.Inactive;
            
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            try
            {
                result = ParseStatus(status);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the status represents a terminal state (cannot transition further)
        /// </summary>
        /// <param name="status">The status to check</param>
        /// <returns>True if terminal state</returns>
        public static bool IsTerminalState(this OnboardingStatusEnum status)
        {
            return status switch
            {
                OnboardingStatusEnum.Completed => true,
                OnboardingStatusEnum.Aborted => true,
                OnboardingStatusEnum.Cancelled => true,
                OnboardingStatusEnum.Rejected => true,
                OnboardingStatusEnum.Terminated => true,
                OnboardingStatusEnum.ForceCompleted => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if the status allows operations (not in terminal or paused state)
        /// </summary>
        /// <param name="status">The status to check</param>
        /// <returns>True if operations are allowed</returns>
        public static bool AllowsOperations(this OnboardingStatusEnum status)
        {
            return status switch
            {
                OnboardingStatusEnum.Active => true,
                OnboardingStatusEnum.InProgress => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// Onboarding Action Enumeration
    /// Defines all possible actions that can be performed on an onboarding
    /// </summary>
    public enum OnboardingActionEnum
    {
        /// <summary>
        /// Start Onboarding - Activate an inactive onboarding
        /// </summary>
        [Description("Start Onboarding")]
        StartOnboarding = 1,

        /// <summary>
        /// Proceed - Enter onboarding details page
        /// </summary>
        [Description("Proceed")]
        Proceed = 2,


        /// <summary>
        /// Pause - Pause the onboarding process
        /// </summary>
        [Description("Pause")]
        Pause = 4,

        /// <summary>
        /// Resume - Resume a paused onboarding
        /// </summary>
        [Description("Resume")]
        Resume = 5,

        /// <summary>
        /// Abort - Abort/terminate the onboarding process
        /// </summary>
        [Description("Abort")]
        Abort = 6,

        /// <summary>
        /// Reactivate - Reactivate an aborted onboarding
        /// </summary>
        [Description("Reactivate")]
        Reactivate = 7,

        /// <summary>
        /// Edit - Edit onboarding details
        /// </summary>
        [Description("Edit")]
        Edit = 8,

        /// <summary>
        /// View - View onboarding in read-only mode
        /// </summary>
        [Description("View")]
        View = 9,

        /// <summary>
        /// Complete - Complete current stage
        /// </summary>
        [Description("Complete")]
        Complete = 10
    }
}