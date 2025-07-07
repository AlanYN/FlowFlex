using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// User event log type (for recording all event logs of user data)
    /// </summary>
    public enum UserEventRecordLogTypeEnum
    {
        /// <summary>
        /// Create user
        /// </summary>
        [Description("User Creation")]
        Create = 1,

        /// <summary>
        /// Modify user
        /// </summary>
        [Description("User Information Modification")]
        Update = 2,

        /// <summary>
        /// Delete user
        /// </summary>
        [Description("User Deletion ")]
        Delete = 3,

        /// <summary>
        /// Role change
        /// </summary>
        [Description("Role Change")]
        RoleChange = 4,

        /// <summary>
        /// Login
        /// </summary>
        [Description("Login Record")]
        Login = 5
    }
}
