namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// User Context Service Interface
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>
        /// Get current user ID
        /// </summary>
        /// <returns>User ID</returns>
        long GetCurrentUserId();

        /// <summary>
        /// Get current user email
        /// </summary>
        /// <returns>User email</returns>
        string GetCurrentUserEmail();

        /// <summary>
        /// Get current username
        /// </summary>
        /// <returns>Username</returns>
        string GetCurrentUsername();

        /// <summary>
        /// Get current user's tenant ID
        /// </summary>
        /// <returns>Tenant ID</returns>
        string GetCurrentTenantId();

        /// <summary>
        /// Is authenticated
        /// </summary>
        /// <returns>Is authenticated</returns>
        bool IsAuthenticated();
    }
}
