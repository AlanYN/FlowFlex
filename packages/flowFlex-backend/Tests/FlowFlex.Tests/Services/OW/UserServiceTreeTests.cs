using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests to analyze why some users are missing from the user tree
    /// Specifically analyzing why "1029USERA1" (id: "1983413796546416640") is missing
    /// </summary>
    public class UserServiceTreeTests
    {
        /// <summary>
        /// Test to analyze the teamUserLookup dictionary creation
        /// This might reveal why some users are being filtered out
        /// </summary>
        [Fact]
        public void AnalyzeTeamUserLookup_WithEmptyTeamId_ShouldGroupCorrectly()
        {
            // Arrange - Based on actual API response data
            var teamUsers = new List<IdmTeamUserDto>
            {
                new IdmTeamUserDto
                {
                    Id = "1983420757136510976",
                    UserName = "1029USERB1",
                    Email = "1029USERB1@ITEM.COM",
                    FirstName = "B",
                    LastName = "1",
                    TeamId = "", // Empty string
                    UserType = 3
                },
                new IdmTeamUserDto
                {
                    Id = "1983413796546416640",
                    UserName = "1029USERA1",
                    Email = "1029USERA1@ITEM.COM",
                    FirstName = "A",
                    LastName = "1",
                    TeamId = "", // Empty string - THE MISSING USER
                    UserType = 3
                },
                new IdmTeamUserDto
                {
                    Id = "1983420992336302080",
                    UserName = "1029USERC1",
                    Email = "1029USERC1@ITEM.COM",
                    FirstName = "C",
                    LastName = "1",
                    TeamId = "1983419338111193088", // Has teamId
                    UserType = 3
                }
            };

            // Act - Simulate the lookup creation logic from ConvertIdmTeamTreeToUserTreeInternal
            var teamUserLookup = teamUsers.GroupBy(tu => tu.TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Assert
            teamUserLookup.Should().ContainKey(""); // Empty string key should exist
            teamUserLookup[""].Should().HaveCount(2, "Both users with empty teamId should be grouped together");
            teamUserLookup[""].Should().Contain(u => u.Id == "1983420757136510976");
            teamUserLookup[""].Should().Contain(u => u.Id == "1983413796546416640");

            // Test string.IsNullOrEmpty behavior - this is the logic used in the actual code
            var allTeamIds = new HashSet<string> { "1983419338111193088" };
            var usersWithoutTeam = teamUsers
                .Where(tu => (string.IsNullOrEmpty(tu.TeamId) || !allTeamIds.Contains(tu.TeamId)) && tu.UserType != 1)
                .GroupBy(u => u.Id) // Deduplicate users by ID
                .Select(g => g.First())
                .ToList();

            usersWithoutTeam.Should().HaveCount(2, "Both users with empty teamId should be identified as users without team");
            usersWithoutTeam.Should().Contain(u => u.Id == "1983420757136510976");
            usersWithoutTeam.Should().Contain(u => u.Id == "1983413796546416640");
        }

        /// <summary>
        /// Test to analyze potential issue: if a team node has empty Value, users with empty TeamId might be incorrectly assigned
        /// This tests the scenario where teamUserLookup contains empty string key and a team node might match it
        /// </summary>
        [Fact]
        public void AnalyzeTeamUserLookup_WithEmptyTeamNodeValue_ShouldNotMatchEmptyTeamId()
        {
            // Arrange - Simulate the scenario where teamUserLookup has empty string key
            var teamUsers = new List<IdmTeamUserDto>
            {
                new IdmTeamUserDto
                {
                    Id = "1983413796546416640",
                    UserName = "1029USERA1",
                    TeamId = "", // Empty string
                    UserType = 3
                }
            };

            // Simulate team tree nodes - check if any team has empty Value
            var teamTreeNodes = new List<IdmTeamTreeNodeDto>
            {
                new IdmTeamTreeNodeDto
                {
                    Value = "1983419304879722496", // Normal team ID
                    Label = "A",
                    Children = new List<IdmTeamTreeNodeDto>()
                }
            };

            // Act - Simulate the lookup creation
            var teamUserLookup = teamUsers.GroupBy(tu => tu.TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Check if any team node Value matches the empty TeamId key
            var allTeamValues = new HashSet<string>();
            void CollectTeamValues(List<IdmTeamTreeNodeDto> nodes)
            {
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.Value))
                    {
                        allTeamValues.Add(node.Value);
                    }
                    if (node.Children != null && node.Children.Any())
                    {
                        CollectTeamValues(node.Children);
                    }
                }
            }
            CollectTeamValues(teamTreeNodes);

            // Assert
            teamUserLookup.Should().ContainKey("");
            allTeamValues.Should().NotContain("", "No team node should have empty Value");

            // The user with empty TeamId should NOT be matched to any team node
            var userWithEmptyTeamId = teamUserLookup[""].First();
            var shouldBeInOtherTeam = !allTeamValues.Contains(userWithEmptyTeamId.TeamId) || string.IsNullOrEmpty(userWithEmptyTeamId.TeamId);
            shouldBeInOtherTeam.Should().BeTrue("User with empty TeamId should be in 'Other' team, not matched to any team node");
        }

        /// <summary>
        /// Critical test: Simulate the actual ConvertIdmTeamTreeToUserTreeInternal logic
        /// This tests if users with empty TeamId are incorrectly assigned to teams during the team iteration
        /// </summary>
        [Fact]
        public void SimulateConvertLogic_UsersWithEmptyTeamId_ShouldNotBeAssignedToTeams()
        {
            // Arrange - Based on actual API response
            var teamTreeNodes = new List<IdmTeamTreeNodeDto>
            {
                new IdmTeamTreeNodeDto
                {
                    Value = "1983419304879722496",
                    Label = "A",
                    Children = new List<IdmTeamTreeNodeDto>()
                },
                new IdmTeamTreeNodeDto
                {
                    Value = "1983419338111193088",
                    Label = "C",
                    Children = new List<IdmTeamTreeNodeDto>()
                }
            };

            var teamUsers = new List<IdmTeamUserDto>
            {
                new IdmTeamUserDto
                {
                    Id = "1983420757136510976",
                    UserName = "1029USERB1",
                    TeamId = "", // Empty - should go to "Other"
                    UserType = 3
                },
                new IdmTeamUserDto
                {
                    Id = "1983413796546416640",
                    UserName = "1029USERA1",
                    TeamId = "", // Empty - THE MISSING USER
                    UserType = 3
                },
                new IdmTeamUserDto
                {
                    Id = "1983420992336302080",
                    UserName = "1029USERC1",
                    TeamId = "1983419338111193088", // Has teamId
                    UserType = 3
                }
            };

            // Act - Simulate the exact logic from ConvertIdmTeamTreeToUserTreeInternal
            var teamUserLookup = teamUsers.GroupBy(tu => tu.TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var assignedUserIds = new HashSet<string>();

            // Simulate the team iteration logic
            foreach (var idmNode in teamTreeNodes)
            {
                // This is the critical check: if teamUserLookup contains the team node's Value
                if (teamUserLookup.ContainsKey(idmNode.Value))
                {
                    var usersInTeam = teamUserLookup[idmNode.Value]
                        .Where(tu => tu.UserType != 1)
                        .ToList();

                    foreach (var teamUser in usersInTeam)
                    {
                        assignedUserIds.Add(teamUser.Id);
                    }
                }
            }

            // Collect all team IDs from tree
            var allTeamIds = new HashSet<string>();
            void CollectAllTeamIds(List<IdmTeamTreeNodeDto> nodes)
            {
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.Value))
                    {
                        allTeamIds.Add(node.Value);
                    }
                    if (node.Children != null && node.Children.Any())
                    {
                        CollectAllTeamIds(node.Children);
                    }
                }
            }
            CollectAllTeamIds(teamTreeNodes);

            // Find users without team (this is the logic for "Other" team)
            var usersWithoutTeam = teamUsers
                .Where(tu => (string.IsNullOrEmpty(tu.TeamId) || !allTeamIds.Contains(tu.TeamId)) && tu.UserType != 1)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList();

            // Assert
            // Users with empty TeamId should NOT be assigned to any team during iteration
            assignedUserIds.Should().NotContain("1983420757136510976",
                "User with empty TeamId should not be assigned to any team node");
            assignedUserIds.Should().NotContain("1983413796546416640",
                "User with empty TeamId should not be assigned to any team node");

            // Users with empty TeamId should be in usersWithoutTeam
            usersWithoutTeam.Should().Contain(u => u.Id == "1983420757136510976");
            usersWithoutTeam.Should().Contain(u => u.Id == "1983413796546416640",
                "THE MISSING USER should be identified as user without team");

            // User with valid TeamId should be assigned
            assignedUserIds.Should().Contain("1983420992336302080");
            usersWithoutTeam.Should().NotContain(u => u.Id == "1983420992336302080");
        }

        /// <summary>
        /// Test to check if there's an issue with duplicate user IDs
        /// The API response shows user "1983495399779995648" appears twice with different teamIds
        /// </summary>
        [Fact]
        public void ConvertIdmTeamTreeToUserTree_WithDuplicateUserIds_ShouldHandleCorrectly()
        {
            // Arrange
            var teamTreeNodes = new List<IdmTeamTreeNodeDto>
            {
                new IdmTeamTreeNodeDto
                {
                    Value = "1983419304879722496",
                    Label = "A",
                    Children = new List<IdmTeamTreeNodeDto>
                    {
                        new IdmTeamTreeNodeDto
                        {
                            Value = "1986015619933409280",
                            Label = "A1",
                            Children = new List<IdmTeamTreeNodeDto>()
                        }
                    }
                }
            };

            // User appears in API response twice with different teamIds
            var teamUsers = new List<IdmTeamUserDto>
            {
                new IdmTeamUserDto
                {
                    Id = "1983495399779995648",
                    UserName = "1029AdminA1",
                    TeamId = "1986015619933409280", // First occurrence
                    UserType = 2
                },
                new IdmTeamUserDto
                {
                    Id = "1983495399779995648", // Same user ID
                    UserName = "1029AdminA1",
                    TeamId = "1983419304879722496", // Second occurrence with different teamId
                    UserType = 2
                },
                new IdmTeamUserDto
                {
                    Id = "1983413796546416640",
                    UserName = "1029USERA1",
                    TeamId = "", // Empty teamId
                    UserType = 3
                }
            };

            // Analyze: When grouping by TeamId, the same user might appear in multiple groups
            var teamUserLookup = teamUsers.GroupBy(tu => tu.TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // The user with ID "1983495399779995648" appears in both:
            // - teamUserLookup["1986015619933409280"]
            // - teamUserLookup["1983419304879722496"]

            // When processing "Other" team, we filter users without teamId
            var allTeamIds = new HashSet<string> { "1983419304879722496", "1986015619933409280" };
            var usersWithoutTeam = teamUsers
                .Where(tu => (string.IsNullOrEmpty(tu.TeamId) || !allTeamIds.Contains(tu.TeamId)) && tu.UserType != 1)
                .GroupBy(u => u.Id) // This deduplicates by user ID
                .Select(g => g.First())
                .ToList();

            // Assert
            usersWithoutTeam.Should().HaveCount(1, "Only user with empty teamId should be in usersWithoutTeam");
            usersWithoutTeam.Should().Contain(u => u.Id == "1983413796546416640");
            usersWithoutTeam.Should().NotContain(u => u.Id == "1983495399779995648",
                "User with teamId should not be in usersWithoutTeam even if it appears multiple times");
        }
    }
}

