using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;

namespace FlowFlex.Tests.Services.Permission
{
    /// <summary>
    /// Unit tests for CasePermissionService
    /// Tests Case permission verification logic including Ownership and Workflow inheritance
    /// </summary>
    public class CasePermissionServiceTests
    {
        private readonly Mock<ILogger<CasePermissionService>> _mockLogger;
        private readonly Mock<ILogger<PermissionHelpers>> _mockHelpersLogger;
        private readonly Mock<ILogger<WorkflowPermissionService>> _mockWorkflowLogger;
        private readonly Mock<IOnboardingRepository> _mockOnboardingRepo;
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public CasePermissionServiceTests()
        {
            _mockLogger = new Mock<ILogger<CasePermissionService>>();
            _mockHelpersLogger = new Mock<ILogger<PermissionHelpers>>();
            _mockWorkflowLogger = new Mock<ILogger<WorkflowPermissionService>>();
            _mockOnboardingRepo = new Mock<IOnboardingRepository>();
            _mockWorkflowRepo = new Mock<IWorkflowRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        }

        private CasePermissionService CreateService(UserContext userContext)
        {
            var helpers = new PermissionHelpers(
                _mockHelpersLogger.Object,
                userContext,
                _mockHttpContextAccessor.Object);

            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                helpers);

            return new CasePermissionService(
                _mockLogger.Object,
                userContext,
                _mockOnboardingRepo.Object,
                _mockWorkflowRepo.Object,
                helpers,
                workflowPermissionService);
        }

        #region Ownership Tests

