using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Tests.TestBase
{
    /// <summary>
    /// Test data builder for creating test entities
    /// </summary>
    public static class TestDataBuilder
    {
        // Default test user IDs
        public const long DefaultUserId = 123;
        public const long OwnerUserId = 100;
        public const long OtherUserId = 999;

        // Default test team IDs (as strings for JSON serialization in whitelist/blacklist)
        public const string TeamA = "1001";
        public const string TeamB = "1002";
        public const string TeamC = "1003";

        // Numeric team IDs for UserTeamModel
        public const long TeamAId = 1001;
        public const long TeamBId = 1002;
        public const long TeamCId = 1003;

        // Default test tenant ID
        public const string DefaultTenantId = "tenant-1";

        #region Workflow Builders

        public static Workflow CreatePublicWorkflow(long? createUserId = null)
        {
            return new Workflow
            {
                Id = 1,
                Name = "Public Workflow",
                ViewPermissionMode = ViewPermissionModeEnum.Public,
                ViewTeams = null,
                OperateTeams = null,
                CreateUserId = createUserId ?? OwnerUserId,
                TenantId = DefaultTenantId
            };
        }

        public static Workflow CreateVisibleToTeamsWorkflow(List<string> viewTeams, List<string> operateTeams = null)
        {
            return new Workflow
            {
                Id = 2,
                Name = "Visible To Teams Workflow",
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewTeams = Newtonsoft.Json.JsonConvert.SerializeObject(viewTeams),
                OperateTeams = operateTeams != null ? Newtonsoft.Json.JsonConvert.SerializeObject(operateTeams) : null,
                CreateUserId = OwnerUserId,
                TenantId = DefaultTenantId
            };
        }

        public static Workflow CreateInvisibleToTeamsWorkflow(List<string> viewTeams, List<string> operateTeams = null)
        {
            return new Workflow
            {
                Id = 3,
                Name = "Invisible To Teams Workflow",
                ViewPermissionMode = ViewPermissionModeEnum.InvisibleToTeams,
                ViewTeams = Newtonsoft.Json.JsonConvert.SerializeObject(viewTeams),
                OperateTeams = operateTeams != null ? Newtonsoft.Json.JsonConvert.SerializeObject(operateTeams) : null,
                CreateUserId = OwnerUserId,
                TenantId = DefaultTenantId
            };
        }

        public static Workflow CreatePrivateWorkflow(long createUserId)
        {
            return new Workflow
            {
                Id = 4,
                Name = "Private Workflow",
                ViewPermissionMode = ViewPermissionModeEnum.Private,
                ViewTeams = null,
                OperateTeams = null,
                CreateUserId = createUserId,
                TenantId = DefaultTenantId
            };
        }

        #endregion

        #region Stage Builders

        public static Stage CreateStageWithInheritedPermissions(long workflowId)
        {
            return new Stage
            {
                Id = 10,
                WorkflowId = workflowId,
                Name = "Stage with Inherited Permissions",
                ViewPermissionMode = ViewPermissionModeEnum.Public,
                ViewTeams = null, // Inherits from workflow
                OperateTeams = null, // Inherits from workflow
                DefaultAssignee = null,
                CreateUserId = OwnerUserId
            };
        }

        public static Stage CreateStageWithNarrowedPermissions(long workflowId, List<string> viewTeams, List<string> operateTeams = null)
        {
            return new Stage
            {
                Id = 11,
                WorkflowId = workflowId,
                Name = "Stage with Narrowed Permissions",
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewTeams = Newtonsoft.Json.JsonConvert.SerializeObject(viewTeams),
                OperateTeams = operateTeams != null ? Newtonsoft.Json.JsonConvert.SerializeObject(operateTeams) : null,
                DefaultAssignee = null,
                CreateUserId = OwnerUserId
            };
        }

        public static Stage CreateStageWithAssignedUser(long workflowId, List<string> assignedUserIds)
        {
            return new Stage
            {
                Id = 12,
                WorkflowId = workflowId,
                Name = "Stage with Assigned Users",
                ViewPermissionMode = ViewPermissionModeEnum.Public,
                ViewTeams = null,
                OperateTeams = null,
                DefaultAssignee = Newtonsoft.Json.JsonConvert.SerializeObject(assignedUserIds),
                CreateUserId = OwnerUserId
            };
        }

        #endregion

        #region Case (Onboarding) Builders

        public static Onboarding CreatePublicCase(long workflowId, long? ownership = null)
        {
            return new Onboarding
            {
                Id = 20,
                WorkflowId = workflowId,
                ViewPermissionMode = ViewPermissionModeEnum.Public,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = null,
                ViewUsers = null,
                OperateTeams = null,
                OperateUsers = null,
                Ownership = ownership
            };
        }

        public static Onboarding CreateVisibleToTeamsCase(List<string> viewTeams, List<string> operateTeams = null, long? ownership = null)
        {
            return new Onboarding
            {
                Id = 21,
                WorkflowId = 1,
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = Newtonsoft.Json.JsonConvert.SerializeObject(viewTeams),
                ViewUsers = null,
                OperateTeams = operateTeams != null ? Newtonsoft.Json.JsonConvert.SerializeObject(operateTeams) : null,
                OperateUsers = null,
                Ownership = ownership
            };
        }

        public static Onboarding CreateVisibleToUsersCase(List<string> viewUsers, List<string> operateUsers = null, long? ownership = null)
        {
            return new Onboarding
            {
                Id = 22,
                WorkflowId = 1,
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User,
                ViewTeams = null,
                ViewUsers = Newtonsoft.Json.JsonConvert.SerializeObject(viewUsers),
                OperateTeams = null,
                OperateUsers = operateUsers != null ? Newtonsoft.Json.JsonConvert.SerializeObject(operateUsers) : null,
                Ownership = ownership
            };
        }

        public static Onboarding CreateOnboardingWithOwnership(long ownerUserId, ViewPermissionModeEnum viewMode)
        {
            return new Onboarding
            {
                Id = 23,
                WorkflowId = 1,
                ViewPermissionMode = viewMode,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = null,
                ViewUsers = null,
                OperateTeams = null,
                OperateUsers = null,
                Ownership = ownerUserId
            };
        }

        public static Onboarding CreateOnboardingInPublicMode(long workflowId, long? ownership = null)
        {
            return new Onboarding
            {
                Id = 24,
                WorkflowId = workflowId,
                ViewPermissionMode = ViewPermissionModeEnum.Public,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = null,
                ViewUsers = null,
                OperateTeams = null,
                OperateUsers = null,
                Ownership = ownership
            };
        }

        public static Onboarding CreateOnboardingWithTeamPermissions(
            ViewPermissionModeEnum viewMode,
            List<string> viewTeams,
            List<string> operateTeams,
            long? ownership = null)
        {
            return new Onboarding
            {
                Id = 25,
                WorkflowId = 1,
                ViewPermissionMode = viewMode,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = viewTeams != null && viewTeams.Any()
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(viewTeams)
                    : null,
                ViewUsers = null,
                OperateTeams = operateTeams != null && operateTeams.Any()
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(operateTeams)
                    : null,
                OperateUsers = null,
                Ownership = ownership
            };
        }

        public static Onboarding CreateOnboardingWithUserPermissions(
            ViewPermissionModeEnum viewMode,
            List<string> viewUsers,
            List<string> operateUsers,
            long? ownership = null)
        {
            return new Onboarding
            {
                Id = 26,
                WorkflowId = 1,
                ViewPermissionMode = viewMode,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User,
                ViewTeams = null,
                ViewUsers = viewUsers != null && viewUsers.Any()
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(viewUsers)
                    : null,
                OperateTeams = null,
                OperateUsers = operateUsers != null && operateUsers.Any()
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(operateUsers)
                    : null,
                Ownership = ownership
            };
        }

        #endregion

        #region UserContext Builders

        public static UserContext CreateUserContext(
            long userId,
            List<string> teamIds = null,
            bool isSystemAdmin = false,
            string tenantId = DefaultTenantId,
            string iamToken = "mock-token")
        {
            var userContext = new UserContext
            {
                UserId = userId.ToString(),
                UserName = $"User{userId}",
                TenantId = tenantId,
                IamToken = iamToken
            };

            // Set UserPermissions for system admin or normal user
            if (isSystemAdmin)
            {
                userContext.UserPermissions = new List<UserPermissionModel>
                {
                    new UserPermissionModel { TenantId = tenantId, UserType = 1 } // 1 = SystemAdmin
                };
            }
            else
            {
                userContext.UserPermissions = new List<UserPermissionModel>
                {
                    new UserPermissionModel { TenantId = tenantId, UserType = 3 } // 3 = NormalUser
                };
            }

            // Set team information if provided
            if (teamIds != null && teamIds.Any())
            {
                // UserTeams is UserTeamModel which represents a tree structure
                // For simplicity in tests, we'll create a flat structure
                // Use the first team as root and remaining teams as sub-teams
                var firstTeamId = teamIds.First();
                var firstTeamNumId = long.Parse(firstTeamId); // teamIds are now numeric strings

                var subTeams = teamIds.Skip(1).Select(id => new UserTeamModel
                {
                    TeamId = long.Parse(id),
                    SubTeam = null
                }).ToList();

                userContext.UserTeams = new UserTeamModel
                {
                    TeamId = firstTeamNumId,
                    SubTeam = subTeams.Any() ? subTeams : null
                };
            }

            return userContext;
        }

        public static UserContext CreateSystemAdminContext()
        {
            return CreateUserContext(DefaultUserId, null, true);
        }

        public static UserContext CreateTenantAdminContext(string tenantId = DefaultTenantId)
        {
            var context = CreateUserContext(DefaultUserId, null, false, tenantId);
            // Tenant admin is identified by UserPermissions with UserType = 2
            context.UserPermissions = new List<UserPermissionModel>
            {
                new UserPermissionModel { TenantId = tenantId, UserType = 2 } // 2 = TenantAdmin
            };
            return context;
        }

        #endregion

        #region Case (Onboarding) Builders

        /// <summary>
        /// Create a Case (Onboarding) with Public permission mode
        /// In Public mode, Case inherits Workflow permissions
        /// </summary>
        public static Onboarding CreateCase(long workflowId, long? ownership = null)
        {
            return new Onboarding
            {
                Id = 10000,
                WorkflowId = workflowId,
                ViewPermissionMode = ViewPermissionModeEnum.Public,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = null,
                ViewUsers = null,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperateTeams = null,
                OperateUsers = null,
                Ownership = ownership,
                TenantId = DefaultTenantId,
                CreateUserId = OwnerUserId
            };
        }

        /// <summary>
        /// Create a Case (Onboarding) with VisibleToTeams permission mode and team restrictions
        /// </summary>
        public static Onboarding CreateCaseWithTeamPermissions(
            long workflowId,
            List<string> viewTeams,
            List<string> operateTeams = null,
            long? ownership = null)
        {
            return new Onboarding
            {
                Id = 10001,
                WorkflowId = workflowId,
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = Newtonsoft.Json.JsonConvert.SerializeObject(viewTeams),
                ViewUsers = null,
                OperatePermissionSubjectType = operateTeams != null
                    ? Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team
                    : Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperateTeams = operateTeams != null
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(operateTeams)
                    : null,
                OperateUsers = null,
                Ownership = ownership,
                TenantId = DefaultTenantId,
                CreateUserId = OwnerUserId
            };
        }

        /// <summary>
        /// Create a Case (Onboarding) with VisibleToTeams permission mode and user restrictions
        /// </summary>
        public static Onboarding CreateCaseWithUserPermissions(
            long workflowId,
            List<long> viewUsers,
            List<long> operateUsers = null,
            long? ownership = null)
        {
            return new Onboarding
            {
                Id = 10002,
                WorkflowId = workflowId,
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User,
                ViewTeams = null,
                ViewUsers = Newtonsoft.Json.JsonConvert.SerializeObject(viewUsers),
                OperatePermissionSubjectType = operateUsers != null
                    ? Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.User
                    : Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperateTeams = null,
                OperateUsers = operateUsers != null
                    ? Newtonsoft.Json.JsonConvert.SerializeObject(operateUsers)
                    : null,
                Ownership = ownership,
                TenantId = DefaultTenantId,
                CreateUserId = OwnerUserId
            };
        }

        /// <summary>
        /// Create a Case (Onboarding) owned by a specific user
        /// Owner has full access regardless of other permission settings
        /// </summary>
        public static Onboarding CreateCaseWithOwnership(long workflowId, long ownerUserId)
        {
            return new Onboarding
            {
                Id = 10003,
                WorkflowId = workflowId,
                ViewPermissionMode = ViewPermissionModeEnum.VisibleToTeams,
                ViewPermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                ViewTeams = null,
                ViewUsers = null,
                OperatePermissionSubjectType = Domain.Shared.Enums.OW.PermissionSubjectTypeEnum.Team,
                OperateTeams = null,
                OperateUsers = null,
                Ownership = ownerUserId, // Owner has full permission
                TenantId = DefaultTenantId,
                CreateUserId = OwnerUserId
            };
        }

        #endregion
    }
}

