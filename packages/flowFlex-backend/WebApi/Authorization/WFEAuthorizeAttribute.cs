using Item.Internal.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    /// <summary>
    /// FlowFlex custom authorization attribute
    /// Applies permission-based authorization to controllers or actions
    /// Usage: [WFEAuthorize(PermissionConsts.Case.Create, PermissionConsts.Case.Update)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WFEAuthorizeAttribute : UnisAuthorizeAttribute
    {
        /// <summary>
        /// Constructor with permission codes
        /// </summary>
        /// <param name="permissions">Permission codes required to access this endpoint</param>
        public WFEAuthorizeAttribute(params string[] permissions) : base(permissions)
        {
        }
    }
}


