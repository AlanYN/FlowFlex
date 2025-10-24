namespace FlowFlex.Application.Contracts.Dtos.OW.Permission
{
    /// <summary>
    /// Permission information DTO
    /// Indicates user's permission for a specific resource
    /// </summary>
    public class PermissionInfoDto
    {
        /// <summary>
        /// Whether user can view this resource
        /// </summary>
        public bool CanView { get; set; }

        /// <summary>
        /// Whether user can operate (edit/delete) this resource
        /// </summary>
        public bool CanOperate { get; set; }

        /// <summary>
        /// Error message if permission check failed
        /// Null if user has at least view permission
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}

