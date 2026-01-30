using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.Application.Contracts.IServices.OW.Onboarding
{
    /// <summary>
    /// Service interface for onboarding user tree and team management
    /// Handles: User tree filtering, team management, authorized users
    /// </summary>
    public interface IOnboardingUserManagementService : IScopedService
    {
        /// <summary>
        /// Get authorized users for an onboarding based on permission configuration
        /// </summary>
        /// <param name="id">Onboarding ID</param>
        /// <returns>List of authorized user tree nodes</returns>
        Task<List<UserTreeNodeDto>> GetAuthorizedUsersAsync(long id);

        /// <summary>
        /// Filter user tree based on permission configuration
        /// </summary>
        Task<List<UserTreeNodeDto>> FilterUserTreeByPermissionAsync(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            PermissionSubjectTypeEnum viewPermissionSubjectType,
            List<string> viewTeams,
            List<string> viewUsers,
            long? ownership);

        /// <summary>
        /// Filter user tree to return only ownership user
        /// </summary>
        Task<List<UserTreeNodeDto>> FilterUserTreeByOwnershipAsync(
            List<UserTreeNodeDto> allUsersTree,
            long? ownership);

        /// <summary>
        /// Find user node in the tree by user ID
        /// </summary>
        UserTreeNodeDto FindUserNodeInTree(List<UserTreeNodeDto> tree, string userId);

        /// <summary>
        /// Build tree structure for a single user, preserving team hierarchy if possible
        /// </summary>
        List<UserTreeNodeDto> BuildTreeForSingleUser(UserTreeNodeDto userNode, List<UserTreeNodeDto> allUsersTree);

        /// <summary>
        /// Extract flat list of user nodes from a (team+user) tree, with deduplication by user ID
        /// </summary>
        List<UserTreeNodeDto> ExtractUserNodes(List<UserTreeNodeDto> nodes);

        /// <summary>
        /// Find the team that contains a specific user
        /// </summary>
        UserTreeNodeDto FindUserTeam(List<UserTreeNodeDto> tree, string userId);

        /// <summary>
        /// Filter user tree by teams based on permission mode
        /// </summary>
        List<UserTreeNodeDto> FilterUserTreeByTeams(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewTeams);

        /// <summary>
        /// Filter user tree by users based on permission mode
        /// </summary>
        List<UserTreeNodeDto> FilterUserTreeByUsers(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewUsers);

        /// <summary>
        /// Ensure case code is generated for legacy data (if CaseCode is null or empty)
        /// </summary>
        Task EnsureCaseCodeAsync(Domain.Entities.OW.Onboarding entity);
    }
}
