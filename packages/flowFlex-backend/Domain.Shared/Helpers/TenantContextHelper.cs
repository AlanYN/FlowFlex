using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Shared.Helpers
{
    /// <summary>
    /// Helper class for tenant context validation and retrieval
    /// Ensures proper tenant isolation across the application
    /// </summary>
    public static class TenantContextHelper
    {
        /// <summary>
        /// Get TenantId from UserContext with validation
        /// Throws ArgumentException if TenantId is missing
        /// </summary>
        /// <param name="userContext">User context</param>
        /// <param name="operation">Operation name for error context</param>
        /// <returns>Valid TenantId</returns>
        /// <exception cref="ArgumentException">Thrown when TenantId is missing</exception>
        public static string GetRequiredTenantId(UserContext? userContext, string? operation = null)
        {
            if (userContext == null)
            {
                throw new ArgumentException(
                    $"User context is required for tenant isolation{(operation != null ? $" in {operation}" : "")}");
            }

            if (string.IsNullOrWhiteSpace(userContext.TenantId))
            {
                throw new ArgumentException(
                    $"TenantId is required for tenant isolation{(operation != null ? $" in {operation}" : "")}. Ensure the request includes X-Tenant-Id header.");
            }

            return userContext.TenantId;
        }

        /// <summary>
        /// Get TenantId from UserContext with fallback to default
        /// Use this only for non-critical operations where default tenant is acceptable
        /// </summary>
        /// <param name="userContext">User context</param>
        /// <returns>TenantId or "default"</returns>
        public static string GetTenantIdOrDefault(UserContext? userContext)
        {
            return userContext?.TenantId ?? "default";
        }

        /// <summary>
        /// Check if UserContext has valid tenant context
        /// </summary>
        /// <param name="userContext">User context</param>
        /// <returns>True if valid tenant context exists</returns>
        public static bool HasValidTenantContext(UserContext? userContext)
        {
            return userContext != null && !string.IsNullOrWhiteSpace(userContext.TenantId);
        }

        /// <summary>
        /// Get AppCode from UserContext with fallback to default
        /// </summary>
        /// <param name="userContext">User context</param>
        /// <returns>AppCode or "default"</returns>
        public static string GetAppCodeOrDefault(UserContext? userContext)
        {
            return userContext?.AppCode ?? "default";
        }
    }
}
