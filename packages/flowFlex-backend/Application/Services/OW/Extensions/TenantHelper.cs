using System;

namespace FlowFlex.Application.Services.OW.Extensions
{
    /// <summary>
    /// Tenant helper class
    /// </summary>
    public static class TenantHelper
    {
        /// <summary>
        /// Get tenant ID by email address
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Tenant ID</returns>
        public static string GetTenantIdByEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                return "default";
            }

            // Extract email domain
            var domain = email.Split('@')[1].ToUpper();

            // Generate tenant ID based on domain
            // Here we use domain as tenant ID, you can adjust the rules as needed
            return domain;
        }

        /// <summary>
        /// Get tenant ID by email domain
        /// </summary>
        /// <param name="domain">Email domain</param>
        /// <returns>Tenant ID</returns>
        public static string GetTenantIdByDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return "default";
            }

            return domain.ToUpper();
        }
    }
}