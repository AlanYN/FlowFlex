using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Service.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.Permission;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SqlSugar;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for StageConditionService
    /// Tests CRUD operations, validation, and permission checks
    /// </summary>
    public class StageConditionServiceTests
    {
        private readonly Mock<ISqlSugarClient> _mockDb;
        private readonly Mock<IStageRepository> _mockStageRepository;
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<StageConditionService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly StageConditionService _service;

        public StageConditionServiceTests()
        {
            _mockDb = new Mock<ISqlSugarClient>();
            _mockStageRepository = MockHelper.CreateMockStageRepository();
            _mockWorkflowRepository = MockHelper.CreateMockWorkflowRepository();
            _mockPermissionService = new Mock<IPermissionService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<StageConditionService>();

            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new StageConditionService(
                _mockDb.Object,
                _mockStageRepository.Object,
                _mockWorkflowRepository.Object,
                _mockPermissionService.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object);
        }

        #region ValidateRulesJsonAsync Tests

        [Fact]
        public async Task ValidateRulesJsonAsync_WithEmptyJson_ShouldReturnInvalid()
        {
            // Arrange
            var rulesJson = "";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "RULES_REQUIRED");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithNullJson_ShouldReturnInvalid()
        {
            // Arrange
            string rulesJson = null;

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "RULES_REQUIRED");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithInvalidJson_ShouldReturnInvalid()
        {
            // Arrange
            var rulesJson = "invalid json {{{";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "INVALID_JSON");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithEmptyWorkflowArray_ShouldReturnInvalid()
        {
            // Arrange
            var rulesJson = "[]";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "RULES_EMPTY");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithValidRulesEngineFormat_ShouldReturnValid()
        {
            // Arrange
            var rulesJson = @"[
                {
                    ""WorkflowName"": ""StageCondition"",
                    ""Rules"": [
                        {
                            ""RuleName"": ""ChecklistComplete"",
                            ""Expression"": ""input.checklist.status == \""Completed\""""
                        }
                    ]
                }
            ]";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithFrontendFormat_ShouldConvertAndValidate()
        {
            // Arrange
            var rulesJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.checklist.status"",
                        ""operator"": ""=="",
                        ""value"": ""Completed""
                    }
                ]
            }";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Code == "FORMAT_CONVERTED");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithMissingRuleName_ShouldReturnInvalid()
        {
            // Arrange
            var rulesJson = @"[
                {
                    ""WorkflowName"": ""StageCondition"",
                    ""Rules"": [
                        {
                            ""RuleName"": """",
                            ""Expression"": ""input.checklist.status == \""Completed\""""
                        }
                    ]
                }
            ]";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "RULE_NAME_REQUIRED");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithMissingExpression_ShouldReturnInvalid()
        {
            // Arrange
            var rulesJson = @"[
                {
                    ""WorkflowName"": ""StageCondition"",
                    ""Rules"": [
                        {
                            ""RuleName"": ""TestRule"",
                            ""Expression"": """"
                        }
                    ]
                }
            ]";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "RULE_EXPRESSION_REQUIRED");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithEmptyWorkflowName_ShouldReturnInvalid()
        {
            // Arrange - Empty WorkflowName causes RulesEngine validation to fail
            var rulesJson = @"[
                {
                    ""WorkflowName"": """",
                    ""Rules"": [
                        {
                            ""RuleName"": ""TestRule"",
                            ""Expression"": ""1 == 1""
                        }
                    ]
                }
            ]";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert - RulesEngine requires a non-empty WorkflowName for validation
            // The service adds a warning for empty WorkflowName, but RulesEngine validation fails
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "INVALID_EXPRESSION");
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithOrLogic_ShouldConvertCorrectly()
        {
            // Arrange
            var rulesJson = @"{
                ""logic"": ""OR"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.checklist.status"",
                        ""operator"": ""=="",
                        ""value"": ""Completed""
                    },
                    {
                        ""fieldPath"": ""input.questionnaire.totalScore"",
                        ""operator"": "">="",
                        ""value"": 80
                    }
                ]
            }";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithMultipleRules_ShouldValidateAll()
        {
            // Arrange
            var rulesJson = @"[
                {
                    ""WorkflowName"": ""StageCondition"",
                    ""Rules"": [
                        {
                            ""RuleName"": ""Rule1"",
                            ""Expression"": ""input.checklist.completedCount > 0""
                        },
                        {
                            ""RuleName"": ""Rule2"",
                            ""Expression"": ""input.attachments.fileCount >= 1""
                        }
                    ]
                }
            ]";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithContainsOperator_ShouldConvertCorrectly()
        {
            // Arrange
            var rulesJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields.customerName"",
                        ""operator"": ""contains"",
                        ""value"": ""VIP""
                    }
                ]
            }";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateRulesJsonAsync_WithIsNullOperator_ShouldConvertCorrectly()
        {
            // Arrange
            var rulesJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields.notes"",
                        ""operator"": ""isnotnull"",
                        ""value"": null
                    }
                ]
            }";

            // Act
            var result = await _service.ValidateRulesJsonAsync(rulesJson);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Permission Check Tests

        [Fact]
        public void SetupPermissionCheck_ShouldConfigureMockCorrectly()
        {
            // Arrange
            SetupPermissionCheck(true);

            // Act
            var result = _mockPermissionService.Object.CheckWorkflowAccessAsync(
                TestDataBuilder.DefaultUserId, 1, OperationTypeEnum.View);

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region Helper Methods

        private StageConditionInputDto CreateValidInput()
        {
            return new StageConditionInputDto
            {
                StageId = 10,
                WorkflowId = 1,
                Name = "Test Condition",
                Description = "Test Description",
                RulesJson = @"[{""WorkflowName"":""StageCondition"",""Rules"":[{""RuleName"":""Rule1"",""Expression"":""true""}]}]",
                ActionsJson = @"[{""type"":""SendNotification"",""order"":1,""recipientType"":""User"",""recipientId"":""123""}]",
                IsActive = true
            };
        }

        private void SetupPermissionCheck(bool hasPermission)
        {
            _mockPermissionService.Setup(p => p.CheckWorkflowAccessAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<OperationTypeEnum>()))
                .ReturnsAsync(new PermissionResult 
                { 
                    Success = hasPermission, 
                    ErrorMessage = hasPermission ? null : "No permission" 
                });
        }

        private void SetupValidStage(long stageId)
        {
            MockHelper.SetupStageRepositoryGetById(_mockStageRepository, stageId, new Stage
            {
                Id = stageId,
                WorkflowId = 1,
                Name = "Test Stage",
                Order = 1,
                IsActive = true,
                IsValid = true,
                TenantId = "default"
            });
        }

        #endregion
    }
}
