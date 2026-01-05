using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using FlowFlex.Application.Contracts.Dtos.OW.User;


namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - User tree and team management
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<List<UserTreeNodeDto>> GetAuthorizedUsersAsync(long id)
        {
            // Get onboarding entity
            var onboarding = await _onboardingRepository.GetByIdAsync(id);
            if (onboarding == null)
            {
                throw new CRMException(System.Net.HttpStatusCode.NotFound, $"Onboarding with ID {id} not found");
            }

            // Get all users tree
            var allUsersTree = await _userService.GetUserTreeAsync();

            // If permission mode is Public, return all users (flatten to user-only list)
            if (onboarding.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return ExtractUserNodes(allUsersTree);
            }

            // Parse permission fields
            var viewTeams = ParseJsonArraySafe(onboarding.ViewTeams) ?? new List<string>();
            var viewUsers = ParseJsonArraySafe(onboarding.ViewUsers) ?? new List<string>();

            // Filter users based on permission configuration
            var filteredTree = await FilterUserTreeByPermissionAsync(
                allUsersTree,
                onboarding.ViewPermissionMode,
                onboarding.ViewPermissionSubjectType,
                viewTeams,
                viewUsers,
                onboarding.Ownership
            );

            // Always return flat user-only list
            return ExtractUserNodes(filteredTree);
        }

        /// <summary>
        /// Filter user tree based on permission configuration
        /// </summary>
        private async Task<List<UserTreeNodeDto>> FilterUserTreeByPermissionAsync(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            PermissionSubjectTypeEnum viewPermissionSubjectType,
            List<string> viewTeams,
            List<string> viewUsers,
            long? ownership)
        {
            if (allUsersTree == null || !allUsersTree.Any())
            {
                return new List<UserTreeNodeDto>();
            }

            // Private mode: return ownership user only
            if (viewPermissionMode == ViewPermissionModeEnum.Private)
            {
                return await FilterUserTreeByOwnershipAsync(allUsersTree, ownership);
            }

            // Team-based permission
            if (viewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                return FilterUserTreeByTeams(allUsersTree, viewPermissionMode, viewTeams);
            }
            // User-based permission
            else if (viewPermissionSubjectType == PermissionSubjectTypeEnum.User)
            {
                return FilterUserTreeByUsers(allUsersTree, viewPermissionMode, viewUsers);
            }

            // Default: return all users
            return allUsersTree;
        }

        /// <summary>
        /// Filter user tree to return only ownership user
        /// </summary>
        private async Task<List<UserTreeNodeDto>> FilterUserTreeByOwnershipAsync(
            List<UserTreeNodeDto> allUsersTree,
            long? ownership)
        {
            if (!ownership.HasValue || ownership.Value <= 0)
            {
                // No ownership set, return empty tree
                return new List<UserTreeNodeDto>();
            }

            var ownershipUserId = ownership.Value.ToString();
            var ownershipUserNode = FindUserNodeInTree(allUsersTree, ownershipUserId);

            if (ownershipUserNode == null)
            {
                // Ownership user not found in tree, try to get from UserService
                try
                {
                    var userDto = await _userService.GetUserByIdAsync(ownership.Value);
                    if (userDto != null)
                    {
                        // Create a user node from UserDto
                        ownershipUserNode = new UserTreeNodeDto
                        {
                            Id = userDto.Id.ToString(),
                            Name = userDto.Username ?? userDto.Email,
                            Type = "user",
                            Username = userDto.Username,
                            Email = userDto.Email,
                            UserDetails = userDto,
                            MemberCount = 0,
                            Children = null
                        };
                    }
                }
                catch
                {
                    // If user not found, return empty tree
                    return new List<UserTreeNodeDto>();
                }
            }

            if (ownershipUserNode == null)
            {
                return new List<UserTreeNodeDto>();
            }

            // Return only the ownership user as a flat list (no team structure)
            return new List<UserTreeNodeDto>
            {
                new UserTreeNodeDto
                {
                    Id = ownershipUserNode.Id,
                    Name = ownershipUserNode.Name,
                    Type = "user",
                    Username = ownershipUserNode.Username,
                    Email = ownershipUserNode.Email,
                    UserDetails = ownershipUserNode.UserDetails,
                    MemberCount = 0,
                    Children = null
                }
            };
        }

        /// <summary>
        /// Find user node in the tree by user ID
        /// </summary>
        private UserTreeNodeDto FindUserNodeInTree(List<UserTreeNodeDto> tree, string userId)
        {
            if (tree == null || !tree.Any())
            {
                return null;
            }

            foreach (var node in tree)
            {
                // Check if current node is the user
                if (node.Type == "user" && node.Id == userId)
                {
                    return node;
                }

                // Recursively search in children
                if (node.Children != null && node.Children.Any())
                {
                    var found = FindUserNodeInTree(node.Children, userId);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Build tree structure for a single user, preserving team hierarchy if possible
        /// </summary>
        private List<UserTreeNodeDto> BuildTreeForSingleUser(UserTreeNodeDto userNode, List<UserTreeNodeDto> allUsersTree)
        {
            if (userNode == null)
            {
                return new List<UserTreeNodeDto>();
            }

            // Try to find the team that contains this user
            var userTeam = FindUserTeam(allUsersTree, userNode.Id);

            if (userTeam != null)
            {
                // Create a team node with only this user
                var teamNode = new UserTreeNodeDto
                {
                    Id = userTeam.Id,
                    Name = userTeam.Name,
                    Type = "team",
                    MemberCount = 1,
                    Children = new List<UserTreeNodeDto>
                    {
                        new UserTreeNodeDto
                        {
                            Id = userNode.Id,
                            Name = userNode.Name,
                            Type = "user",
                            Username = userNode.Username,
                            Email = userNode.Email,
                            UserDetails = userNode.UserDetails,
                            MemberCount = 0,
                            Children = null
                        }
                    }
                };

                return new List<UserTreeNodeDto> { teamNode };
            }
            else
            {
                // User not in any team, create a simple user node
                // Put it in "Other" team for consistency
                var otherTeamNode = new UserTreeNodeDto
                {
                    Id = "Other",
                    Name = "Other",
                    Type = "team",
                    MemberCount = 1,
                    Children = new List<UserTreeNodeDto>
                    {
                        new UserTreeNodeDto
                        {
                            Id = userNode.Id,
                            Name = userNode.Name,
                            Type = "user",
                            Username = userNode.Username,
                            Email = userNode.Email,
                            UserDetails = userNode.UserDetails,
                            MemberCount = 0,
                            Children = null
                        }
                    }
                };

                return new List<UserTreeNodeDto> { otherTeamNode };
            }
        }

        /// <summary>
        /// Extract flat list of user nodes from a (team+user) tree, with deduplication by user ID
        /// </summary>
        private List<UserTreeNodeDto> ExtractUserNodes(List<UserTreeNodeDto> nodes)
        {
            var result = new List<UserTreeNodeDto>();
            var seenUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (nodes == null || nodes.Count == 0) return result;

            void Traverse(UserTreeNodeDto node)
            {
                if (node == null) return;
                if (node.Type == "user")
                {
                    // Deduplicate by user ID
                    if (!string.IsNullOrEmpty(node.Id) && !seenUserIds.Contains(node.Id))
                    {
                        seenUserIds.Add(node.Id);
                        result.Add(new UserTreeNodeDto
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Type = node.Type,
                            Username = node.Username,
                            Email = node.Email,
                            UserDetails = node.UserDetails,
                            MemberCount = 0,
                            Children = null
                        });
                    }
                }
                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        Traverse(child);
                    }
                }
            }

            foreach (var root in nodes)
            {
                Traverse(root);
            }

            return result;
        }

        /// <summary>
        /// Find the team that contains a specific user
        /// </summary>
        private UserTreeNodeDto FindUserTeam(List<UserTreeNodeDto> tree, string userId)
        {
            if (tree == null || !tree.Any())
            {
                return null;
            }

            foreach (var node in tree)
            {
                if (node.Type == "team" && node.Children != null && node.Children.Any())
                {
                    // Check if this team contains the user
                    var containsUser = FindUserNodeInTree(node.Children, userId);
                    if (containsUser != null)
                    {
                        return node;
                    }

                    // Recursively search in child teams
                    var childTeam = FindUserTeam(node.Children, userId);
                    if (childTeam != null)
                    {
                        return childTeam;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Filter user tree by teams based on permission mode
        /// </summary>
        private List<UserTreeNodeDto> FilterUserTreeByTeams(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewTeams)
        {
            var viewTeamsSet = new HashSet<string>(viewTeams ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var result = new List<UserTreeNodeDto>();

            foreach (var teamNode in allUsersTree)
            {
                if (teamNode.Type != "team")
                {
                    continue;
                }

                bool includeTeam = false;

                if (viewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
                {
                    // Only include teams in the whitelist
                    includeTeam = viewTeamsSet.Contains(teamNode.Id);
                }
                else if (viewPermissionMode == ViewPermissionModeEnum.InvisibleToTeams)
                {
                    // Exclude teams in the blacklist
                    includeTeam = !viewTeamsSet.Contains(teamNode.Id);
                }

                if (includeTeam)
                {
                    // Create a copy of the team node with filtered children
                    var filteredTeamNode = new UserTreeNodeDto
                    {
                        Id = teamNode.Id,
                        Name = teamNode.Name,
                        Type = teamNode.Type,
                        MemberCount = 0,
                        Children = new List<UserTreeNodeDto>()
                    };

                    // Recursively filter child teams and include all users under this team
                    if (teamNode.Children != null && teamNode.Children.Any())
                    {
                        foreach (var child in teamNode.Children)
                        {
                            if (child.Type == "team")
                            {
                                // Recursively filter child teams
                                var filteredChildTeams = FilterUserTreeByTeams(
                                    new List<UserTreeNodeDto> { child },
                                    viewPermissionMode,
                                    viewTeams);
                                if (filteredChildTeams.Any())
                                {
                                    filteredTeamNode.Children.AddRange(filteredChildTeams);
                                }
                            }
                            else if (child.Type == "user")
                            {
                                // Include all users under the team
                                filteredTeamNode.Children.Add(child);
                            }
                        }
                    }

                    // Update member count
                    filteredTeamNode.MemberCount = filteredTeamNode.Children.Count;

                    // Only add team if it has members or child teams
                    if (filteredTeamNode.Children.Any())
                    {
                        result.Add(filteredTeamNode);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Filter user tree by users based on permission mode
        /// </summary>
        private List<UserTreeNodeDto> FilterUserTreeByUsers(
            List<UserTreeNodeDto> allUsersTree,
            ViewPermissionModeEnum viewPermissionMode,
            List<string> viewUsers)
        {
            var viewUsersSet = new HashSet<string>(viewUsers ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var result = new List<UserTreeNodeDto>();

            foreach (var rootNode in allUsersTree)
            {
                var filteredNode = FilterNodeByUsers(rootNode, viewPermissionMode, viewUsersSet);
                if (filteredNode != null)
                {
                    result.Add(filteredNode);
                }
            }

            return result;
        }

        /// <summary>
        /// Recursively filter a node and its children by users
        /// </summary>
        private UserTreeNodeDto FilterNodeByUsers(
            UserTreeNodeDto node,
            ViewPermissionModeEnum viewPermissionMode,
            HashSet<string> viewUsersSet)
        {
            if (node == null)
            {
                return null;
            }

            // If it's a user node, check if it should be included
            if (node.Type == "user")
            {
                bool includeUser = false;

                if (viewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
                {
                    // Only include users in the whitelist
                    includeUser = viewUsersSet.Contains(node.Id);
                }
                else if (viewPermissionMode == ViewPermissionModeEnum.InvisibleToTeams)
                {
                    // Exclude users in the blacklist
                    includeUser = !viewUsersSet.Contains(node.Id);
                }

                if (includeUser)
                {
                    return new UserTreeNodeDto
                    {
                        Id = node.Id,
                        Name = node.Name,
                        Type = node.Type,
                        Username = node.Username,
                        Email = node.Email,
                        UserDetails = node.UserDetails,
                        MemberCount = 0,
                        Children = null
                    };
                }

                return null;
            }

            // If it's a team node, filter its children
            if (node.Type == "team")
            {
                var filteredChildren = new List<UserTreeNodeDto>();

                if (node.Children != null && node.Children.Any())
                {
                    foreach (var child in node.Children)
                    {
                        var filteredChild = FilterNodeByUsers(child, viewPermissionMode, viewUsersSet);
                        if (filteredChild != null)
                        {
                            filteredChildren.Add(filteredChild);
                        }
                    }
                }

                // Only include team if it has filtered children
                if (filteredChildren.Any())
                {
                    return new UserTreeNodeDto
                    {
                        Id = node.Id,
                        Name = node.Name,
                        Type = node.Type,
                        MemberCount = filteredChildren.Count,
                        Children = filteredChildren
                    };
                }
            }

            return null;
        }

        #region Case Code Auto-Fill for Legacy Data

        /// <summary>
        /// Ensure case code is generated for legacy data (if CaseCode is null or empty)
        /// </summary>
        private async Task EnsureCaseCodeAsync(Onboarding entity)
        {
            if (string.IsNullOrWhiteSpace(entity.CaseCode))
            {
                try
                {
                    // Generate case code from lead name
                    entity.CaseCode = await _caseCodeGeneratorService.GenerateCaseCodeAsync(entity.CaseName);

                    // Update database
                    var updateSql = "UPDATE ff_onboarding SET case_code = @CaseCode WHERE id = @Id";
                    await _onboardingRepository.GetSqlSugarClient().Ado.ExecuteCommandAsync(updateSql, new
                    {
                        CaseCode = entity.CaseCode,
                        Id = entity.Id
                    });

                    _logger.LogInformation("Auto-generated CaseCode '{CaseCode}' for Onboarding {OnboardingId}", entity.CaseCode, entity.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-generate CaseCode for Onboarding {OnboardingId}", entity.Id);
                    // Don't throw - this is a background enhancement, not critical
                }
            }
        }

        #endregion
    }
}
