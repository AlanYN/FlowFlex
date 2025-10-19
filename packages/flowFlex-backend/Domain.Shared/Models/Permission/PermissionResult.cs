using FlowFlex.Domain.Shared.Enums.Permission;

namespace FlowFlex.Domain.Shared.Models.Permission
{
    /// <summary>
    /// Permission result
    /// </summary>
    public class PermissionResult
    {
        /// <summary>
        /// Whether successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether can view
        /// </summary>
        public bool CanView { get; set; }

        /// <summary>
        /// Whether can operate
        /// </summary>
        public bool CanOperate { get; set; }

        /// <summary>
        /// Permission level
        /// </summary>
        public PermissionLevelEnum PermissionLevel { get; set; }

        /// <summary>
        /// Grant reason (Owner, AssignedTo, Team, Direct)
        /// </summary>
        public string GrantReason { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Create a success result
        /// </summary>
        public static PermissionResult CreateSuccess(PermissionLevelEnum level, string reason = null)
        {
            return new PermissionResult
            {
                Success = true,
                CanView = level >= PermissionLevelEnum.ViewOnly,
                CanOperate = level >= PermissionLevelEnum.Operate,
                PermissionLevel = level,
                GrantReason = reason
            };
        }

        /// <summary>
        /// Create a failure result
        /// </summary>
        public static PermissionResult CreateFailure(string errorMessage, string errorCode = null)
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
    }
}

