using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Xunit;
using static FlowFlex.Application.Services.OW.Permission.PermissionHelpers;

namespace FlowFlex.Tests.Services.Permission
{
    /// <summary>
    /// Tests for PermissionHelpers JSON deserialization improvements
    /// </summary>
    public class PermissionHelpersJsonTests
    {
        private readonly PermissionHelpers _helpers;

        public PermissionHelpersJsonTests()
        {
            var mockLogger = MockHelper.CreateMockLogger<PermissionHelpers>();
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();

            _helpers = new PermissionHelpers(
                mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);
        }

        #region DeserializeTeamList Tests

        [Fact]
        public void DeserializeTeamList_ValidJson_ShouldReturnList()
        {
            // Arrange
            var json = "[\"team-a\", \"team-b\", \"team-c\"]";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain("team-a");
            result.Should().Contain("team-b");
            result.Should().Contain("team-c");
        }

        [Fact]
        public void DeserializeTeamList_DoubleEscapedJson_ShouldReturnList()
        {
            // Arrange
            var json = "\"[\\\"team-a\\\", \\\"team-b\\\"]\"";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain("team-a");
            result.Should().Contain("team-b");
        }

        [Fact]
        public void DeserializeTeamList_EmptyString_ShouldReturnEmptyList()
        {
            // Arrange
            var json = "";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DeserializeTeamList_NullString_ShouldReturnEmptyList()
        {
            // Arrange
            string json = null;

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DeserializeTeamList_WhitespaceString_ShouldReturnEmptyList()
        {
            // Arrange
            var json = "   ";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DeserializeTeamList_InvalidJson_ShouldReturnEmptyList()
        {
            // Arrange
            var json = "invalid-json-{[}";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DeserializeTeamList_EmptyArray_ShouldReturnEmptyList()
        {
            // Arrange
            var json = "[]";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DeserializeTeamList_ArrayWithEmptyStrings_ShouldReturnListWithEmptyStrings()
        {
            // Arrange
            var json = "[\"\", \"team-a\", \"\"]";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain("");
            result.Should().Contain("team-a");
        }

        [Fact]
        public void DeserializeTeamList_ArrayWithSpecialCharacters_ShouldReturnList()
        {
            // Arrange
            var json = "[\"team-1\", \"team@2\", \"team#3\", \"team$4\"]";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result.Should().Contain("team-1");
            result.Should().Contain("team@2");
            result.Should().Contain("team#3");
            result.Should().Contain("team$4");
        }

        [Fact]
        public void DeserializeTeamList_ArrayWithUnicodeCharacters_ShouldReturnList()
        {
            // Arrange
            var json = "[\"团队-1\", \"팀-2\", \"チーム-3\"]";

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain("团队-1");
            result.Should().Contain("팀-2");
            result.Should().Contain("チーム-3");
        }

        [Fact]
        public void DeserializeTeamList_LargeArray_ShouldReturnList()
        {
            // Arrange
            var teams = Enumerable.Range(1, 100).Select(i => $"team-{i}").ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(teams);

            // Act
            var result = _helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(100);
            result.Should().Contain("team-1");
            result.Should().Contain("team-50");
            result.Should().Contain("team-100");
        }

        #endregion

        #region Constants Tests

        [Fact]
        public void DEFAULT_TEAM_OTHER_ShouldBeOther()
        {
            // Assert
            DEFAULT_TEAM_OTHER.Should().Be("Other");
        }

        #endregion
    }
}

