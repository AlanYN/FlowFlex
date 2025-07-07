namespace FlowFlex.Domain.Shared.Models
{
    public class RoleUserModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Phone
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        public long RoleId { get; set; }

        /// <summary>
        /// User role relationship ID
        /// </summary>
        public long UserRoleId { get; set; }
    }
    /// <summary>
    /// Temporary role group class
    /// </summary>
    public class RoleGroupModel
    {
        /// <summary>
        /// Role ID
        /// </summary>
        public string RoleId { get; set; }
        /// <summary>
        /// Role name
        /// </summary>
        public string RoleName { get; set; }
    }
}
