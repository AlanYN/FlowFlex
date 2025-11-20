using FlowFlex.Domain.Shared.Enums.Permission;

namespace FlowFlex.Domain.Shared.Models.Permission
{
    /// <summary>
    /// Permission result model
    /// Function: Return the result of permission verification
    /// </summary>
    public class PermissionResult
    {
        /// <summary>
        /// Whether the permission check was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether the user can view
        /// </summary>
        public bool CanView { get; set; }

        /// <summary>
        /// Whether the user can operate
        /// </summary>
        public bool CanOperate { get; set; }

        /// <summary>
        /// Permission level
        /// </summary>
        public PermissionLevelEnum PermissionLevel { get; set; }

        /// <summary>
        /// Error message (if failed)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error code (if failed)
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Grant reason (e.g., "WorkflowTeam", "Owner", "AssignedTo")
        /// </summary>
        public string GrantReason { get; set; }

        /// <summary>
        /// Create a successful result
        /// </summary>
        public static PermissionResult CreateSuccess(bool canView, bool canOperate, string grantReason)
        {
            return new PermissionResult
            {
                Success = true,
                CanView = canView,
                CanOperate = canOperate,
                PermissionLevel = canOperate ? PermissionLevelEnum.Operate :
                                 canView ? PermissionLevelEnum.ViewOnly :
                                 PermissionLevelEnum.None,
                GrantReason = grantReason
            };
        }

        /// <summary>
        /// Create a failure result
        /// </summary>
        public static PermissionResult CreateFailure(string errorMessage, string errorCode)
        {
            return new PermissionResult
            {
                Success = false,
                CanView = false,
                CanOperate = false,
                PermissionLevel = PermissionLevelEnum.None,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }

        /// <summary>
        /// Create a full control result (for Owner)
        /// </summary>
        public static PermissionResult CreateFullControl(string grantReason)
        {
            return new PermissionResult
            {
                Success = true,
                CanView = true,
                CanOperate = true,
                PermissionLevel = PermissionLevelEnum.FullControl,
                GrantReason = grantReason
            };
        }
    }
}
