namespace FlowFlex.Domain.Shared.Models
{
    public class UserModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// Default tenant ID
        /// </summary>
        public string TenantId { get; set; }
    }
}
