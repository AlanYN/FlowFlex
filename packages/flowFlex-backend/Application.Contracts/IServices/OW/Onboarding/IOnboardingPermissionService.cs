using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for onboarding permission operations
    /// Centralizes all permission-related logic for onboarding module
    /// </summary>
    public interface IOnboardingPermissionService : IScopedService
    {
        #region Permission Check Methods

        /// <summary>
        /// Check if current user has operate permission on the case
        /// </summary>
        /// <param name="caseId">Case ID to check permission for</param>
        /// <returns>True if user has permission, false otherwise</returns>
        Task<bool> CheckCaseOperatePermissionAsync(long caseId);

        /// <summary>
        /// Ensure current user has operate permission on the case
        /// Throws CRMException if user does not have permission
        /// </summary>
        /// <param name="caseId">Case ID to check permission for</param>
        /// <exception cref="CRMException">Thrown when user does not have permission</exception>
        Task EnsureCaseOperatePermissionAsync(long caseId);

        /// <summary>
        /// Check if current user has view permission on the case
        /// </summary>
        /// <param name="caseId">Case ID to check permission for</param>
        /// <returns>True if user has permission, false otherwise</returns>
        Task<bool> CheckCaseViewPermissionAsync(long caseId);

        /// <summary>
        /// Ensure current user has view permission on the case
        /// Throws CRMException if user does not have permission
        /// </summary>
        /// <param name="caseId">Case ID to check permission for</param>
        /// <exception cref="CRMException">Thrown when user does not have permission</exception>
        Task EnsureCaseViewPermissionAsync(long caseId);

        #endregion

        #region Permission Info Methods

        /// <summary>
        /// Get permission info for a case (view and operate permissions)
        /// </summary>
        /// <param name="caseId">Case ID</param>
        /// <returns>Permission info DTO</returns>
        Task<PermissionInfoDto> GetCasePermissionInfoAsync(long caseId);

        /// <summary>
        /// Get permission info for a stage within an onboarding
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>Permission info DTO</returns>
        Task<PermissionInfoDto> GetStagePermissionInfoAsync(long stageId);

        /// <summary>
        /// Batch check case permissions for multiple onboardings
        /// Optimized for list operations to avoid N+1 queries
        /// </summary>
        /// <param name="entities">List of onboarding entities</param>
        /// <returns>Dictionary mapping case ID to permission info</returns>
        Task<Dictionary<long, PermissionInfoDto>> BatchCheckCasePermissionsAsync(List<Domain.Entities.OW.Onboarding> entities);

        #endregion

        #region User Context Methods

        /// <summary>
        /// Get current user ID as long
        /// </summary>
        /// <returns>User ID or null if not available</returns>
        long? GetCurrentUserId();

        /// <summary>
        /// Check if current user is authenticated
        /// </summary>
        /// <returns>True if user is authenticated</returns>
        bool IsAuthenticated();

        /// <summary>
        /// Check if current user is system admin
        /// </summary>
        /// <returns>True if user is system admin</returns>
        bool IsSystemAdmin();

        /// <summary>
        /// Check if current user is tenant admin
        /// </summary>
        /// <returns>True if user is tenant admin</returns>
        bool IsTenantAdmin();

        /// <summary>
        /// Check if current user has admin privileges (system admin or tenant admin)
        /// </summary>
        /// <returns>True if user has admin privileges</returns>
        bool HasAdminPrivileges();

        /// <summary>
        /// Get current user's team IDs
        /// </summary>
        /// <returns>List of team IDs</returns>
        List<string> GetUserTeamIds();

        /// <summary>
        /// Check if current request is using Client Credentials token
        /// </summary>
        /// <returns>True if using Client Credentials token</returns>
        bool IsClientCredentialsToken();

        #endregion

        #region Module Permission Methods

        /// <summary>
        /// Check if current user has module-level view permission for cases
        /// </summary>
        /// <returns>True if user has permission</returns>
        Task<bool> CanViewCasesAsync();

        /// <summary>
        /// Check if current user has module-level operate permission for cases
        /// </summary>
        /// <returns>True if user has permission</returns>
        Task<bool> CanOperateCasesAsync();

        #endregion
    }
}
