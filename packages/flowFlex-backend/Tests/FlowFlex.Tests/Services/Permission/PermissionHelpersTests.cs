using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using Xunit;
using static FlowFlex.Application.Services.OW.Permission.PermissionHelpers;

namespace FlowFlex.Tests.Services.Permission
{
    public class PermissionHelpersTests
    {
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<PermissionHelpers>> _mockLogger;

        public PermissionHelpersTests()
        {
            _mockLogger = MockHelper.CreateMockLogger<PermissionHelpers>();
        }

        #region GetUserTeamIds Tests

        [Fact]
        public void GetUserTeamIds_WithValidTeams_ShouldReturnTeamIdStrings()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamA, TestDataBuilder.TeamB });
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.GetUserTeamIds();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(TestDataBuilder.TeamA);
            result.Should().Contain(TestDataBuilder.TeamB);
        }

        [Fact]
        public void GetUserTeamIds_WithSingleTeam_ShouldReturnOneTeamId()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(
                TestDataBuilder.DefaultUserId,
                new List<string> { TestDataBuilder.TeamB });
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.GetUserTeamIds();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.Should().Contain(TestDataBuilder.TeamB);
        }

        [Fact]
        public void GetUserTeamIds_WithNullUserTeams_ShouldReturnOtherTeam()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            userContext.UserTeams = null;
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.GetUserTeamIds();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.Should().Contain(DEFAULT_TEAM_OTHER);
        }

        #endregion

        #region CheckTeamWhitelist Tests

        [Fact]
        public void CheckTeamWhitelist_UserInWhitelist_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var teamsJson = "[\"team-a\", \"team-b\"]";
            var userTeamIds = new List<string> { "team-a" };

            // Act
            var result = helpers.CheckTeamWhitelist(teamsJson, userTeamIds);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CheckTeamWhitelist_UserNotInWhitelist_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var teamsJson = "[\"team-a\", \"team-b\"]";
            var userTeamIds = new List<string> { "team-c" };

            // Act
            var result = helpers.CheckTeamWhitelist(teamsJson, userTeamIds);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CheckTeamWhitelist_EmptyTeamsJson_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var userTeamIds = new List<string> { "team-a" };

            // Act
            var result = helpers.CheckTeamWhitelist("", userTeamIds);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CheckTeamBlacklist Tests

        [Fact]
        public void CheckTeamBlacklist_UserNotInBlacklist_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var teamsJson = "[\"team-a\", \"team-b\"]";
            var userTeamIds = new List<string> { "team-c" };

            // Act
            var result = helpers.CheckTeamBlacklist(teamsJson, userTeamIds);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CheckTeamBlacklist_UserInBlacklist_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var teamsJson = "[\"team-a\", \"team-b\"]";
            var userTeamIds = new List<string> { "team-a" };

            // Act
            var result = helpers.CheckTeamBlacklist(teamsJson, userTeamIds);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CheckTeamBlacklist_EmptyTeamsJson_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var userTeamIds = new List<string> { "team-a" };

            // Act
            var result = helpers.CheckTeamBlacklist("", userTeamIds);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region CheckUserWhitelist Tests

        [Fact]
        public void CheckUserWhitelist_UserInWhitelist_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var usersJson = "[\"123\", \"456\"]";
            var userId = "123";

            // Act
            var result = helpers.CheckUserWhitelist(usersJson, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CheckUserWhitelist_UserNotInWhitelist_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var usersJson = "[\"123\", \"456\"]";
            var userId = "789";

            // Act
            var result = helpers.CheckUserWhitelist(usersJson, userId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CheckUserBlacklist Tests

        [Fact]
        public void CheckUserBlacklist_UserNotInBlacklist_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var usersJson = "[\"123\", \"456\"]";
            var userId = "789";

            // Act
            var result = helpers.CheckUserBlacklist(usersJson, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CheckUserBlacklist_UserInBlacklist_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var usersJson = "[\"123\", \"456\"]";
            var userId = "123";

            // Act
            var result = helpers.CheckUserBlacklist(usersJson, userId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsCurrentUserOwner Tests

        [Fact]
        public void IsCurrentUserOwner_UserIsOwner_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsCurrentUserOwner(TestDataBuilder.DefaultUserId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsCurrentUserOwner_UserIsNotOwner_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsCurrentUserOwner(TestDataBuilder.OwnerUserId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsCurrentUserOwner_NullCreateUserId_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsCurrentUserOwner(null);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Admin Privilege Tests

        [Fact]
        public void IsSystemAdmin_SystemAdminUser_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateSystemAdminContext();
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsSystemAdmin();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsTenantAdmin_TenantAdminUser_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateTenantAdminContext();
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsTenantAdmin();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasAdminPrivileges_RegularUser_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.HasAdminPrivileges();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsPortalTokenWithPortalAccess Tests

        [Fact]
        public void IsPortalTokenWithPortalAccess_PortalTokenAndPortalEndpoint_ShouldReturnTrue()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor(
                isPortalToken: true,
                hasPortalAccessAttribute: true);
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsPortalTokenWithPortalAccess();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsPortalTokenWithPortalAccess_RegularTokenAndPortalEndpoint_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor(
                isPortalToken: false,
                hasPortalAccessAttribute: true);
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsPortalTokenWithPortalAccess();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsPortalTokenWithPortalAccess_PortalTokenAndRegularEndpoint_ShouldReturnFalse()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor(
                isPortalToken: true,
                hasPortalAccessAttribute: false);
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.IsPortalTokenWithPortalAccess();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeserializeTeamList Tests

        [Fact]
        public void DeserializeTeamList_ValidJson_ShouldReturnList()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var json = "[\"team-a\", \"team-b\"]";

            // Act
            var result = helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain("team-a");
            result.Should().Contain("team-b");
        }

        [Fact]
        public void DeserializeTeamList_DoubleEscapedJson_ShouldReturnList()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var json = "\"[\\\"team-a\\\", \\\"team-b\\\"]\"";

            // Act
            var result = helpers.DeserializeTeamList(json);

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
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            // Act
            var result = helpers.DeserializeTeamList("");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void DeserializeTeamList_InvalidJson_ShouldReturnEmptyList()
        {
            // Arrange
            var userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);
            var mockHttpContextAccessor = MockHelper.CreateMockHttpContextAccessor();
            var helpers = new PermissionHelpers(
                _mockLogger.Object,
                userContext,
                mockHttpContextAccessor.Object);

            var json = "invalid-json";

            // Act
            var result = helpers.DeserializeTeamList(json);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion
    }
}

