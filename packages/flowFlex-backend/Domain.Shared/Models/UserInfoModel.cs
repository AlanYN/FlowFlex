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
        /// User permissions per tenant (from IDM)
        /// Each entry contains TenantId, UserType (1=SystemAdmin, 2=TenantAdmin, 3=NormalUser), and RoleIds
        /// </summary>
        public List<UserPermissionModel> UserPermissions { get; set; } = new List<UserPermissionModel>();

        /// <summary>
        /// Check if current user is system admin (UserType = 1 in any tenant)
        /// </summary>
        public bool IsSystemAdmin
        {
            get
            {
                // Check if user has system admin permission in any tenant
                return UserPermissions?.Any(p => p.UserType == 1) ?? false;
            }
        }

        /// <summary>
        /// Check if current user is tenant admin for the specified tenant
        /// Tenant Admin (UserType = 2) has full permissions within their tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID to check</param>
        /// <returns>True if user is tenant admin for the specified tenant</returns>
        public bool IsTenantAdmin(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return false;
            }

            // Check if user has tenant admin permission (UserType = 2) for the specified tenant
            return UserPermissions?.Any(p => 
                p.TenantId == tenantId && p.UserType == 2) ?? false;
        }

        /// <summary>
        /// Check if current user has admin privileges (System Admin or Tenant Admin) for the specified tenant
        /// </summary>
        /// <param name="tenantId">Tenant ID to check</param>
        /// <returns>True if user is system admin or tenant admin for the specified tenant</returns>
        public bool HasAdminPrivileges(string tenantId)
        {
            // System admin has privileges everywhere
            if (IsSystemAdmin)
            {
                return true;
            }

            // Check tenant admin for specific tenant
            return IsTenantAdmin(tenantId);
        }

        public UserTeamModel UserTeams { get; set; }
        public UserDataPermissionConfigModel UserDataPermissionConfig { get; set; }
    }

    /// <summary>
    /// User permission model for a specific tenant
    /// </summary>
    public class UserPermissionModel
    {
        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// User type in this tenant: 1=SystemAdmin, 2=TenantAdmin, 3=NormalUser
        /// </summary>
        public int UserType { get; set; }

        /// <summary>
        /// Role IDs assigned to user in this tenant
        /// </summary>
        public List<string> RoleIds { get; set; } = new List<string>();
    }
}
