using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Services.Integration;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Tests.TestBase;
using FluentAssertions;
using Moq;
using Xunit;
using AutoMapper;
using Domain.Shared.Enums;

namespace FlowFlex.Tests.Services.Integration
{
    public class IntegrationSyncServiceTests
    {
        private readonly Mock<IIntegrationSyncLogRepository> _mockRepository;
        private readonly Mock<IIntegrationRepository> _mockIntegrationRepository;
        private readonly Mock<IEntityMappingRepository> _mockEntityMappingRepository;
        private readonly Mock<IFieldMappingRepository> _mockFieldMappingRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<IntegrationSyncService>> _mockLogger;
        private readonly UserContext _userContext;
        private readonly IntegrationSyncService _service;

        public IntegrationSyncServiceTests()
        {
            _mockRepository = new Mock<IIntegrationSyncLogRepository>();
            _mockIntegrationRepository = new Mock<IIntegrationRepository>();
            _mockEntityMappingRepository = new Mock<IEntityMappingRepository>();
            _mockFieldMappingRepository = new Mock<IFieldMappingRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = MockHelper.CreateMockLogger<IntegrationSyncService>();
            _userContext = TestDataBuilder.CreateUserContext(TestDataBuilder.DefaultUserId);

            _service = new IntegrationSyncService(
                _mockRepository.Object,
                _mockIntegrationRepository.Object,
                _mockEntityMappingRepository.Object,
                _mockFieldMappingRepository.Object,
                _mockMapper.Object,
                _userContext,
                _mockLogger.Object
            );
        }

        #region LogSyncAsync Tests

        [Fact]
        public async Task LogSyncAsync_WithValidInput_ShouldReturnLogId()
        {
            // Arrange
            var input = new IntegrationSyncLogInputDto
            {
                IntegrationId = 123,
                EntityMappingId = 456,
                SyncDirection = SyncDirection.ViewOnly,
                ExternalId = "ext-123",
                InternalId = "int-456",
                SyncStatus = SyncStatus.Success,
                RecordsProcessed = 1,
                DurationMs = 150
            };

            var entity = new IntegrationSyncLog
            {
                Id = 789,
                IntegrationId = input.IntegrationId,
                SyncDirection = input.SyncDirection,
                SyncStatus = input.SyncStatus
            };

            _mockMapper.Setup(m => m.Map<IntegrationSyncLog>(It.IsAny<IntegrationSyncLogInputDto>()))
                .Returns(entity);
            _mockRepository.Setup(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<IntegrationSyncLog>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(789L);

            // Act
            var result = await _service.LogSyncAsync(input);

            // Assert
            result.Should().Be(789L);
            _mockRepository.Verify(r => r.InsertReturnSnowflakeIdAsync(It.IsAny<IntegrationSyncLog>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetSyncLogsAsync Tests

        [Fact]
        public async Task GetSyncLogsAsync_WithValidIntegrationId_ShouldReturnPagedList()
        {
            // Arrange
            var integrationId = 123L;
            var pageIndex = 1;
            var pageSize = 10;
            var entities = new List<IntegrationSyncLog>
            {
                new IntegrationSyncLog
                {
                    Id = 1,
                    IntegrationId = integrationId,
                    SyncStatus = SyncStatus.Success,
                    CreateDate = DateTime.UtcNow.AddMinutes(-5)
                },
                new IntegrationSyncLog
                {
                    Id = 2,
                    IntegrationId = integrationId,
                    SyncStatus = SyncStatus.Failed,
                    CreateDate = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            var dtos = new List<IntegrationSyncLogOutputDto>
            {
                new IntegrationSyncLogOutputDto { Id = 1, IntegrationId = integrationId },
                new IntegrationSyncLogOutputDto { Id = 2, IntegrationId = integrationId }
            };

            _mockRepository.Setup(r => r.QueryPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<long?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync((entities.Take(pageSize).ToList(), entities.Count));
            _mockMapper.Setup(m => m.Map<List<IntegrationSyncLogOutputDto>>(It.IsAny<List<IntegrationSyncLog>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetSyncLogsAsync(integrationId, pageIndex, pageSize);

            // Assert
            result.items.Should().NotBeNull();
            result.total.Should().Be(entities.Count);
        }

        #endregion

        #region GetSyncStatisticsAsync Tests

        [Fact]
        public async Task GetSyncStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var integrationId = 123L;
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            var expectedStatistics = new Dictionary<string, int>
            {
                ["Total"] = 100,
                ["Success"] = 85,
                ["Failed"] = 10,
                ["Pending"] = 3,
                ["InProgress"] = 2,
                ["PartialSuccess"] = 0
            };

            _mockRepository.Setup(r => r.GetSyncStatisticsAsync(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(expectedStatistics);

            // Act
            var result = await _service.GetSyncStatisticsAsync(integrationId, startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result["Total"].Should().Be(100);
            result["Success"].Should().Be(85);
            result["Failed"].Should().Be(10);
        }

        #endregion

        #region GetFailedSyncLogsAsync Tests

        [Fact]
        public async Task GetFailedSyncLogsAsync_ShouldReturnOnlyFailedLogs()
        {
            // Arrange
            var integrationId = 123L;
            var limit = 50;
            var entities = new List<IntegrationSyncLog>
            {
                new IntegrationSyncLog
                {
                    Id = 1,
                    IntegrationId = integrationId,
                    SyncStatus = SyncStatus.Failed,
                    ErrorMessage = "Connection timeout"
                },
                new IntegrationSyncLog
                {
                    Id = 2,
                    IntegrationId = integrationId,
                    SyncStatus = SyncStatus.Failed,
                    ErrorMessage = "Invalid credentials"
                }
            };

            var dtos = new List<IntegrationSyncLogOutputDto>
            {
                new IntegrationSyncLogOutputDto { Id = 1 },
                new IntegrationSyncLogOutputDto { Id = 2 }
            };

            _mockRepository.Setup(r => r.GetFailedSyncLogsAsync(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<IntegrationSyncLogOutputDto>>(It.IsAny<List<IntegrationSyncLog>>()))
                .Returns(dtos);

            // Act
            var result = await _service.GetFailedSyncLogsAsync(integrationId, limit);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        #endregion
    }
}
