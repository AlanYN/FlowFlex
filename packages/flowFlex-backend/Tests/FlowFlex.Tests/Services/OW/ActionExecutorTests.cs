using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
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
        private readonly Mock<ILogger<ConditionActionExecutor>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly ConditionActionExecutor _executor;

        public ActionExecutorTests()
        {
            _mockDb = new Mock<ISqlSugarClient>();
            _mockStageRepository = MockHelper.CreateMockStageRepository();
            _mockOnboardingRepository = MockHelper.CreateMockOnboardingRepository();
            _mockLogger = MockHelper.CreateMockLogger<ConditionActionExecutor>();

            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _executor = new ConditionActionExecutor(
                _mockDb.Object,
                _mockStageRepository.Object,
                _mockOnboardingRepository.Object,
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
            result.Details[0].ErrorMessage.Should().Contain("FieldName is required");
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
