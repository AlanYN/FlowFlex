namespace FlowFlex.Domain.Shared.Models
{
    public class UserRoleModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        public long RoleId { get; set; }

        /// <summary>
        /// Role name
        /// </summary>
        public string RoleName { get; set; }
    }
}
