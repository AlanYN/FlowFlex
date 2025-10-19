namespace FlowFlex.Domain.Shared.Enums.Permission
{
    /// <summary>
    /// Permission level enumeration
    /// </summary>
    public enum PermissionLevelEnum
    {
        /// <summary>
        /// No access permission
        /// </summary>
        None = 0,

        /// <summary>
        /// View only
        /// </summary>
        ViewOnly = 1,

        /// <summary>
        /// View and operate
        /// </summary>
        Operate = 2,

        /// <summary>
        /// Full control (Owner)
        /// </summary>
        FullControl = 3
    }
}