        [Fact]
        public async Task CheckCasePermission_Owner_ShouldGrantFullAccess()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var onboarding = TestDataBuilder.CreateOnboardingWithOwnership(
                TestDataBuilder.DefaultUserId,
                ViewPermissionModeEnum.Private);
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
            result.GrantReason.Should().Be("Owner");
        }

        [Fact]
        public async Task CheckCasePermission_NotOwner_PrivateMode_ShouldDenyAccess()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var onboarding = TestDataBuilder.CreateOnboardingWithOwnership(
                999L, // Different owner
                ViewPermissionModeEnum.Private);
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region Public Mode - Workflow Inheritance Tests

        [Fact]
        public async Task CheckCasePermission_PublicMode_InheritsWorkflowViewPermission()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var onboarding = TestDataBuilder.CreateOnboardingInPublicMode(workflow.Id);
            var service = CreateService(userContext);

            _mockWorkflowRepo.Setup(x => x.GetByIdAsync(workflow.Id, false, default))
                .ReturnsAsync(workflow);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
            result.GrantReason.Should().Be("WorkflowInheritedViewPermission");
        }

        [Fact]
        public async Task CheckCasePermission_PublicMode_InheritsWorkflowOperatePermission()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            workflow.OperateTeams = null; // Everyone can operate in public mode
            var onboarding = TestDataBuilder.CreateOnboardingInPublicMode(workflow.Id);
            var service = CreateService(userContext);

            _mockWorkflowRepo.Setup(x => x.GetByIdAsync(workflow.Id, false, default))
                .ReturnsAsync(workflow);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
            result.GrantReason.Should().Be("WorkflowInheritedOperatePermission");
        }

        [Fact]
        public async Task CheckCasePermission_PublicMode_WorkflowOperateRestricted_UserNotInTeams_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            workflow.OperateTeams = Newtonsoft.Json.JsonConvert.SerializeObject(
                new List<string> { TestDataBuilder.TeamA });
            var onboarding = TestDataBuilder.CreateOnboardingInPublicMode(workflow.Id);
            var service = CreateService(userContext);

            _mockWorkflowRepo.Setup(x => x.GetByIdAsync(workflow.Id, false, default))
                .ReturnsAsync(workflow);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue(); // Should be able to view
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region VisibleToTeams Mode - Team-based Permission Tests

        [Fact]
        public async Task CheckCasePermission_VisibleToTeams_TeamBased_UserInViewTeams_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.VisibleToTeams,
                new List<string> { TestDataBuilder.TeamA },
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        [Fact]
        public async Task CheckCasePermission_VisibleToTeams_TeamBased_UserNotInViewTeams_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.VisibleToTeams,
                new List<string> { TestDataBuilder.TeamA },
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.CanOperate.Should().BeFalse();
        }

        [Fact]
        public async Task CheckCasePermission_VisibleToTeams_TeamBased_UserInViewButNotOperate_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.VisibleToTeams,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB },
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue(); // Should be able to view
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region VisibleToTeams Mode - User-based Permission Tests

        [Fact]
        public async Task CheckCasePermission_VisibleToTeams_UserBased_UserInViewUsers_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var onboarding = TestDataBuilder.CreateOnboardingWithUserPermissions(
                ViewPermissionModeEnum.VisibleToTeams,
                new List<string> { TestDataBuilder.DefaultUserId.ToString() },
                new List<string> { TestDataBuilder.DefaultUserId.ToString() });
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        [Fact]
        public async Task CheckCasePermission_VisibleToTeams_UserBased_UserNotInViewUsers_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var onboarding = TestDataBuilder.CreateOnboardingWithUserPermissions(
                ViewPermissionModeEnum.VisibleToTeams,
                new List<string> { "999" }, // Different user
                new List<string> { "999" });
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region InvisibleToTeams Mode - Team-based Blacklist Tests

        [Fact]
        public async Task CheckCasePermission_InvisibleToTeams_TeamBased_UserNotInBlacklist_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.InvisibleToTeams,
                new List<string> { TestDataBuilder.TeamA }, // Blacklist TeamA
                new List<string>());
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        [Fact]
        public async Task CheckCasePermission_InvisibleToTeams_TeamBased_UserInBlacklist_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.InvisibleToTeams,
                new List<string> { TestDataBuilder.TeamA }, // Blacklist TeamA
                new List<string>());
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.CanOperate.Should().BeFalse();
        }

        [Fact]
        public async Task CheckCasePermission_InvisibleToTeams_TeamBased_UserNotInOperateWhitelist_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.InvisibleToTeams,
                new List<string> { TestDataBuilder.TeamA }, // View blacklist (TeamB not in blacklist, can view)
                new List<string> { TestDataBuilder.TeamA }); // Operate whitelist (TeamB not in whitelist, cannot operate)
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue(); // Not in view blacklist
            result.CanOperate.Should().BeFalse(); // NOT in operate whitelist (Operate is ALWAYS whitelist)
        }

        #endregion

        #region InvisibleToTeams Mode - User-based Blacklist Tests

        [Fact]
        public async Task CheckCasePermission_InvisibleToTeams_UserBased_UserNotInBlacklist_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var onboarding = TestDataBuilder.CreateOnboardingWithUserPermissions(
                ViewPermissionModeEnum.InvisibleToTeams,
                new List<string> { "999" }, // Blacklist different user
                new List<string>());
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        [Fact]
        public async Task CheckCasePermission_InvisibleToTeams_UserBased_UserInBlacklist_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var onboarding = TestDataBuilder.CreateOnboardingWithUserPermissions(
                ViewPermissionModeEnum.InvisibleToTeams,
                new List<string> { TestDataBuilder.DefaultUserId.ToString() }, // Blacklist current user
                new List<string>());
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region Delete Permission Tests

        [Fact]
        public async Task CheckCasePermission_DeleteOperation_SameAsOperatePermission()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var onboarding = TestDataBuilder.CreateOnboardingWithTeamPermissions(
                ViewPermissionModeEnum.VisibleToTeams,
                new List<string> { TestDataBuilder.TeamA },
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = await service.CheckCasePermissionAsync(onboarding, TestDataBuilder.DefaultUserId, PermissionOperationType.Delete);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
        }

        #endregion
    }
}

