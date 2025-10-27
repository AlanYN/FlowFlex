using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using Xunit;

namespace FlowFlex.Tests.Services.Permission
{
    public class StagePermissionServiceTests
    {
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<StagePermissionService>> _mockLogger;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<WorkflowPermissionService>> _mockWorkflowLogger;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<PermissionHelpers>> _mockHelperLogger;
        private readonly Mock<FlowFlex.Domain.Repository.OW.IStageRepository> _mockStageRepo;
        private readonly Mock<FlowFlex.Domain.Repository.OW.IWorkflowRepository> _mockWorkflowRepo;

        public StagePermissionServiceTests()
        {
            _mockLogger = MockHelper.CreateMockLogger<StagePermissionService>();
            _mockWorkflowLogger = MockHelper.CreateMockLogger<WorkflowPermissionService>();
            _mockHelperLogger = MockHelper.CreateMockLogger<PermissionHelpers>();
            _mockStageRepo = MockHelper.CreateMockStageRepository();
            _mockWorkflowRepo = MockHelper.CreateMockWorkflowRepository();
        }

        private StagePermissionService CreateService(Domain.Shared.Models.UserContext userContext)
        {
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var permissionHelpers = new PermissionHelpers(
                _mockHelperLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var workflowPermissionService = new WorkflowPermissionService(
                _mockWorkflowLogger.Object,
                userContext,
                _mockWorkflowRepo.Object,
                permissionHelpers);

            return new StagePermissionService(
                _mockLogger.Object,
                userContext,
                _mockStageRepo.Object,
                _mockWorkflowRepo.Object,
                permissionHelpers,
                workflowPermissionService);
        }

        #region CheckStagePermission - Inherited Permissions Tests

        [Fact]
        public void CheckStagePermission_InheritedFromPublicWorkflow_ViewOperation_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
            result.GrantReason.Should().Contain("Inherited");
        }

        [Fact]
        public void CheckStagePermission_InheritedFromPublicWorkflow_OperateWithEmptyTeams_ShouldGrantOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            workflow.OperateTeams = null; // Inherit = all can operate
            var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
        }

        [Fact]
        public void CheckStagePermission_InheritedFromPrivateWorkflow_NotOwner_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var workflow = TestDataBuilder.CreatePrivateWorkflow(TestDataBuilder.OwnerUserId);
            var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not have view permission");
        }

        #endregion

        #region CheckStagePermission - Narrowed Permissions Tests

        [Fact]
        public void CheckStagePermission_NarrowedPermissions_UserInBothWorkflowAndStage_ShouldGrantView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var stage = TestDataBuilder.CreateStageWithNarrowedPermissions(
                workflow.Id,
                new List<string> { TestDataBuilder.TeamA }); // Narrowed from workflow
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanView.Should().BeTrue();
        }

        [Fact]
        public void CheckStagePermission_NarrowedPermissions_UserInWorkflowButNotStage_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var stage = TestDataBuilder.CreateStageWithNarrowedPermissions(
                workflow.Id,
                new List<string> { TestDataBuilder.TeamA }); // User not in stage teams
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not have view permission");
        }

        [Fact]
        public void CheckStagePermission_NarrowedPermissions_UserInStageButNotWorkflow_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamC });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var stage = TestDataBuilder.CreateStageWithNarrowedPermissions(
                workflow.Id,
                new List<string> { TestDataBuilder.TeamC }); // User in stage but not workflow
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not have view permission");
        }

        [Fact]
        public void CheckStagePermission_NarrowedOperate_UserInBothWorkflowAndStageOperateTeams_ShouldGrantOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB },
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var stage = TestDataBuilder.CreateStageWithNarrowedPermissions(
                workflow.Id,
                new List<string> { TestDataBuilder.TeamA },
                new List<string> { TestDataBuilder.TeamA }); // Narrowed operate teams
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.CanOperate.Should().BeTrue();
        }

        [Fact]
        public void CheckStagePermission_NarrowedOperate_UserInWorkflowOperateButNotStageOperate_ShouldDenyOperate()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var workflow = TestDataBuilder.CreateVisibleToTeamsWorkflow(
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB },
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var stage = TestDataBuilder.CreateStageWithNarrowedPermissions(
                workflow.Id,
                new List<string> { TestDataBuilder.TeamB },
                new List<string> { TestDataBuilder.TeamA }); // User not in stage operate teams
            var service = CreateService(userContext);

            // Act
            var result = service.CheckStagePermission(stage, workflow, TestDataBuilder.DefaultUserId, OperationTypeEnum.Operate);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.CanView.Should().BeTrue();
            result.CanOperate.Should().BeFalse();
        }

        #endregion

        #region GetStagePermissionInfoForList - Batch Optimization Tests

        [Fact]
        public void GetStagePermissionInfoForList_PreloadedEntities_ShouldNotQueryDatabase()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA });
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
            var service = CreateService(userContext);

            // Act - This method should NOT call the repository
            var result = service.GetStagePermissionInfoForList(
                stage,
                workflow,
                TestDataBuilder.DefaultUserId,
                hasViewModulePermission: true,
                hasOperateModulePermission: true);

            // Assert
            result.Should().NotBeNull();
            result.CanView.Should().BeTrue();
            _mockStageRepo.Verify(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockWorkflowRepo.Verify(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GetStagePermissionInfoForList_NoModulePermission_ShouldDenyView()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var workflow = TestDataBuilder.CreatePublicWorkflow();
            var stage = TestDataBuilder.CreateStageWithInheritedPermissions(workflow.Id);
            var service = CreateService(userContext);

            // Act
            var result = service.GetStagePermissionInfoForList(
                stage,
                workflow,
                TestDataBuilder.DefaultUserId,
                hasViewModulePermission: false,
                hasOperateModulePermission: false);

            // Assert
            result.Should().NotBeNull();
            result.CanView.Should().BeFalse();
            result.ErrorMessage.Should().Contain("module permission");
        }

        #endregion
    }
}

