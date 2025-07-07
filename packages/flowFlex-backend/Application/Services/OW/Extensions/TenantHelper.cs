using System;

namespace FlowFlex.Application.Services.OW.Extensions
{
    /// <summary>
    /// 租户帮助类
    /// </summary>
    public static class TenantHelper
    {
        /// <summary>
        /// 根据邮箱地址获取租户ID
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <returns>租户ID</returns>
        public static string GetTenantIdByEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                return "DEFAULT";
            }

            // 提取邮箱域名
            var domain = email.Split('@')[1].ToUpper();
            
            // 根据域名生成租户ID
            // 这里使用域名作为租户ID，您可以根据需要调整规则
            return domain;
        }

        /// <summary>
        /// 根据邮箱域名获取租户ID
        /// </summary>
        /// <param name="domain">邮箱域名</param>
        /// <returns>租户ID</returns>
        public static string GetTenantIdByDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return "DEFAULT";
            }

            return domain.ToUpper();
        }
    }
} 