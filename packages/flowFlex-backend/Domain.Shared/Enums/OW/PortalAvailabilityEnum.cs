namespace FlowFlex.Domain.Shared.Enums.OW
{
    /// <summary>
    /// Portal Availability Enum
    /// Defines the availability level of Workflow/Stage in customer portal
    /// </summary>
    public enum PortalAvailabilityEnum
    {
        /// <summary>
        /// Not Available - Content is not available in the customer portal
        /// </summary>
        NotAvailable = 0,

        /// <summary>
        /// Viewable only - User can view the content but cannot complete or modify it
        /// </summary>
        Viewable = 1,

        /// <summary>
        /// Completable - User can view and complete the content
        /// </summary>
        Completable = 2
    }
}

