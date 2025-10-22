using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.Item;
using FlowFlex.Domain.Shared.Const;
using System.Linq;

namespace FlowFlex.Domain.Shared.Models
{
    public class UserInfoModel : BaseUserInfoModel
    {
        /// <summary>
        /// Company - very important for item
        /// </summary>
        public string CompanyId { get; set; }

        /// <summary>
        /// Default tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// User optional tenant list
        /// </summary>
        public Dictionary<string, string> Tenants { get; set; } = [];

        /// <summary>
        /// Role ID list
        /// </summary>
        public IList<string> RoleIds { get; set; } = [];

        public string ClientShortName { get; set; } = "";

        /// <summary>
        /// Distinguish system source Unis, itemApp, itemWeb
        /// </summary>
        public SourceEnum SystemSource { get; set; } = SourceEnum.None;

        /// <summary>
        /// User default time zone
        /// </summary>
        public string DefaultTimeZone { get; set; }

        /// <summary>
        /// User type - 1: System Admin, 2: Normal User
        /// System Admin (UserType = 1) bypasses all permission checks
        /// </summary>
        public int UserType { get; set; } = 2;

        /// <summary>
        /// Check if current user is system admin
        /// </summary>
        public bool IsSystemAdmin => UserType == 1;

        public UserTeamModel UserTeams { get; set; }
        public UserDataPermissionConfigModel UserDataPermissionConfig { get; set; }
    }
}
