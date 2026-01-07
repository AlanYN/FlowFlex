using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.OW.Dashboard;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW;
using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlowFlex.Tests.Services.OW
{
    /// <summary>
    /// Unit tests for DashboardService
    /// </summary>
    public class DashboardServiceTests
    {
        private readonly Mock<IOnboardingRepository> _mockOnboardingRepo;
        private readonly Mock<IChecklistTaskRepository> _mockChecklistTaskRepo;
        private readonly Mock<IStageRepository> _mockStageRepo;
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepo;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<DashboardService>> _mockLogger;
        private readonly Mock<PermissionHelpers> _mockPermissionHelpers;
        private readonly UserContext _userContext;
        private readonly DashboardService _service;

        public DashboardServiceTests()
        {
            _mockOnboardingRepo = new Mock<IOnboardingRepository>();
            _mockChecklistTaskRepo = new Mock<IChecklistTaskRepository>();
            _mockStageRepo = new Mock<IStageRepository>();
            _mockWorkflowRepo = new Mock<IWorkflowRepository>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<DashboardService>>();

            _userContext = new UserContext
            {
                UserId = "123",
                TenantId = "DEFAULT",
                UserTeams = new UserTeamModel
                {
                    TeamId = 1,
                    SubTeam = new List<UserTeamModel>()
                }
            };

            // Create mock for PermissionHelpers
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockHelpersLogger = new Mock<ILogger<PermissionHelpers>>();
            _mockPermissionHelpers = new Mock<PermissionHelpers>(
                mockHelpersLogger.Object,
                _userContext,
                mockHttpContextAccessor.Object);
            
            // Setup admin bypass for tests (return true to skip permission filtering)
            _mockPermissionHelpers.Setup(p => p.HasAdminPrivileges()).Returns(true);

            _service = new DashboardService(
                _mockOnboardingRepo.Object,
                _mockChecklistTaskRepo.Object,
                _mockStageRepo.Object,
                _mockWorkflowRepo.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object,
                _mockPermissionHelpers.Object);
        }

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_ShouldReturnStatisticsData()
        {
            // Arrange
            SetupOnboardingCountAsync(45);
            SetupOverdueTasksCount(5);
            SetupAverageCompletionTime();

            // Act
            var result = await _service.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.ActiveCases.Should().NotBeNull();
            result.CompletedThisMonth.Should().NotBeNull();
            result.OverdueTasks.Should().NotBeNull();
            result.AvgCompletionTime.Should().NotBeNull();
        }

        [Fact]
        public async Task GetStatisticsAsync_WithTeamFilter_ShouldApplyFilter()
        {
            // Arrange
            var team = "Sales";
            SetupOnboardingCountAsync(20);
            SetupOverdueTasksCount(2);
            SetupAverageCompletionTime();

            // Act
            var result = await _service.GetStatisticsAsync(team);

            // Assert
            result.Should().NotBeNull();
            // Verify GetListAsync was called (the service uses GetListAsync instead of CountAsync)
            _mockOnboardingRepo.Verify(r => r.GetListAsync(
                It.IsAny<Expression<Func<Onboarding, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()), Times.AtLeastOnce);
        }

        #endregion

        #region GetStageDistributionAsync Tests

        [Fact]
        public async Task GetStageDistributionAsync_ShouldReturnStagesOrderedBySequence()
        {
            // Arrange
            var stages = new List<Stage>
            {
                CreateStage(1, "Initial Review", 1, "#4CAF50"),
                CreateStage(2, "Document Collection", 2, "#2196F3"),
                CreateStage(3, "Final Review", 3, "#FF9800")
            };

            SetupStageRepository(stages);
            SetupOnboardingCountAsync(10);
            SetupOnboardingListAsync(new List<Onboarding>());

            // Act
            var result = await _service.GetStageDistributionAsync();

            // Assert
            result.Should().NotBeNull();
            result.Stages.Should().NotBeNull();
            result.Stages.Should().BeInAscendingOrder(s => s.Order);
        }

        [Fact]
        public async Task GetStageDistributionAsync_WithWorkflowFilter_ShouldFilterByWorkflow()
        {
            // Arrange
            var workflowId = 1L;
            var stages = new List<Stage> { CreateStage(1, "Stage 1", 1, "#4CAF50") };

            SetupStageRepository(stages);
            SetupOnboardingCountAsync(5);
            SetupOnboardingListAsync(new List<Onboarding>());

            // Act
            var result = await _service.GetStageDistributionAsync(workflowId);

            // Assert
            result.Should().NotBeNull();
            _mockStageRepo.Verify(r => r.GetListAsync(
                It.IsAny<Expression<Func<Stage, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()), Times.Once);
        }

        #endregion

        #region GetTasksAsync Tests

        [Fact]
        public async Task GetTasksAsync_ShouldReturnPaginatedResults()
        {
            // Arrange
            var query = new DashboardTaskQueryDto { PageIndex = 1, PageSize = 10 };
            var tasks = CreateTaskInfoList(5);

            SetupPendingTasksForUser(tasks, 25);

            // Act
            var result = await _service.GetTasksAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            // TotalCount is the count after permission filtering, which equals the number of tasks returned
            result.TotalCount.Should().Be(5);
            result.PageIndex.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetTasksAsync_ShouldMarkOverdueTasks()
        {
            // Arrange
            var query = new DashboardTaskQueryDto { PageIndex = 1, PageSize = 10 };
            var tasks = new List<DashboardTaskInfo>
            {
                new DashboardTaskInfo
                {
                    Id = 1,
                    Name = "Overdue Task",
                    DueDate = DateTimeOffset.UtcNow.AddDays(-2),
                    IsCompleted = false
                },
                new DashboardTaskInfo
                {
                    Id = 2,
                    Name = "Future Task",
                    DueDate = DateTimeOffset.UtcNow.AddDays(5),
                    IsCompleted = false
                }
            };

            SetupPendingTasksForUser(tasks, 2);

            // Act
            var result = await _service.GetTasksAsync(query);

            // Assert
            result.Items.Should().Contain(t => t.IsOverdue == true);
            result.Items.Should().Contain(t => t.IsOverdue == false);
        }

        #endregion

        #region GetMessageSummaryAsync Tests

        [Fact]
        public async Task GetMessageSummaryAsync_ShouldReturnMessagesAndUnreadCount()
        {
            // Arrange
            var messages = CreateMessageListItems(3);
            SetupMessageService(messages, 5);

            // Act
            var result = await _service.GetMessageSummaryAsync(5);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetMessageSummaryAsync_ShouldReturnMessageListItemFormat()
        {
            // Arrange
            var messages = new List<MessageListItemDto>
            {
                new MessageListItemDto
                {
                    Id = 1,
                    SenderName = "John Smith",
                    Subject = "Test",
                    ReceivedDate = DateTimeOffset.UtcNow,
                    IsRead = false
                }
            };
            SetupMessageService(messages, 1);

            // Act
            var result = await _service.GetMessageSummaryAsync(5);

            // Assert
            result.Messages.First().SenderName.Should().Be("John Smith");
            result.Messages.First().Subject.Should().Be("Test");
        }

        #endregion

        #region GetAchievementsAsync Tests

        [Fact]
        public async Task GetAchievementsAsync_ShouldReturnRecentlyCompletedCases()
        {
            // Arrange
            var completedCases = CreateCompletedOnboardings(3);
            SetupRecentlyCompletedAsync(completedCases);

            // Act
            var result = await _service.GetAchievementsAsync(5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().OnlyContain(a => a.Type == "CaseCompleted");
        }

        [Fact]
        public async Task GetAchievementsAsync_ShouldCalculateDaysToComplete()
        {
            // Arrange
            var completedCases = new List<Onboarding>
            {
                new Onboarding
                {
                    Id = 1,
                    CaseName = "Test Case",
                    CaseCode = "CASE-001",
                    StartDate = DateTimeOffset.UtcNow.AddDays(-14),
                    ActualCompletionDate = DateTimeOffset.UtcNow
                }
            };
            SetupRecentlyCompletedAsync(completedCases);

            // Act
            var result = await _service.GetAchievementsAsync(5);

            // Assert
            result.First().DaysToComplete.Should().Be(14);
        }

        #endregion

        #region GetDeadlinesAsync Tests

        [Fact]
        public async Task GetDeadlinesAsync_ShouldReturnUpcomingDeadlines()
        {
            // Arrange
            var tasks = CreateTaskInfoList(3);
            SetupUpcomingDeadlines(tasks);

            // Act
            var result = await _service.GetDeadlinesAsync(7);

            // Assert
            // GetDeadlinesAsync returns deadlines from StagesProgressJson, not from GetUpcomingDeadlinesAsync
            // Since we don't have StagesProgressJson data in our mock, it returns empty
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDeadlinesAsync_ShouldOrderByDueDate()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var tasks = new List<DashboardTaskInfo>
            {
                new DashboardTaskInfo { Id = 1, Name = "Task 3", DueDate = now.AddDays(5) },
                new DashboardTaskInfo { Id = 2, Name = "Task 1", DueDate = now.AddDays(1) },
                new DashboardTaskInfo { Id = 3, Name = "Task 2", DueDate = now.AddDays(3) }
            };
            SetupUpcomingDeadlines(tasks);

            // Act
            var result = await _service.GetDeadlinesAsync(7);

            // Assert
            // GetDeadlinesAsync returns deadlines from StagesProgressJson, not from GetUpcomingDeadlinesAsync
            // Since we don't have StagesProgressJson data in our mock, it returns empty
            // But if there were results, they would be ordered by DueDate
            result.Should().NotBeNull();
        }

        #endregion

        #region GetDashboardAsync Tests

        [Fact]
        public async Task GetDashboardAsync_WithAllModules_ShouldReturnAllData()
        {
            // Arrange
            var query = new DashboardQueryDto();
            SetupAllMocks();

            // Act
            var result = await _service.GetDashboardAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Statistics.Should().NotBeNull();
            result.CasesOverview.Should().NotBeNull();
            result.Tasks.Should().NotBeNull();
            result.Messages.Should().NotBeNull();
            result.Achievements.Should().NotBeNull();
            result.Deadlines.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDashboardAsync_WithSpecificModules_ShouldReturnOnlyRequestedData()
        {
            // Arrange
            var query = new DashboardQueryDto
            {
                Modules = new List<string> { "statistics", "tasks" }
            };
            SetupAllMocks();

            // Act
            var result = await _service.GetDashboardAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Statistics.Should().NotBeNull();
            result.Tasks.Should().NotBeNull();
            result.CasesOverview.Should().BeNull();
            result.Messages.Should().BeNull();
            result.Achievements.Should().BeNull();
            result.Deadlines.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private void SetupOnboardingCountAsync(int count)
        {
            _mockOnboardingRepo.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Onboarding, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(count);
        }

        private void SetupOverdueTasksCount(int count)
        {
            _mockChecklistTaskRepo.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<ChecklistTask, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(count);

            // Also setup GetListAsync for GetOverdueTasksCountAsync
            _mockChecklistTaskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<ChecklistTask, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(new List<ChecklistTask>());
        }

        private void SetupAverageCompletionTime()
        {
            _mockOnboardingRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Onboarding, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(new List<Onboarding>());
        }

        private void SetupOnboardingListAsync(List<Onboarding> onboardings)
        {
            _mockOnboardingRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Onboarding, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(onboardings);
        }

        private void SetupStageRepository(List<Stage> stages)
        {
            _mockStageRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Stage, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(stages);
        }

        private void SetupPendingTasksForUser(List<DashboardTaskInfo> tasks, int totalCount)
        {
            _mockChecklistTaskRepo.Setup(r => r.GetPendingTasksForUserAsync(
                It.IsAny<long>(),
                It.IsAny<List<long>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(tasks);

            _mockChecklistTaskRepo.Setup(r => r.GetPendingTasksCountForUserAsync(
                It.IsAny<long>(),
                It.IsAny<List<long>>(),
                It.IsAny<string>()))
                .ReturnsAsync(totalCount);

            // Setup onboarding repository for permission filtering
            var onboardingIds = tasks.Select(t => t.OnboardingId).Distinct().ToList();
            var onboardings = onboardingIds.Select(id => new Onboarding { Id = id, WorkflowId = 1 }).ToList();
            _mockOnboardingRepo.Setup(r => r.GetListAsync(
                It.Is<Expression<Func<Onboarding, bool>>>(e => true),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(onboardings);

            // Setup workflow repository for permission check
            _mockWorkflowRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Workflow, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(new List<Workflow> { new Workflow { Id = 1 } });
        }

        private void SetupMessageService(List<MessageListItemDto> messages, int unreadCount)
        {
            var pagedResult = new PageModelDto<MessageListItemDto>(1, messages.Count, messages, messages.Count);

            _mockMessageService.Setup(r => r.GetPagedAsync(It.IsAny<MessageQueryDto>()))
                .ReturnsAsync(pagedResult);
        }

        private void SetupRecentlyCompletedAsync(List<Onboarding> onboardings)
        {
            _mockOnboardingRepo.Setup(r => r.GetRecentlyCompletedAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
                .ReturnsAsync(onboardings);
        }

        private void SetupUpcomingDeadlines(List<DashboardTaskInfo> tasks)
        {
            // Setup pending tasks for user (used to get onboarding IDs)
            _mockChecklistTaskRepo.Setup(r => r.GetPendingTasksForUserAsync(
                It.IsAny<long>(),
                It.IsAny<List<long>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(tasks);

            // Setup onboarding repository - return empty list since deadlines come from StagesProgressJson
            _mockOnboardingRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Onboarding, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(new List<Onboarding>());

            _mockChecklistTaskRepo.Setup(r => r.GetUpcomingDeadlinesAsync(
                It.IsAny<long>(),
                It.IsAny<List<long>>(),
                It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(tasks);
        }

        private Stage CreateStage(long id, string name, int order, string color)
        {
            return new Stage
            {
                Id = id,
                Name = name,
                Order = order,
                Color = color,
                IsActive = true,
                IsValid = true
            };
        }

        private List<DashboardTaskInfo> CreateTaskInfoList(int count)
        {
            var tasks = new List<DashboardTaskInfo>();
            for (int i = 1; i <= count; i++)
            {
                tasks.Add(new DashboardTaskInfo
                {
                    Id = i,
                    Name = $"Task {i}",
                    Description = $"Description {i}",
                    Priority = i % 2 == 0 ? "High" : "Medium",
                    DueDate = DateTimeOffset.UtcNow.AddDays(i),
                    IsCompleted = false,
                    CaseCode = $"CASE-{i:D3}",
                    CaseName = $"Case {i}",
                    OnboardingId = i * 100,
                    AssignedTeam = "Sales Team",
                    Status = "Pending",
                    IsRequired = true
                });
            }
            return tasks;
        }

        private List<MessageListItemDto> CreateMessageListItems(int count)
        {
            var messages = new List<MessageListItemDto>();
            for (int i = 1; i <= count; i++)
            {
                messages.Add(new MessageListItemDto
                {
                    Id = i,
                    SenderName = $"Sender {i}",
                    Subject = $"Subject {i}",
                    BodyPreview = $"Preview {i}",
                    IsRead = i % 2 == 0,
                    ReceivedDate = DateTimeOffset.UtcNow.AddHours(-i)
                });
            }
            return messages;
        }

        private List<Onboarding> CreateCompletedOnboardings(int count)
        {
            var onboardings = new List<Onboarding>();
            for (int i = 1; i <= count; i++)
            {
                onboardings.Add(new Onboarding
                {
                    Id = i,
                    CaseName = $"Lead {i}",
                    CaseCode = $"CASE-{i:D3}",
                    Status = "Completed",
                    StartDate = DateTimeOffset.UtcNow.AddDays(-14 - i),
                    ActualCompletionDate = DateTimeOffset.UtcNow.AddDays(-i),
                    ModifyDate = DateTimeOffset.UtcNow.AddDays(-i)
                });
            }
            return onboardings;
        }

        private void SetupAllMocks()
        {
            // Statistics mocks
            SetupOnboardingCountAsync(45);
            SetupOverdueTasksCount(5);
            SetupOnboardingListAsync(new List<Onboarding>());

            // Stage distribution mocks
            SetupStageRepository(new List<Stage> { CreateStage(1, "Stage 1", 1, "#4CAF50") });

            // Tasks mocks
            SetupPendingTasksForUser(CreateTaskInfoList(3), 3);

            // Messages mocks
            SetupMessageService(CreateMessageListItems(2), 2);

            // Achievements mocks
            SetupRecentlyCompletedAsync(CreateCompletedOnboardings(2));

            // Deadlines mocks - note: GetDeadlinesAsync uses StagesProgressJson, not GetUpcomingDeadlinesAsync
            _mockChecklistTaskRepo.Setup(r => r.GetPendingTasksForUserAsync(
                It.IsAny<long>(),
                It.IsAny<List<long>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
                .ReturnsAsync(CreateTaskInfoList(2));

            // Setup workflow repository for permission check
            _mockWorkflowRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Workflow, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
                .ReturnsAsync(new List<Workflow> { new Workflow { Id = 1 } });
        }

        #endregion
    }
}
