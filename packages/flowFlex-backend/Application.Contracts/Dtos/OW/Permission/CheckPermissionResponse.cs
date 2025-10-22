namespace FlowFlex.Application.Contracts.Dtos.OW.Permission
{
    /// <summary>
    /// Check permission response
    /// </summary>
    public class CheckPermissionResponse
    {
        /// <summary>
        /// Can the user view this resource
        /// </summary>
        public bool CanView { get; set; }

        /// <summary>
        /// Can the user operate this resource
        /// </summary>
        public bool CanOperate { get; set; }

        /// <summary>
        /// Permission grant reason (for debugging)
        /// </summary>
        public string GrantReason { get; set; }

        /// <summary>
        /// Error message (if permission denied)
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}

