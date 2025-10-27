using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using Xunit;

namespace FlowFlex.Tests.Services.Permission
{
    public class WorkflowPermissionServiceTests
    {
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<WorkflowPermissionService>> _mockLogger;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<PermissionHelpers>> _mockHelperLogger;
        private readonly Mock<FlowFlex.Domain.Repository.OW.IWorkflowRepository> _mockWorkflowRepo;

        public WorkflowPermissionServiceTests()
        {
            _mockLogger = MockHelper.CreateMockLogger<WorkflowPermissionService>();
            _mockHelperLogger = MockHelper.CreateMockLogger<PermissionHelpers>();
            _mockWorkflowRepo = MockHelper.CreateMockWorkflowRepository();
        }

        private WorkflowPermissionService CreateService(Domain.Shared.Models.UserContext userContext)
        {
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var permissionHelpers = new PermissionHelpers(
                _mockHelperLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            return new WorkflowPermissionService(
                _mockLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                permissionHelpers);
        }

        #region CheckWorkflowPermission - Public Mode Tests

        [Fact]
        public void CheckWorkflowPermission_PublicMode_ViewOperation_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
            result.GrantReason.Should().Be("ViewPermission");
        }

        [Fact]
        public void CheckWorkflowPermission_PublicMode_OperateWithEmptyTeams_ShouldGrantOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            workflow.OperateTeams = null; // Empty = all can operate in Public mode
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
            result.GrantReason.Should().Be("OperatePermission");
        }

        [Fact]
        public void CheckWorkflowPermission_PublicMode_OperateWithTeamsWhitelist_UserInTeams_ShouldGrantOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            workflow.OperateTeams = Newtonsoft.Json.JsonConvert.SerializeObject(
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
        }

        [Fact]
        public void CheckWorkflowPermission_PublicMode_OperateWithTeamsWhitelist_UserNotInTeams_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            workflow.OperateTeams = Newtonsoft.Json.JsonConvert.SerializeObject(
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not operate permission");
        }

        #endregion

        #region CheckWorkflowPermission - VisibleToTeams Mode Tests

        [Fact]
        public void CheckWorkflowPermission_VisibleToTeams_UserInTeams_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
        }

        [Fact]
        public void CheckWorkflowPermission_VisibleToTeams_UserNotInTeams_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not have view permission");
        }

        [Fact]
        public void CheckWorkflowPermission_VisibleToTeams_OperateWithWhitelist_UserInBoth_ShouldGrantOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB },
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
        }

        [Fact]
        public void CheckWorkflowPermission_VisibleToTeams_OperateWithWhitelist_UserInViewOnlyNotOperate_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB },
                new List<string> { TestDataBuilder.TeamA });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region CheckWorkflowPermission - InvisibleToTeams Mode Tests

        [Fact]
        public void CheckWorkflowPermission_InvisibleToTeams_UserNotInBlacklist_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var workflow = TestDataBuilder.CreateInvisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
        }

        [Fact]
        public void CheckWorkflowPermission_InvisibleToTeams_UserInBlacklist_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreateInvisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
        }

        [Fact]
        public void CheckWorkflowPermission_InvisibleToTeams_UserInOperateWhitelist_ShouldGrantOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var workflow = TestDataBuilder.CreateInvisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA }, // View blacklist (TeamC not in blacklist, can view)
                new List<string> { TestDataBuilder.TeamC }); // Operate whitelist (TeamC in whitelist, can operate)
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
        }

        [Fact]
        public void CheckWorkflowPermission_InvisibleToTeams_UserNotInOperateWhitelist_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var workflow = TestDataBuilder.CreateInvisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA }, // View blacklist (TeamB not in blacklist, can view)
                new List<string> { TestDataBuilder.TeamC }); // Operate whitelist (TeamB NOT in whitelist, cannot operate)
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region CheckWorkflowPermission - Private Mode Tests

        [Fact]
        public void CheckWorkflowPermission_Private_Owner_ShouldGrantView()
        {
            // Arrange
            var userId = TestDataBuilder.OwnerUserId;
            var userContext = TestDataBuilder.CreateUserContext(userId);
            var workflow = TestDataBuilder.CreatePrivateWorkflow(userId);
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, userId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
        }

        [Fact]
        public void CheckWorkflowPermission_Private_NotOwner_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var workflow = TestDataBuilder.CreatePrivateWorkflow(TestDataBuilder.OwnerUserId);
            var service = CreateService(userContext);

            // Act
            var result = service.CheckWorkflowPermission(workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
        }

        #endregion
    }
}

