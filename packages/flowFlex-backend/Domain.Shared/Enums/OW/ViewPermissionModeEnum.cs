namespace FlowFlex.Domain.Shared.Enums.OW
{
    /// <summary>
    /// View Permission Mode Enum
    /// Defines how view permissions are controlled for Workflow/Stage
    /// </summary>
    public enum ViewPermissionModeEnum
    {
        /// <summary>
        /// Public - All users can view
        /// </summary>
        Public = 0,

        /// <summary>
        /// Visible to specific teams - Only listed teams can view
        /// </summary>
        VisibleToTeams = 1,

        /// <summary>
        /// Invisible to specific teams - All teams except listed teams can view
        /// </summary>
        InvisibleToTeams = 2
    }
}

