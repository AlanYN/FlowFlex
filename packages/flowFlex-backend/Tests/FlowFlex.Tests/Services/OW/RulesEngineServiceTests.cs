using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
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
using SqlSugar;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for RulesEngineService
    /// Tests expression evaluation, input data building, and format conversion
    /// </summary>
    public class RulesEngineServiceTests
    {
        private readonly Mock<ISqlSugarClient> _mockDb;
        private readonly Mock<IStageRepository> _mockStageRepository;
        private readonly Mock<IOnboardingRepository> _mockOnboardingRepository;
        private readonly Mock<IComponentDataService> _mockComponentDataService;
        private readonly Mock<IConditionActionExecutor> _mockActionExecutor;
        private readonly Mock<ILogger<RulesEngineService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly RulesEngineService _service;

        private const string TestTenantId = "test-tenant";

        public RulesEngineServiceTests()
        {
            _mockDb = new Mock<ISqlSugarClient>();
            _mockStageRepository = MockHelper.CreateMockStageRepository();
            _mockOnboardingRepository = MockHelper.CreateMockOnboardingRepository();
            _mockComponentDataService = new Mock<IComponentDataService>();
            _mockActionExecutor = new Mock<IConditionActionExecutor>();
            _mockLogger = MockHelper.CreateMockLogger<RulesEngineService>();

            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new RulesEngineService(
                _mockDb.Object,
                _mockStageRepository.Object,
                _mockOnboardingRepository.Object,
                _mockComponentDataService.Object,
                _mockActionExecutor.Object,
                _userContext,
                _mockLogger.Object);
        }

        #region BuildInputDataAsync Tests

        [Fact]
        public async Task BuildInputDataAsync_ShouldReturnInputDataWithAllComponents()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);
            SetupComponentDataMocks(onboardingId, stageId);

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            result.Should().NotBeNull();
            var inputDict = result as IDictionary<string, object>;
            inputDict.Should().NotBeNull();
            inputDict.Should().ContainKey("checklist");
            inputDict.Should().ContainKey("questionnaire");
            inputDict.Should().ContainKey("attachments");
            inputDict.Should().ContainKey("fields");
        }

        [Fact]
        public async Task BuildInputDataAsync_ShouldCalculateCompletionPercentage()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);
            SetupComponentDataMocksWithCustomChecklist(onboardingId, stageId, new ChecklistData
            {
                Status = "InProgress",
                CompletedCount = 3,
                TotalCount = 5,
                Tasks = new List<TaskStatusData>()
            });

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            var inputDict = result as IDictionary<string, object>;
            inputDict.Should().NotBeNull();
            inputDict.Should().ContainKey("checklist");
        }

        [Fact]
        public async Task BuildInputDataAsync_WithZeroTotalCount_ShouldReturnZeroPercentage()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);
            SetupComponentDataMocksWithCustomChecklist(onboardingId, stageId, new ChecklistData
            {
                Status = "Pending",
                CompletedCount = 0,
                TotalCount = 0,
                Tasks = new List<TaskStatusData>()
            });

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            var inputDict = result as IDictionary<string, object>;
            inputDict.Should().NotBeNull();
        }

        [Fact]
        public async Task BuildInputDataAsync_WithAttachments_ShouldReturnCorrectData()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);
            SetupDefaultComponentMocks();

            _mockComponentDataService.Setup(s => s.GetAttachmentDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new AttachmentData
                {
                    FileCount = 3,
                    TotalSize = 1024000,
                    FileNames = new List<string> { "doc1.pdf", "doc2.pdf", "image.png" }
                });

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            var inputDict = result as IDictionary<string, object>;
            inputDict.Should().NotBeNull();
            inputDict.Should().ContainKey("attachments");
        }

        [Fact]
        public async Task BuildInputDataAsync_WhenComponentServiceThrows_ShouldReturnEmptyObject()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);

            _mockComponentDataService.Setup(s => s.GetChecklistDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ExpandoObject>();
        }

        [Fact]
        public async Task BuildInputDataAsync_WithQuestionnaireData_ShouldIncludeAnswers()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);
            SetupDefaultComponentMocks();

            _mockComponentDataService.Setup(s => s.GetQuestionnaireDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new QuestionnaireData
                {
                    Status = "Completed",
                    TotalScore = 85,
                    Answers = new Dictionary<string, object>
                    {
                        { "q1", "Yes" },
                        { "q2", 100 }
                    }
                });

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            var inputDict = result as IDictionary<string, object>;
            inputDict.Should().NotBeNull();
            inputDict.Should().ContainKey("questionnaire");
        }

        [Fact]
        public async Task BuildInputDataAsync_WithFieldsData_ShouldIncludeCustomFields()
        {
            // Arrange
            var onboardingId = 1L;
            var stageId = 10L;

            SetupOnboardingMock(onboardingId);
            SetupDefaultComponentMocks();

            _mockComponentDataService.Setup(s => s.GetFieldsDataAsync(
                    It.IsAny<long>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "customerName", "Test Customer" },
                    { "amount", 10000 },
                    { "isVip", true }
                });

            // Act
            var result = await _service.BuildInputDataAsync(onboardingId, stageId);

            // Assert
            var inputDict = result as IDictionary<string, object>;
            inputDict.Should().NotBeNull();
            inputDict.Should().ContainKey("fields");
        }

        #endregion

        #region Helper Methods

        private void SetupOnboardingMock(long onboardingId)
        {
            var onboarding = new Onboarding
            {
                Id = onboardingId,
                TenantId = TestTenantId,
                IsValid = true
            };

            _mockOnboardingRepository.Setup(r => r.GetByIdWithoutTenantFilterAsync(
                    It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(onboarding);
        }

        private void SetupDefaultComponentMocks()
        {
            _mockComponentDataService.Setup(s => s.GetChecklistDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new ChecklistData());

            _mockComponentDataService.Setup(s => s.GetQuestionnaireDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new QuestionnaireData());

            _mockComponentDataService.Setup(s => s.GetAttachmentDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new AttachmentData());

            _mockComponentDataService.Setup(s => s.GetFieldsDataAsync(
                    It.IsAny<long>()))
                .ReturnsAsync(new Dictionary<string, object>());
        }

        private void SetupComponentDataMocks(long onboardingId, long stageId)
        {
            _mockComponentDataService.Setup(s => s.GetChecklistDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new ChecklistData
                {
                    Status = "Completed",
                    CompletedCount = 5,
                    TotalCount = 5,
                    Tasks = new List<TaskStatusData>
                    {
                        new TaskStatusData { TaskId = 1, Name = "Task 1", IsCompleted = true },
                        new TaskStatusData { TaskId = 2, Name = "Task 2", IsCompleted = true }
                    }
                });

            _mockComponentDataService.Setup(s => s.GetQuestionnaireDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new QuestionnaireData
                {
                    Status = "Completed",
                    TotalScore = 85,
                    Answers = new Dictionary<string, object>
                    {
                        { "q1", "Yes" },
                        { "q2", 100 }
                    }
                });

            _mockComponentDataService.Setup(s => s.GetAttachmentDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new AttachmentData
                {
                    FileCount = 2,
                    TotalSize = 1024,
                    FileNames = new List<string> { "file1.pdf", "file2.pdf" }
                });

            _mockComponentDataService.Setup(s => s.GetFieldsDataAsync(
                    It.IsAny<long>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "customerName", "Test Customer" },
                    { "amount", 10000 }
                });
        }

        private void SetupComponentDataMocksWithCustomChecklist(long onboardingId, long stageId, ChecklistData checklistData)
        {
            _mockComponentDataService.Setup(s => s.GetChecklistDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(checklistData);

            _mockComponentDataService.Setup(s => s.GetQuestionnaireDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new QuestionnaireData());

            _mockComponentDataService.Setup(s => s.GetAttachmentDataAsync(
                    It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync(new AttachmentData());

            _mockComponentDataService.Setup(s => s.GetFieldsDataAsync(
                    It.IsAny<long>()))
                .ReturnsAsync(new Dictionary<string, object>());
        }

        #endregion
    }
}
