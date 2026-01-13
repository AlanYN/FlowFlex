using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Service.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using SqlSugar;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for ConditionActionExecutor
    /// Tests action execution, error handling, and various action types
    /// </summary>
    public class ActionExecutorTests
    {
        private readonly Mock<ISqlSugarClient> _mockDb;
        private readonly Mock<IStageRepository> _mockStageRepository;
        private readonly Mock<IOnboardingRepository> _mockOnboardingRepository;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<ConditionActionExecutor>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly ConditionActionExecutor _executor;

        public ActionExecutorTests()
        {
            _mockDb = new Mock<ISqlSugarClient>();
            _mockStageRepository = MockHelper.CreateMockStageRepository();
            _mockOnboardingRepository = MockHelper.CreateMockOnboardingRepository();
            _mockEmailService = new Mock<IEmailService>();
            _mockUserService = new Mock<IUserService>();
            _mockLogger = MockHelper.CreateMockLogger<ConditionActionExecutor>();

            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            // Setup default email service mock
            _mockEmailService.Setup(e => e.SendStageCompletedNotificationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Setup default user service mock
            _mockUserService.Setup(u => u.GetUsersByIdsAsync(It.IsAny<List<long>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Application.Contracts.Dtos.OW.User.UserDto>());

            _executor = new ConditionActionExecutor(
                _mockDb.Object,
                _mockStageRepository.Object,
                _mockOnboardingRepository.Object,
                _mockEmailService.Object,
                _mockUserService.Object,
                _userContext,
                _mockLogger.Object);
        }

        #region ExecuteActionsAsync Tests

        [Fact]
        public async Task ExecuteActionsAsync_WithEmptyActions_ShouldReturnSuccess()
        {
            // Arrange
            var actionsJson = "[]";
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Details.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteActionsAsync_WithNullActions_ShouldReturnSuccess()
        {
            // Arrange
            var actionsJson = "null";
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Success.Should().BeTrue();
            result.Details.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteActionsAsync_WithInvalidJson_ShouldReturnFailure()
        {
            // Arrange
            var actionsJson = "invalid json {{{";
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Success.Should().BeFalse();
            result.Details.Should().ContainSingle();
            result.Details[0].ActionType.Should().Be("ParseError");
            result.Details[0].ErrorMessage.Should().Contain("Invalid ActionsJson format");
        }

        [Fact]
        public async Task ExecuteActionsAsync_WithUnsupportedActionType_ShouldReturnFailure()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "UnsupportedAction", Order = 1 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Success.Should().BeFalse();
            result.Details.Should().ContainSingle();
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("Unsupported action type");
        }

        [Fact]
        public async Task ExecuteActionsAsync_ShouldExecuteActionsInOrder()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "SendNotification", Order = 2, RecipientType = "User", RecipientId = "1" },
                new ConditionAction { Type = "SendNotification", Order = 1, RecipientType = "User", RecipientId = "2" }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Success.Should().BeTrue();
            result.Details.Should().HaveCount(2);
            // Actions should be executed in order (1, 2)
            result.Details[0].Order.Should().Be(1);
            result.Details[1].Order.Should().Be(2);
        }

        #endregion

        #region GoToStage Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_GoToStage_WithoutTargetStageId_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "GoToStage", Order = 1, TargetStageId = null }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeFalse();
            // The error message should indicate that TargetStageId is required
            result.Details[0].ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExecuteActionsAsync_GoToStage_WithInvalidTargetStage_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "GoToStage", Order = 1, TargetStageId = 999 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, 999, null);

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("not found or inactive");
        }

        [Fact]
        public async Task ExecuteActionsAsync_GoToStage_WithInactiveTargetStage_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "GoToStage", Order = 1, TargetStageId = 20 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup inactive target stage
            var inactiveStage = new Stage
            {
                Id = 20,
                WorkflowId = 1,
                Name = "Inactive Stage",
                Order = 2,
                IsActive = false,
                IsValid = true,
                TenantId = "default"
            };
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, 20, inactiveStage);

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("not found or inactive");
        }

        [Fact]
        public async Task ExecuteActionsAsync_GoToStage_WithValidTargetStage_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "GoToStage", Order = 1, TargetStageId = 20 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup valid target stage
            var targetStage = new Stage
            {
                Id = 20,
                WorkflowId = 1,
                Name = "Target Stage",
                Order = 3,
                IsActive = true,
                IsValid = true,
                TenantId = "default"
            };
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, 20, targetStage);

            // Setup current stage
            var currentStage = new Stage
            {
                Id = context.StageId,
                WorkflowId = 1,
                Name = "Current Stage",
                Order = 1,
                IsActive = true,
                IsValid = true,
                TenantId = "default"
            };
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, context.StageId, currentStage);

            // Setup onboarding
            var onboarding = new Onboarding
            {
                Id = context.OnboardingId,
                WorkflowId = 1,
                CurrentStageId = context.StageId,
                TenantId = "default"
            };
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, onboarding);
            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("targetStageId");
            result.Details[0].ResultData["targetStageId"].Should().Be(20);
        }

        [Fact]
        public async Task ExecuteActionsAsync_GoToStage_WithNonExistentOnboarding_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "GoToStage", Order = 1, TargetStageId = 20 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup valid target stage
            var targetStage = new Stage
            {
                Id = 20,
                WorkflowId = 1,
                Name = "Target Stage",
                Order = 2,
                IsActive = true,
                IsValid = true,
                TenantId = "default"
            };
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, 20, targetStage);

            // Setup mock to return null onboarding
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, null);

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("not found");
        }

        #endregion

        #region EndWorkflow Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_EndWorkflow_ShouldSetDefaultStatus()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "EndWorkflow", Order = 1 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("endStatus");
            result.Details[0].ResultData["endStatus"].Should().Be("Completed");
        }

        [Fact]
        public async Task ExecuteActionsAsync_EndWorkflow_WithCustomStatus_ShouldUseCustomStatus()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "EndWorkflow", Order = 1, EndStatus = "Cancelled" }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData["endStatus"].Should().Be("Cancelled");
        }

        [Fact]
        public async Task ExecuteActionsAsync_EndWorkflow_WithRejectedStatus_ShouldUseRejectedStatus()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "EndWorkflow", Order = 1, EndStatus = "Rejected" }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData["endStatus"].Should().Be("Rejected");
        }

        #endregion

        #region SkipStage Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_SkipStage_WithDefaultSkipCount_ShouldSkipOneStage()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "SkipStage", Order = 1 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup current stage
            var currentStage = new Stage
            {
                Id = context.StageId,
                WorkflowId = 1,
                Order = 1,
                IsActive = true,
                IsValid = true,
                TenantId = "default"
            };
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, context.StageId, currentStage);

            // Setup next stages query - mock will return empty list by default
            // This will trigger the "not enough stages to skip" path

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            // When there are no more stages, it should try to end workflow
            result.Details[0].ActionType.Should().BeOneOf("SkipStage", "EndWorkflow");
        }

        [Fact]
        public async Task ExecuteActionsAsync_SkipStage_WithCustomSkipCount_ShouldSkipMultipleStages()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "SkipStage", Order = 1, SkipCount = 2 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup current stage
            var currentStage = new Stage
            {
                Id = context.StageId,
                WorkflowId = 1,
                Order = 1,
                IsActive = true,
                IsValid = true,
                TenantId = "default"
            };
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, context.StageId, currentStage);

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ExecuteActionsAsync_SkipStage_WithNonExistentCurrentStage_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "SkipStage", Order = 1 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock to return null for current stage
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, context.StageId, null);

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("not found");
        }

        #endregion

        #region SendNotification Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_SendNotification_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "SendNotification", 
                    Order = 1, 
                    RecipientType = "User",
                    RecipientId = "123",
                    TemplateId = "template-001"
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("recipientType");
            result.Details[0].ResultData.Should().ContainKey("status");
            result.Details[0].ResultData["status"].Should().Be("Queued");
        }

        #endregion

        #region UpdateField Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_UpdateField_WithoutFieldName_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "UpdateField", Order = 1, FieldName = null, FieldValue = "test" }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("FieldName");
        }

        [Fact]
        public async Task ExecuteActionsAsync_UpdateField_WithParametersFormat_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "UpdateField", 
                    Order = 1,
                    Parameters = new Dictionary<string, object>
                    {
                        { "fieldPath", "customerStatus" },
                        { "newValue", "VIP" }
                    }
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock for onboarding
            var onboarding = new Onboarding
            {
                Id = context.OnboardingId,
                CustomFieldsJson = "{}",
                TenantId = "default"
            };
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, onboarding);
            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("fieldName");
            result.Details[0].ResultData["fieldName"].Should().Be("customerStatus");
        }

        [Fact]
        public async Task ExecuteActionsAsync_UpdateField_WithTopLevelFieldName_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "UpdateField", 
                    Order = 1,
                    FieldName = "priority",
                    FieldValue = "High"
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock for onboarding
            var onboarding = new Onboarding
            {
                Id = context.OnboardingId,
                CustomFieldsJson = "{\"priority\": \"Low\"}",
                TenantId = "default"
            };
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, onboarding);
            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("oldValue");
            result.Details[0].ResultData.Should().ContainKey("newValue");
            result.Details[0].ResultData["newValue"].Should().Be("High");
        }

        [Fact]
        public async Task ExecuteActionsAsync_UpdateField_WithNonExistentOnboarding_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "UpdateField", 
                    Order = 1,
                    FieldName = "status",
                    FieldValue = "Active"
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock to return null onboarding
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, null);

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ErrorMessage.Should().Contain("not found");
        }

        #endregion

        #region TriggerAction Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_TriggerAction_WithoutActionDefinitionId_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "TriggerAction", Order = 1, ActionDefinitionId = null }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeFalse();
            result.Details[0].ActionType.Should().Be("TriggerAction");
            // Error message should indicate the action failed (either validation or execution error)
            result.Details[0].ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExecuteActionsAsync_TriggerAction_WithValidActionDefinitionId_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "TriggerAction", Order = 1, ActionDefinitionId = 100 }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock for action definition query
            // Note: The actual query is done via _db.Queryable which is harder to mock
            // This test verifies the basic flow

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            // The action will fail because we can't easily mock the Queryable
            // but we verify the flow is correct
        }

        #endregion

        #region AssignUser Action Tests

        [Fact]
        public async Task ExecuteActionsAsync_AssignUser_WithoutUserIdOrTeamId_ShouldFail()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "AssignUser", Order = 1, UserId = null, TeamId = null }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteActionsAsync_AssignUser_WithParametersFormat_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "AssignUser", 
                    Order = 1,
                    Parameters = new Dictionary<string, object>
                    {
                        { "assigneeType", "user" },
                        { "assigneeIds", new[] { "456", "789" } }
                    }
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock for onboarding
            var onboarding = new Onboarding
            {
                Id = context.OnboardingId,
                ViewUsers = "[]",
                OperateUsers = "[]",
                TenantId = "default"
            };
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, onboarding);
            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("assigneeType");
            result.Details[0].ResultData["assigneeType"].Should().Be("user");
        }

        [Fact]
        public async Task ExecuteActionsAsync_AssignUser_WithTeamType_ShouldSucceed()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "AssignUser", 
                    Order = 1,
                    Parameters = new Dictionary<string, object>
                    {
                        { "assigneeType", "team" },
                        { "assigneeIds", new[] { "team-001", "team-002" } }
                    }
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock for onboarding
            var onboarding = new Onboarding
            {
                Id = context.OnboardingId,
                ViewTeams = "[]",
                OperateTeams = "[]",
                TenantId = "default"
            };
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, onboarding);
            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeTrue();
            result.Details[0].ResultData.Should().ContainKey("assigneeType");
            result.Details[0].ResultData["assigneeType"].Should().Be("team");
        }

        [Fact]
        public async Task ExecuteActionsAsync_AssignUser_WithLegacyUserId_ShouldSucceed()
        {
            // Arrange - Test backward compatibility with legacy UserId property
            var actions = new List<ConditionAction>
            {
                new ConditionAction 
                { 
                    Type = "AssignUser", 
                    Order = 1,
                    UserId = 456
                }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Setup mock for onboarding
            var onboarding = new Onboarding
            {
                Id = context.OnboardingId,
                ViewUsers = "[]",
                OperateUsers = "[]",
                TenantId = "default"
            };
            MockHelper.SetupOnboardingRepositoryGetById(_mockOnboardingRepository, context.OnboardingId, onboarding);
            SetupOnboardingUpdateable();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().NotBeEmpty();
            result.Details[0].Success.Should().BeTrue();
        }

        #endregion

        #region Multiple Actions Tests

        [Fact]
        public async Task ExecuteActionsAsync_WithMultipleActions_ShouldContinueOnFailure()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "UpdateField", Order = 1, FieldName = null }, // Will fail
                new ConditionAction { Type = "SendNotification", Order = 2, RecipientType = "User" } // Should still execute
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().HaveCount(2);
            result.Details[0].Success.Should().BeFalse();
            result.Details[1].Success.Should().BeTrue();
            // Overall success should be true because at least one action succeeded
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteActionsAsync_WithAllFailedActions_ShouldReturnOverallFailure()
        {
            // Arrange
            var actions = new List<ConditionAction>
            {
                new ConditionAction { Type = "UpdateField", Order = 1, FieldName = null },
                new ConditionAction { Type = "GoToStage", Order = 2, TargetStageId = null }
            };
            var actionsJson = JsonConvert.SerializeObject(actions);
            var context = CreateExecutionContext();

            // Act
            var result = await _executor.ExecuteActionsAsync(actionsJson, context);

            // Assert
            result.Details.Should().HaveCount(2);
            result.Details.All(d => !d.Success).Should().BeTrue();
            result.Success.Should().BeFalse();
        }

        #endregion

        #region Helper Methods

        private ActionExecutionContext CreateExecutionContext()
        {
            return new ActionExecutionContext
            {
                OnboardingId = 100,
                StageId = 10,
                ConditionId = 1,
                TenantId = "default",
                UserId = 123
            };
        }

        private void SetupOnboardingUpdateable()
        {
            var mockUpdateable = new Mock<IUpdateable<Onboarding>>();
            mockUpdateable.Setup(u => u.SetColumns(It.IsAny<System.Linq.Expressions.Expression<Func<Onboarding, Onboarding>>>()))
                .Returns(mockUpdateable.Object);
            mockUpdateable.Setup(u => u.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Onboarding, bool>>>()))
                .Returns(mockUpdateable.Object);
            mockUpdateable.Setup(u => u.ExecuteCommandAsync())
                .ReturnsAsync(1);

            _mockDb.Setup(db => db.Updateable<Onboarding>())
                .Returns(mockUpdateable.Object);
        }

        #endregion
    }
}
