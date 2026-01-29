using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - User tree and team management (delegates to IOnboardingUserManagementService)
    /// </summary>
    public partial class OnboardingService
    {
        // Note: _userManagementService is injected in OnboardingService.Main.cs

        /// <inheritdoc />
        public Task<List<UserTreeNodeDto>> GetAuthorizedUsersAsync(long id)
            => _userManagementService.GetAuthorizedUsersAsync(id);

        /// <summary>
        /// Filter user tree based on permission configuration
        /// </summary>
        private Task<List<UserTreeNodeDto>> FilterUserTreeByPermissionAsync(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            PermissionSubjectTypeEnum viewPermissionSubjectType,
            List<string> viewTeams,
            List<string> viewUsers,
            long? ownership)
            => _userManagementService.FilterUserTreeByPermissionAsync(
                allUsersTree, viewPermissionMode, viewPermissionSubjectType, viewTeams, viewUsers, ownership);

        /// <summary>
        /// Filter user tree to return only ownership user
        /// </summary>
        private Task<List<UserTreeNodeDto>> FilterUserTreeByOwnershipAsync(
            List<UserTreeNodeDto> allUsersTree,
            long? ownership)
            => _userManagementService.FilterUserTreeByOwnershipAsync(allUsersTree, ownership);

        /// <summary>
        /// Find user node in the tree by user ID
        /// </summary>
        private UserTreeNodeDto FindUserNodeInTree(List<UserTreeNodeDto> tree, string userId)
            => _userManagementService.FindUserNodeInTree(tree, userId);

        /// <summary>
        /// Build tree structure for a single user, preserving team hierarchy if possible
        /// </summary>
        private List<UserTreeNodeDto> BuildTreeForSingleUser(UserTreeNodeDto userNode, List<UserTreeNodeDto> allUsersTree)
            => _userManagementService.BuildTreeForSingleUser(userNode, allUsersTree);

        /// <summary>
        /// Extract flat list of user nodes from a (team+user) tree, with deduplication by user ID
        /// </summary>
        private List<UserTreeNodeDto> ExtractUserNodes(List<UserTreeNodeDto> nodes)
            => _userManagementService.ExtractUserNodes(nodes);

        /// <summary>
        /// Find the team that contains a specific user
        /// </summary>
        private UserTreeNodeDto FindUserTeam(List<UserTreeNodeDto> tree, string userId)
            => _userManagementService.FindUserTeam(tree, userId);

        /// <summary>
        /// Filter user tree by teams based on permission mode
        /// </summary>
        private List<UserTreeNodeDto> FilterUserTreeByTeams(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewTeams)
            => _userManagementService.FilterUserTreeByTeams(allUsersTree, viewPermissionMode, viewTeams);

        /// <summary>
        /// Filter user tree by users based on permission mode
        /// </summary>
        private List<UserTreeNodeDto> FilterUserTreeByUsers(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewUsers)
            => _userManagementService.FilterUserTreeByUsers(allUsersTree, viewPermissionMode, viewUsers);

        /// <summary>
        /// Ensure case code is generated for legacy data (if CaseCode is null or empty)
        /// </summary>
        private Task EnsureCaseCodeAsync(Onboarding entity)
            => _userManagementService.EnsureCaseCodeAsync(entity);
    }
}
