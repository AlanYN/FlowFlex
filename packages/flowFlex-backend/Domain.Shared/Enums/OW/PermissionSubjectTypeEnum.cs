namespace FlowFlex.Domain.Shared.Enums.OW
{
    /// <summary>
    /// Permission Subject Type Enum
    /// Defines whether permissions are based on Teams or Individual Users
    /// </summary>
    public enum PermissionSubjectTypeEnum
    {
        /// <summary>
        /// Team-based permissions (default) - Permission subjects are team names
        /// </summary>
        Team = 1,

        /// <summary>
        /// User-based permissions - Permission subjects are individual user IDs
        /// </summary>
        User = 2
    }
}

