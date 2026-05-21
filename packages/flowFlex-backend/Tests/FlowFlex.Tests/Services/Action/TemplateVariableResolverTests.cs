using System.Collections.Generic;
using FlowFlex.Application.Services.Action;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FlowFlex.Tests.Services.Action
{
    public class TemplateVariableResolverTests
    {
        private readonly Mock<ILogger<TemplateVariableResolver>> _mockLogger;
        private readonly TemplateVariableResolver _resolver;

        public TemplateVariableResolverTests()
        {
            _mockLogger = new Mock<ILogger<TemplateVariableResolver>>();
            _resolver = new TemplateVariableResolver(_mockLogger.Object);
        }

        [Fact]
        public void Replace_TopLevelVariable_ShouldResolve()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001",
                ["WorkflowId"] = 3001L
            };

            var result = _resolver.Replace("Code: {{CaseCode}}", context);

            result.Should().Be("Code: OW-001");
        }

        [Fact]
        public void Replace_NestedPath_ShouldResolve()
        {
            var context = new Dictionary<string, object>
            {
                ["previousActionResult"] = JObject.Parse("{\"data\":{\"customerCode\":\"C0001\"}}")
            };

            var result = _resolver.Replace("{{previousActionResult.data.customerCode}}", context);

            result.Should().Be("C0001");
        }

        [Fact]
        public void Replace_QuestionnaireAnswerByQuestionId_ShouldResolve()
        {
            var context = new Dictionary<string, object>
            {
                ["questionnaireAnswerByQuestionId"] = new Dictionary<string, object>
                {
                    ["2001"] = "ABC Logistics"
                }
            };

            var result = _resolver.Replace("{{questionnaireAnswerByQuestionId.2001}}", context);

            result.Should().Be("ABC Logistics");
        }

        [Fact]
        public void Replace_QuestionnaireAnswerMap_DoubleNestedNumericKey_ShouldResolve()
        {
            var context = new Dictionary<string, object>
            {
                ["questionnaireAnswerMap"] = new Dictionary<string, object>
                {
                    ["1001"] = new Dictionary<string, object>
                    {
                        ["2001"] = "ABC Logistics"
                    }
                }
            };

            var result = _resolver.Replace("{{questionnaireAnswerMap.1001.2001}}", context);

            result.Should().Be("ABC Logistics");
        }

        [Fact]
        public void Replace_MultiplePlaceholders_ShouldResolveAll()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001",
                ["CaseName"] = "Test Case"
            };

            var result = _resolver.Replace("{{CaseCode}} - {{CaseName}}", context);

            result.Should().Be("OW-001 - Test Case");
        }

        [Fact]
        public void Replace_UnknownPath_ShouldReturnEmptyString()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001"
            };

            var result = _resolver.Replace("{{nonExistent.path}}", context);

            result.Should().Be("");
        }

        [Fact]
        public void Replace_UnknownPath_ShouldLogWarning()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001"
            };

            _resolver.Replace("{{nonExistent.path}}", context);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("nonExistent.path")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Replace_NullInput_ShouldReturnNull()
        {
            var context = new Dictionary<string, object>();

            var result = _resolver.Replace(null, context);

            result.Should().BeNull();
        }

        [Fact]
        public void Replace_EmptyInput_ShouldReturnEmpty()
        {
            var context = new Dictionary<string, object>();

            var result = _resolver.Replace("", context);

            result.Should().BeEmpty();
        }

        [Fact]
        public void Replace_NullContext_ShouldReturnOriginal()
        {
            var result = _resolver.Replace("{{CaseCode}}", null);

            result.Should().Be("{{CaseCode}}");
        }

        [Fact]
        public void Replace_OldShallowTemplate_ShouldStillWork()
        {
            var context = new Dictionary<string, object>
            {
                ["WorkflowId"] = 3001L,
                ["OnboardingId"] = 123L,
                ["StageId"] = 456L
            };

            var result = _resolver.Replace("wf/{{WorkflowId}}/ob/{{OnboardingId}}", context);

            result.Should().Be("wf/3001/ob/123");
        }

        [Fact]
        public void Replace_PlaceholderWithSpaces_ShouldResolve()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001"
            };

            var result = _resolver.Replace("{{ CaseCode }}", context);

            result.Should().Be("OW-001");
        }

        [Fact]
        public void Replace_ObjectValue_ShouldSerializeAsJson()
        {
            var context = new Dictionary<string, object>
            {
                ["previousActionResult"] = JObject.Parse("{\"data\":{\"id\":1,\"name\":\"test\"}}")
            };

            var result = _resolver.Replace("{{previousActionResult.data}}", context);

            result.Should().Contain("\"id\":1");
            result.Should().Contain("\"name\":\"test\"");
        }

        [Fact]
        public void ResolvePath_TopLevel_ShouldReturnValue()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001"
            };

            var result = _resolver.ResolvePath(context, "CaseCode");

            result.Should().Be("OW-001");
        }

        [Fact]
        public void ResolvePath_Nested_ShouldReturnValue()
        {
            var context = new Dictionary<string, object>
            {
                ["questionnaireAnswerMap"] = new Dictionary<string, object>
                {
                    ["1001"] = new Dictionary<string, object>
                    {
                        ["2001"] = "Answer Value"
                    }
                }
            };

            var result = _resolver.ResolvePath(context, "questionnaireAnswerMap.1001.2001");

            result.Should().Be("Answer Value");
        }

        [Fact]
        public void ResolvePath_NonExistent_ShouldReturnNull()
        {
            var context = new Dictionary<string, object>
            {
                ["CaseCode"] = "OW-001"
            };

            var result = _resolver.ResolvePath(context, "nonExistent.path");

            result.Should().BeNull();
        }
    }
}
