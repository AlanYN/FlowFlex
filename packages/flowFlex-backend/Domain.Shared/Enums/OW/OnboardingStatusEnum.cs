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
        /// Paused - Onboarding process temporarily paused
        /// </summary>
        [Description("Paused")]
        Paused = 4,

        /// <summary>
        /// Aborted - Onboarding process terminated/cancelled
        /// </summary>
        [Description("Aborted")]
        Aborted = 5
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