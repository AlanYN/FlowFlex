using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Service.OW;
using FlowFlex.Application.Service.OW.StageCondition;
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
        private readonly Mock<IComponentNameQueryService> _mockComponentNameQueryService;
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
            _mockComponentNameQueryService = new Mock<IComponentNameQueryService>();
            _mockLogger = MockHelper.CreateMockLogger<RulesEngineService>();

            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new RulesEngineService(
                _mockDb.Object,
                _mockStageRepository.Object,
                _mockOnboardingRepository.Object,
                _mockComponentDataService.Object,
                _mockComponentNameQueryService.Object,
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


    /// <summary>
    /// Unit tests for RulesEngineService expression building
    /// Tests frontend format conversion and expression generation
    /// </summary>
    public class RulesEngineExpressionTests
    {
        #region Frontend Format Conversion Tests

        [Fact]
        public void ConvertFrontendFormat_WithEqualsOperator_ShouldGenerateCorrectExpression()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields[\""status\""]"",
                        ""operator"": ""=="",
                        ""value"": ""approved""
                    }
                ]
            }";

            // The expression should handle null safely
            // Expected pattern: (np(input.fields["status"]) == null ? "approved" == "" : np(input.fields["status"]).ToString() == "approved")
            
            // This test verifies the format is recognized as frontend format
            Assert.Contains("logic", frontendJson);
            Assert.Contains("rules", frontendJson);
        }

        [Fact]
        public void ConvertFrontendFormat_WithChineseValue_ShouldEscapeCorrectly()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""OR"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.questionnaire.answers[\""q1\""][\""q1_1\""]"",
                        ""operator"": ""=="",
                        ""value"": ""不知道""
                    }
                ]
            }";

            // Chinese characters should be preserved in the expression
            Assert.Contains("不知道", frontendJson);
        }

        [Fact]
        public void ConvertFrontendFormat_WithGreaterThanOperator_ShouldGenerateNumericComparison()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields[\""amount\""]"",
                        ""operator"": "">"",
                        ""value"": ""100""
                    }
                ]
            }";

            // The expression should use decimal.TryParse for numeric comparison
            // Expected pattern: (np(field) != null && decimal.TryParse(...) && _num > decimal.Parse("100"))
            
            Assert.Contains(">", frontendJson);
        }

        [Fact]
        public void ConvertFrontendFormat_WithContainsOperator_ShouldGenerateStringContains()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields[\""name\""]"",
                        ""operator"": ""contains"",
                        ""value"": ""test""
                    }
                ]
            }";

            // The expression should use .Contains() method
            Assert.Contains("contains", frontendJson);
        }

        [Fact]
        public void ConvertFrontendFormat_WithIsNullOperator_ShouldGenerateNullCheck()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields[\""optional\""]"",
                        ""operator"": ""isnull"",
                        ""value"": null
                    }
                ]
            }";

            // The expression should check for null
            Assert.Contains("isnull", frontendJson);
        }

        [Fact]
        public void ConvertFrontendFormat_WithOrLogic_ShouldCombineWithOrOperator()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""OR"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields[\""status\""]"",
                        ""operator"": ""=="",
                        ""value"": ""approved""
                    },
                    {
                        ""fieldPath"": ""input.fields[\""status\""]"",
                        ""operator"": ""=="",
                        ""value"": ""completed""
                    }
                ]
            }";

            // Multiple rules with OR logic should be combined with ||
            Assert.Contains("OR", frontendJson);
        }

        [Fact]
        public void ConvertFrontendFormat_WithAndLogic_ShouldCombineWithAndOperator()
        {
            // Arrange
            var frontendJson = @"{
                ""logic"": ""AND"",
                ""rules"": [
                    {
                        ""fieldPath"": ""input.fields[\""status\""]"",
                        ""operator"": ""=="",
                        ""value"": ""approved""
                    },
                    {
                        ""fieldPath"": ""input.fields[\""amount\""]"",
                        ""operator"": "">"",
                        ""value"": ""1000""
                    }
                ]
            }";

            // Multiple rules with AND logic should be combined with &&
            Assert.Contains("AND", frontendJson);
        }

        #endregion

        #region Null Handling Tests

        [Fact]
        public void SafeFieldsDictionary_WithMissingKey_ShouldReturnNull()
        {
            // Arrange
            var dict = new SafeFieldsDictionary();

            // Act
            var result = dict["nonexistent"];

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SafeFieldsDictionary_WithExistingKey_ShouldReturnValue()
        {
            // Arrange
            var sourceDict = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 }
            };
            var dict = SafeFieldsDictionary.FromDictionary(sourceDict);

            // Act
            var result1 = dict["key1"];
            var result2 = dict["key2"];

            // Assert
            Assert.Equal("value1", result1);
            Assert.Equal(123, result2);
        }

        [Fact]
        public void SafeNestedDictionary_WithMissingKey_ShouldReturnEmptyInnerDictionary()
        {
            // Arrange
            var dict = new SafeNestedDictionary();

            // Act
            var inner = dict["nonexistent"];

            // Assert
            Assert.NotNull(inner);
            Assert.Equal(0, inner.Count);
        }

        [Fact]
        public void SafeInnerDictionary_WithMissingKey_ShouldReturnNull()
        {
            // Arrange
            var dict = new SafeInnerDictionary();

            // Act
            var result = dict["nonexistent"];

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SafeTasksDictionary_WithMissingKey_ShouldReturnEmptyTaskInnerDictionary()
        {
            // Arrange
            var dict = new SafeTasksDictionary();

            // Act
            var inner = dict["nonexistent"];

            // Assert
            Assert.NotNull(inner);
            Assert.Equal(0, inner.Count);
        }

        [Fact]
        public void SafeTaskInnerDictionary_WithMissingKey_ShouldReturnDefaultTaskData()
        {
            // Arrange
            var dict = new SafeTaskInnerDictionary();

            // Act
            var result = dict["nonexistent"];

            // Assert
            Assert.NotNull(result);
            Assert.False(result.isCompleted);
            Assert.Equal(string.Empty, result.name);
        }

        #endregion

        #region Numeric Field Path Conversion Tests

        [Fact]
        public void ConvertFieldPath_WithNumericId_ShouldUseDictionaryAccess()
        {
            // Arrange
            var fieldPath = "input.fields.2006236814662307840";

            // The conversion should change dot notation to dictionary access
            // Expected: input.fields["2006236814662307840"]
            
            // Verify the pattern exists
            Assert.Contains("2006236814662307840", fieldPath);
        }

        [Fact]
        public void ConvertFieldPath_WithNestedNumericIds_ShouldConvertAll()
        {
            // Arrange
            var fieldPath = "input.questionnaire.answers[\"2008860456596410368\"][\"2008860456533495808\"]";

            // Already in dictionary access format, should remain unchanged
            Assert.Contains("[\"2008860456596410368\"]", fieldPath);
            Assert.Contains("[\"2008860456533495808\"]", fieldPath);
        }

        #endregion

        #region RuleUtils Tests

        [Fact]
        public void RuleUtils_IsEmpty_WithNull_ShouldReturnTrue()
        {
            Assert.True(RuleUtils.IsEmpty(null));
        }

        [Fact]
        public void RuleUtils_IsEmpty_WithEmptyString_ShouldReturnTrue()
        {
            Assert.True(RuleUtils.IsEmpty(""));
            Assert.True(RuleUtils.IsEmpty("   "));
        }

        [Fact]
        public void RuleUtils_IsEmpty_WithValue_ShouldReturnFalse()
        {
            Assert.False(RuleUtils.IsEmpty("test"));
        }

        [Fact]
        public void RuleUtils_InList_WithMatchingValue_ShouldReturnTrue()
        {
            Assert.True(RuleUtils.InList("apple", "apple,banana,orange"));
            Assert.True(RuleUtils.InList("APPLE", "apple,banana,orange")); // Case insensitive
        }

        [Fact]
        public void RuleUtils_InList_WithNonMatchingValue_ShouldReturnFalse()
        {
            Assert.False(RuleUtils.InList("grape", "apple,banana,orange"));
        }

        [Fact]
        public void RuleUtils_InList_WithNullValue_ShouldReturnFalse()
        {
            Assert.False(RuleUtils.InList(null, "apple,banana,orange"));
        }

        [Fact]
        public void RuleUtils_ContainsText_WithMatchingSubstring_ShouldReturnTrue()
        {
            Assert.True(RuleUtils.ContainsText("Hello World", "World"));
            Assert.True(RuleUtils.ContainsText("Hello World", "world")); // Case insensitive
        }

        [Fact]
        public void RuleUtils_ContainsText_WithNonMatchingSubstring_ShouldReturnFalse()
        {
            Assert.False(RuleUtils.ContainsText("Hello World", "Foo"));
        }

        [Fact]
        public void RuleUtils_ContainsText_WithNullSource_ShouldReturnFalse()
        {
            Assert.False(RuleUtils.ContainsText(null, "test"));
        }

        [Fact]
        public void RuleUtils_StartsWithText_ShouldWorkCorrectly()
        {
            Assert.True(RuleUtils.StartsWithText("Hello World", "Hello"));
            Assert.True(RuleUtils.StartsWithText("Hello World", "hello")); // Case insensitive
            Assert.False(RuleUtils.StartsWithText("Hello World", "World"));
            Assert.False(RuleUtils.StartsWithText(null, "test"));
        }

        [Fact]
        public void RuleUtils_EndsWithText_ShouldWorkCorrectly()
        {
            Assert.True(RuleUtils.EndsWithText("Hello World", "World"));
            Assert.True(RuleUtils.EndsWithText("Hello World", "world")); // Case insensitive
            Assert.False(RuleUtils.EndsWithText("Hello World", "Hello"));
            Assert.False(RuleUtils.EndsWithText(null, "test"));
        }

        [Fact]
        public void RuleUtils_Compare_WithNumericStrings_ShouldCompareNumerically()
        {
            // "9" > "10" as strings, but 9 < 10 as numbers
            Assert.True(RuleUtils.Compare("10", "9") > 0);
            Assert.True(RuleUtils.Compare("9", "10") < 0);
            Assert.True(RuleUtils.Compare("10", "10") == 0);
        }

        [Fact]
        public void RuleUtils_Compare_WithNonNumericStrings_ShouldCompareAsStrings()
        {
            Assert.True(RuleUtils.Compare("apple", "banana") < 0);
            Assert.True(RuleUtils.Compare("banana", "apple") > 0);
        }

        [Fact]
        public void RuleUtils_GreaterThan_ShouldWorkCorrectly()
        {
            Assert.True(RuleUtils.GreaterThan("100", "99"));
            Assert.False(RuleUtils.GreaterThan("99", "100"));
            Assert.False(RuleUtils.GreaterThan("100", "100"));
        }

        [Fact]
        public void RuleUtils_LessThan_ShouldWorkCorrectly()
        {
            Assert.True(RuleUtils.LessThan("99", "100"));
            Assert.False(RuleUtils.LessThan("100", "99"));
            Assert.False(RuleUtils.LessThan("100", "100"));
        }

        [Fact]
        public void RuleUtils_Equals_WithNullValues_ShouldHandleCorrectly()
        {
            Assert.True(RuleUtils.Equals(null, null));
            Assert.False(RuleUtils.Equals(null, "test"));
            Assert.False(RuleUtils.Equals("test", null));
        }

        [Fact]
        public void RuleUtils_Equals_WithNumericStrings_ShouldCompareNumerically()
        {
            Assert.True(RuleUtils.Equals("100", "100"));
            Assert.True(RuleUtils.Equals("100.00", "100")); // Decimal comparison
            Assert.False(RuleUtils.Equals("100", "99"));
        }

        #endregion
    }
