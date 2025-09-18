namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Portal Permission Enum
    /// Defines the level of access a user has in the customer portal for a specific stage or component
    /// </summary>
    public enum PortalPermissionEnum
    {
        /// <summary>
        /// Hidden - Content is not visible in the customer portal
        /// </summary>
        Hidden = 0,

        /// <summary>
        /// Viewable only - User can view the stage content but cannot complete or modify it
        /// </summary>
        Viewable = 1,

        /// <summary>
        /// Completable - User can view and complete the stage
        /// </summary>
        Completable = 2
    }
}